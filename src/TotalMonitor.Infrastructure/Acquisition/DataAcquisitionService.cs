using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Modbus;
using TotalMonitor.Infrastructure.Modbus;

namespace TotalMonitor.Infrastructure.Acquisition;

public sealed class DataAcquisitionService(
    IServiceScopeFactory scopeFactory,
    IModbusClientFactory clients,
    MeterRegisterMap registerMap,
    ModbusConnectionOptions defaultOptions,
    ILogger<DataAcquisitionService> logger) : IDataAcquisitionService
{
    private readonly ConcurrentDictionary<int, Measurement> latest = new();
    private readonly ConcurrentDictionary<int, MeterConnectionStatus> meterStatuses = new();
    private readonly SemaphoreSlim lifecycle = new(1, 1);
    private CancellationTokenSource? cancellation;
    private Task? loop;

    public AcquisitionState State { get; private set; } = AcquisitionState.Stopped;
    public MeterHardwareState HardwareState { get; private set; } = MeterHardwareState.ESPERANDO_MEDIDOR;
    public string CurrentPort { get; private set; } = string.Empty;
    public int ActiveMetersCount { get; private set; } = 0;
    public DateTimeOffset? LastAcquisitionTime { get; private set; }
    public string? LastError { get; private set; }
    public long TotalReadingsProcessed { get; private set; } = 0;

    public IReadOnlyDictionary<int, Measurement> LastMeasurements => latest;
    public IReadOnlyDictionary<int, MeterConnectionStatus> MeterStatuses => meterStatuses;

    public event EventHandler<AcquisitionEvent>? EventRaised;
    public event EventHandler<Measurement>? MeasurementReceived;

    public async Task StartAsync(CancellationToken ct = default)
    {
        await lifecycle.WaitAsync(ct);
        try
        {
            if (State is AcquisitionState.Active or AcquisitionState.Starting)
                return;

            State = AcquisitionState.Starting;
            HardwareState = MeterHardwareState.ESPERANDO_MEDIDOR;
            cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
            loop = RunAsync(cancellation.Token);
            await Task.Yield();
        }
        finally
        {
            lifecycle.Release();
        }
    }

    public async Task StopAsync()
    {
        await lifecycle.WaitAsync();
        try
        {
            if (cancellation is null || State == AcquisitionState.Stopped)
                return;

            cancellation.Cancel();
            if (loop is not null)
            {
                try { await loop; } catch (OperationCanceledException) { }
            }
            cancellation.Dispose();
            cancellation = null;
            loop = null;
            State = AcquisitionState.Stopped;
            HardwareState = MeterHardwareState.NO_CONFIGURADO;
            Raise("AcquisitionStopped", null, "Adquisición detenida.");
            logger.LogInformation("Motor de adquisición detenido y puertos seriales liberados.");
        }
        finally
        {
            lifecycle.Release();
        }
    }

    public AcquisitionStatusSummary GetStatusSummary() =>
        new(
            State,
            HardwareState,
            HardwareState.ToDisplayString(),
            CurrentPort,
            ActiveMetersCount,
            LastAcquisitionTime,
            LastError,
            TotalReadingsProcessed);

    private async Task RunAsync(CancellationToken ct)
    {
        State = AcquisitionState.Active;
        Raise("AcquisitionStarted", null, "Adquisición iniciada. Esperando comunicación con medidor TOV452.");
        logger.LogInformation("Motor de adquisición iniciado en modo hardware real.");

        while (!ct.IsCancellationRequested)
        {
            var started = DateTimeOffset.UtcNow;
            int delayMs = 1000;

            try
            {
                delayMs = await PollCycleAsync(ct);
                if (State == AcquisitionState.Faulted)
                    State = AcquisitionState.Active;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                State = AcquisitionState.Faulted;
                LastError = ex.Message;
                logger.LogError(ex, "Error en ciclo de adquisición de hardware: {Message}", ex.Message);
                Raise("CycleError", null, $"Error en ciclo de adquisición: {ex.Message}");
            }

            try
            {
                await Task.Delay(Math.Max(100, delayMs), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<int> PollCycleAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ICommunicationSettingsService>();
        var meterService = scope.ServiceProvider.GetRequiredService<IMeterService>();
        var measurementRepo = scope.ServiceProvider.GetRequiredService<IMeasurementRepository>();
        var statusRepo = scope.ServiceProvider.GetRequiredService<IMeterConnectionStatusRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var settings = await settingsService.GetSettingsAsync(ct);
        CurrentPort = string.IsNullOrWhiteSpace(settings.ComPort) ? defaultOptions.ComPort : settings.ComPort;

        if (string.IsNullOrWhiteSpace(CurrentPort))
        {
            HardwareState = MeterHardwareState.ESPERANDO_COM;
            LastError = "No se ha configurado un puerto COM para la comunicación.";
            return settings.PollingInterval;
        }

        var allMeters = await meterService.GetAllAsync(ct);
        var enabledMeters = allMeters.Where(m => m.IsEnabled).ToList();
        ActiveMetersCount = enabledMeters.Count;

        if (enabledMeters.Count == 0)
        {
            HardwareState = MeterHardwareState.NO_CONFIGURADO;
            return settings.PollingInterval;
        }

        var batch = new List<Measurement>();
        var now = DateTimeOffset.UtcNow;
        var anyConnected = false;

        foreach (var meter in enabledMeters)
        {
            var status = await statusRepo.GetOrCreateAsync(meter.Id, ct);
            meterStatuses[meter.Id] = status;
            var wasConnected = status.IsConnected;
            var sw = Stopwatch.GetTimestamp();

            try
            {
                HardwareState = MeterHardwareState.CONECTANDO;
                var client = clients.Create(meter);

                // If registerMap has group definitions, poll them
                if (registerMap.Groups.Count > 0)
                {
                    foreach (var group in registerMap.Groups)
                    {
                        var request = new ModbusRequest(
                            meter.ModbusAddress,
                            group.FunctionCode,
                            [(byte)(group.Address >> 8), (byte)group.Address, (byte)(group.Quantity >> 8), (byte)group.Quantity]);

                        var response = await client.SendAsync(request, ct);

                        // Decode measurements according to TOV452 register map entries
                        var parsedMeasurements = registerMap.ParseResponse(meter, group, response.Data, now);
                        batch.AddRange(parsedMeasurements);
                        foreach (var m in parsedMeasurements)
                        {
                            latest[m.MeterId] = m;
                            MeasurementReceived?.Invoke(this, m);
                        }
                    }
                }
                else
                {
                    // Ping TOV452 with holding register 0x0000 read to confirm device presence
                    var pingRequest = new ModbusRequest(
                        meter.ModbusAddress,
                        0x03,
                        [0x00, 0x00, 0x00, 0x01]);

                    await client.SendAsync(pingRequest, ct);
                }

                anyConnected = true;
                HardwareState = batch.Count > 0 ? MeterHardwareState.ADQUIRIENDO : MeterHardwareState.CONECTADO;
                status.MarkSuccess(now, (int)Stopwatch.GetElapsedTime(sw).TotalMilliseconds);
                await statusRepo.SaveAsync(status, ct);

                if (!wasConnected)
                {
                    logger.LogInformation("Medidor '{MeterName}' (ID {MeterId}, Addr {Address}) conectado exitosamente en {Port}.", meter.Name, meter.Id, meter.ModbusAddress, CurrentPort);
                    Raise("MeterConnected", meter.Id, $"Medidor '{meter.Name}' conectado.");
                }

                Raise("MeterStatusChanged", meter.Id, status.State);
            }
            catch (Exception ex)
            {
                var errorMsg = ex is ModbusException me ? me.Message : ex.Message;
                status.MarkFailure(now, errorMsg);
                await statusRepo.SaveAsync(status, ct);

                if (wasConnected)
                {
                    logger.LogWarning("Conexión perdida con medidor '{MeterName}' (ID {MeterId}): {Error}. Reintentando automáticamente...", meter.Name, meter.Id, errorMsg);
                    Raise("MeterDisconnected", meter.Id, $"Conexión perdida con medidor '{meter.Name}'.");
                }
                else
                {
                    logger.LogDebug("Esperando medidor '{MeterName}' (ID {MeterId}, Addr {Address}) en {Port}...", meter.Name, meter.Id, meter.ModbusAddress, CurrentPort);
                }

                Raise("MeterStatusChanged", meter.Id, status.State);
            }
        }

        if (!anyConnected)
        {
            HardwareState = enabledMeters.Any(m => meterStatuses.TryGetValue(m.Id, out var st) && st.ConsecutiveFailures > 1)
                ? MeterHardwareState.DESCONECTADO
                : MeterHardwareState.ESPERANDO_MEDIDOR;
        }

        if (batch.Count > 0)
        {
            await measurementRepo.AddRangeAsync(batch, ct);
            await unitOfWork.SaveChangesAsync(ct);
            TotalReadingsProcessed += batch.Count;
            LastAcquisitionTime = now;
            LastError = null;
            logger.LogDebug("Guardado lote de {Count} mediciones reales en MySQL.", batch.Count);
        }

        return settings.PollingInterval;
    }

    private void Raise(string type, int? meterId, string message) =>
        EventRaised?.Invoke(this, new AcquisitionEvent(type, meterId, message, DateTimeOffset.UtcNow));

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        lifecycle.Dispose();
    }
}
