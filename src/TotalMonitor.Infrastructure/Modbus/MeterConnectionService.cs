using Microsoft.Extensions.Logging;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Modbus;

namespace TotalMonitor.Infrastructure.Modbus;

public sealed class MeterConnectionService(ILogger<MeterConnectionService> logger) : IMeterConnectionService
{
    public Task<ConnectionTestResult> TestConnectionAsync(Meter meter, ModbusConnectionOptions options, CancellationToken cancellationToken = default)
    {
        if (!meter.IsEnabled) return Task.FromResult(new ConnectionTestResult(false, MeterConnectionState.Disconnected, null, "El medidor está deshabilitado."));
        logger.LogWarning("[Modbus] No communication test sent for {Meter}; no TOV452 request is documented.", meter.Name);
        return Task.FromResult(new ConnectionTestResult(false, MeterConnectionState.Disconnected, null, "Sin conexión con el dispositivo. Falta una petición TOV452 documentada para probarlo."));
    }
}
