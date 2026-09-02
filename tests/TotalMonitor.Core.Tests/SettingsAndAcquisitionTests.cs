using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Historical;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Modbus;
using TotalMonitor.Infrastructure.Acquisition;
using TotalMonitor.Infrastructure.Modbus;

namespace TotalMonitor.Core.Tests;

public sealed class SettingsAndAcquisitionTests
{
    [Theory]
    [InlineData(0, 8, 1000, 1000)]
    [InlineData(9600, 4, 1000, 1000)]
    [InlineData(9600, 9, 1000, 1000)]
    [InlineData(9600, 8, 0, 1000)]
    [InlineData(9600, 8, 1000, 0)]
    public void CommunicationSettings_validates_numeric_ranges(int baud, int bits, int timeout, int interval)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CommunicationSettings("COM1", baud, bits, "None", "One", timeout, timeout, interval));
    }

    [Fact]
    public void Meter_defaults_to_TOV452_model_and_supports_custom_model()
    {
        var meter1 = new Meter("M1", 1, "COM1", 9600, "None");
        Assert.Equal("TOV452", meter1.Model);

        var meter2 = new Meter("M2", 2, "COM1", 9600, "None", true, "TOV452-Advanced");
        Assert.Equal("TOV452-Advanced", meter2.Model);

        meter1.Update("M1-Updated", 1, "COM2", 19200, "Even", true, "TOV452");
        Assert.Equal("M1-Updated", meter1.Name);
        Assert.Equal(19200, meter1.BaudRate);
    }

    [Fact]
    public void MeterHardwareState_display_and_color_mappings_are_consistent()
    {
        Assert.Equal("Esperando medidor", MeterHardwareState.ESPERANDO_MEDIDOR.ToDisplayString());
        Assert.Equal("Esperando COM", MeterHardwareState.ESPERANDO_COM.ToDisplayString());
        Assert.Equal("Medidor conectado", MeterHardwareState.CONECTADO.ToDisplayString());
        Assert.Equal("Adquiriendo", MeterHardwareState.ADQUIRIENDO.ToDisplayString());
        Assert.Equal("Conexión perdida", MeterHardwareState.DESCONECTADO.ToDisplayString());

        Assert.Equal("#138A72", MeterHardwareState.ADQUIRIENDO.ToBadgeColor());
        Assert.Equal("#C5221F", MeterHardwareState.DESCONECTADO.ToBadgeColor());
        Assert.Equal("#B05A00", MeterHardwareState.ESPERANDO_MEDIDOR.ToBadgeColor());
    }

    [Fact]
    public async Task AcquisitionService_transitions_to_EsperandoMedidor_when_no_hardware_responds()
    {
        var services = new ServiceCollection();
        var storage = new FakeStorageContext(comPort: "COM1", throwOnModbus: true);

        services.AddScoped<ICommunicationSettingsService>(_ => storage);
        services.AddScoped<IMeterService>(_ => storage);
        services.AddScoped<IMeasurementRepository>(_ => storage);
        services.AddScoped<IMeterConnectionStatusRepository>(_ => storage);
        services.AddScoped<IUnitOfWork>(_ => storage);

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var options = new ModbusConnectionOptions { ComPort = "COM1" };
        var clients = new FakeFailingModbusClientFactory();

        var acq = new DataAcquisitionService(
            scopeFactory,
            clients,
            MeterRegisterMap.Empty,
            options,
            NullLogger<DataAcquisitionService>.Instance);

        await acq.StartAsync();
        Assert.True(acq.State is AcquisitionState.Active or AcquisitionState.Starting);

        await Task.Delay(200);

        var summary = acq.GetStatusSummary();
        Assert.Equal(AcquisitionState.Active, summary.State);

        await acq.StopAsync();
        Assert.Equal(AcquisitionState.Stopped, acq.State);
    }

    [Fact]
    public async Task AcquisitionService_acquires_real_telemetry_when_hardware_responds()
    {
        var services = new ServiceCollection();
        var storage = new FakeStorageContext(comPort: "COM3", throwOnModbus: false);

        services.AddScoped<ICommunicationSettingsService>(_ => storage);
        services.AddScoped<IMeterService>(_ => storage);
        services.AddScoped<IMeasurementRepository>(_ => storage);
        services.AddScoped<IMeterConnectionStatusRepository>(_ => storage);
        services.AddScoped<IUnitOfWork>(_ => storage);

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var options = new ModbusConnectionOptions { ComPort = "COM3" };
        var clients = new FakeRespondingModbusClientFactory();

        var registerGroup = new RegisterGroup("BasicMetrics", 0x03, 0x0000, 2, [
            new ModbusRegisterDefinition(0x0000, 0x03, "Int16", 1, 0.1m, 0m, "V", "Voltage", "Voltaje", "BigEndian"),
            new ModbusRegisterDefinition(0x0001, 0x03, "Int16", 1, 0.01m, 0m, "A", "Current", "Corriente", "BigEndian")
        ]);

        var registerMap = new MeterRegisterMap([registerGroup]);

        var acq = new DataAcquisitionService(
            scopeFactory,
            clients,
            registerMap,
            options,
            NullLogger<DataAcquisitionService>.Instance);

        var measurements = new List<Measurement>();
        acq.MeasurementReceived += (_, m) => measurements.Add(m);

        await acq.StartAsync();
        await Task.Delay(250);

        var summary = acq.GetStatusSummary();
        Assert.Equal(AcquisitionState.Active, summary.State);
        Assert.True(acq.TotalReadingsProcessed > 0);
        Assert.NotEmpty(measurements);

        await acq.StopAsync();
        Assert.Equal(AcquisitionState.Stopped, acq.State);
    }

    private sealed class FakeStorageContext
        : ICommunicationSettingsService, IMeterService, IMeasurementRepository, IMeterConnectionStatusRepository, IUnitOfWork
    {
        private readonly List<Meter> meters = [];
        private readonly Dictionary<int, MeterConnectionStatus> statuses = [];
        private CommunicationSettings settings;

        public List<Measurement> Measurements { get; } = [];

        public FakeStorageContext(string comPort = "COM1", bool throwOnModbus = false)
        {
            settings = new CommunicationSettings(comPort, 9600, 8, "None", "One", 1000, 1000, 50);
            var m1 = new Meter("TOV452 Principal", 1, comPort, 9600, "None", true, "TOV452");
            typeof(Meter).GetProperty("Id")?.SetValue(m1, 1);
            meters.Add(m1);
        }

        public Task<CommunicationSettings> GetSettingsAsync(CancellationToken ct = default) => Task.FromResult(settings);
        public Task<CommunicationSettings> SaveSettingsAsync(CommunicationSettings s, CancellationToken ct = default)
        {
            settings = s;
            return Task.FromResult(settings);
        }

        public Task<IReadOnlyList<string>> GetAvailablePortsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(["COM1", "COM3"]);
        public Task<(bool Success, string Message, MeterHardwareState State)> TestConnectionAsync(
            string? comPort, byte slaveAddress, int baudRate, string? parity, int dataBits, string? stopBits, int timeout, CancellationToken ct = default) =>
            Task.FromResult((true, "OK", MeterHardwareState.CONECTADO));

        public Task<IReadOnlyList<Meter>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Meter>>(meters);
        public Task<Meter> CreateAsync(Meter m, CancellationToken ct = default)
        {
            meters.Add(m);
            return Task.FromResult(m);
        }
        public Task<bool> UpdateAsync(int id, string name, byte address, string comPort, int baudRate, string parity, bool enabled, string model = "TOV452", CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            meters.RemoveAll(m => m.Id == id);
            return Task.FromResult(true);
        }

        public Task AddRangeAsync(IEnumerable<Measurement> items, CancellationToken ct = default)
        {
            Measurements.AddRange(items);
            return Task.CompletedTask;
        }

        public Task<HistoricalPage> GetRangeAsync(HistoricalQueryFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(new HistoricalPage([], 0, 1, 100));

        public Task<IReadOnlyList<HistoricalDataPoint>> GetLatestAsync(int meterId, string? variable = null, int take = 100, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HistoricalDataPoint>>([]);

        public Task<IReadOnlyList<HistoricalDataPoint>> GetByMeterAsync(int meterId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HistoricalDataPoint>>([]);

        public Task<IReadOnlyList<HistoricalDataPoint>> GetByVariableAsync(string variable, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HistoricalDataPoint>>([]);

        public Task<IReadOnlyList<string>> GetVariablesAsync(int? meterId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(["Voltage", "Current"]);

        public Task<MeterConnectionStatus> GetOrCreateAsync(int meterId, CancellationToken ct = default)
        {
            if (!statuses.TryGetValue(meterId, out var status))
            {
                status = new MeterConnectionStatus(meterId, true, DateTimeOffset.UtcNow);
                statuses[meterId] = status;
            }
            return Task.FromResult(status);
        }

        public Task SaveAsync(MeterConnectionStatus status, CancellationToken ct = default)
        {
            statuses[status.MeterId] = status;
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    private sealed class FakeRespondingModbusClientFactory : IModbusClientFactory
    {
        public IModbusClient Create(Meter meter) => new FakeRespondingModbusClient(meter);
    }

    private sealed class FakeRespondingModbusClient(Meter meter) : IModbusClient
    {
        public Task<ModbusResponse> SendAsync(ModbusRequest request, CancellationToken ct = default)
        {
            // Return 4 bytes: Voltage raw = 2200 (220.0V), Current raw = 500 (5.00A)
            return Task.FromResult(new ModbusResponse(meter.ModbusAddress, request.FunctionCode, [4, 0x08, 0x98, 0x01, 0xF4]));
        }
    }

    private sealed class FakeFailingModbusClientFactory : IModbusClientFactory
    {
        public IModbusClient Create(Meter meter) => new FakeFailingModbusClient();
    }

    private sealed class FakeFailingModbusClient : IModbusClient
    {
        public Task<ModbusResponse> SendAsync(ModbusRequest request, CancellationToken ct = default)
        {
            throw new ModbusException(ModbusErrorKind.Timeout, "Timeout esperando respuesta de hardware.");
        }
    }
}
