using Microsoft.EntityFrameworkCore;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Infrastructure.Modbus;
using TotalMonitor.Infrastructure.Persistence;

namespace TotalMonitor.Infrastructure.Services;

public sealed class CommunicationSettingsService(TotalMonitorDbContext db) : ICommunicationSettingsService
{
    public async Task<CommunicationSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var settings = await db.CommunicationSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = CommunicationSettings.CreateDefault();
            db.CommunicationSettings.Add(settings);
            await db.SaveChangesAsync(ct);
        }
        return settings;
    }

    public async Task<CommunicationSettings> SaveSettingsAsync(CommunicationSettings settings, CancellationToken ct = default)
    {
        var existing = await db.CommunicationSettings.FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            db.CommunicationSettings.Add(settings);
        }
        else
        {
            existing.Update(
                settings.ComPort,
                settings.BaudRate,
                settings.DataBits,
                settings.Parity,
                settings.StopBits,
                settings.ReadTimeout,
                settings.WriteTimeout,
                settings.PollingInterval);
        }

        await db.SaveChangesAsync(ct);
        return existing ?? settings;
    }

    public Task<IReadOnlyList<string>> GetAvailablePortsAsync(CancellationToken ct = default) =>
        Task.FromResult(SerialPortService.GetAvailablePorts());

    public Task<(bool Success, string Message, MeterHardwareState State)> TestConnectionAsync(
        string? comPort,
        byte slaveAddress,
        int baudRate,
        string? parity,
        int dataBits,
        string? stopBits,
        int timeout,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(comPort))
            return Task.FromResult((false, "No se especificó un puerto COM para probar.", MeterHardwareState.ESPERANDO_COM));

        return Task.FromResult(SerialPortService.TestMeterModbusCommunication(
            comPort,
            slaveAddress,
            baudRate,
            parity ?? "None",
            dataBits,
            stopBits ?? "One",
            timeout));
    }
}
