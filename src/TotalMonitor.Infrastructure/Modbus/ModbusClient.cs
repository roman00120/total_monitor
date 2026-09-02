using Microsoft.Extensions.Logging;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Modbus;

namespace TotalMonitor.Infrastructure.Modbus;

public sealed class ModbusClient(IModbusTransport transport, ILogger<ModbusClient> logger) : IModbusClient, IModbusService
{
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(transport.IsOpen);
    public async Task<ModbusResponse> SendAsync(ModbusRequest request, CancellationToken ct = default)
    {
        var frame = await transport.ExchangeAsync(request.ToFrame(), ct); if (frame.Length >= 2 && frame[1] == (byte)(request.FunctionCode | 0x80)) { var code = frame.Length > 2 ? frame[2] : (byte?)null; throw new ModbusException(ModbusErrorKind.ProtocolException, "The Modbus device returned an exception response.", code); }
        var response = ModbusResponse.Parse(frame, request.SlaveAddress, request.FunctionCode); logger.LogDebug("[Modbus] CRC validated."); return response;
    }
}
