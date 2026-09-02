using TotalMonitor.Core.Entities;

namespace TotalMonitor.Core.Interfaces;

public interface ICommunicationSettingsService
{
    Task<CommunicationSettings> GetSettingsAsync(CancellationToken ct = default);
    Task<CommunicationSettings> SaveSettingsAsync(CommunicationSettings settings, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAvailablePortsAsync(CancellationToken ct = default);
    Task<(bool Success, string Message, MeterHardwareState State)> TestConnectionAsync(
        string? comPort,
        byte slaveAddress,
        int baudRate,
        string? parity,
        int dataBits,
        string? stopBits,
        int timeout,
        CancellationToken ct = default);
}
