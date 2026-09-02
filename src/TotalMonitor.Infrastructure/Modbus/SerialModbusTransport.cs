using Microsoft.Extensions.Logging;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Modbus;

namespace TotalMonitor.Infrastructure.Modbus;

public sealed class SerialModbusTransport(ISerialPortService serialPort, ModbusConnectionOptions options, ILogger<SerialModbusTransport> logger) : IModbusTransport
{
    public bool IsOpen => serialPort.IsOpen;
    public Task OpenAsync(CancellationToken ct = default) { logger.LogInformation("[Modbus] Opening {ComPort}.", serialPort.PortName); return serialPort.OpenAsync(ct); }
    public Task CloseAsync() => serialPort.CloseAsync();
    public async Task<byte[]> ExchangeAsync(ReadOnlyMemory<byte> request, CancellationToken ct = default)
    {
        logger.LogDebug("[Modbus] Sending request."); await serialPort.WriteAsync(request, ct); logger.LogDebug("[Modbus] Waiting response.");
        var buffer = new byte[256]; var count = 0; using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(options.ReadTimeout);
        try { while (count < buffer.Length) { var read = await serialPort.ReadAsync(buffer.AsMemory(count), timeout.Token); if (read == 0) break; count += read; if (count >= 5 && IsCompleteReadFrame(buffer, count)) break; } }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ModbusException(ModbusErrorKind.Timeout, "The Modbus device did not respond within the configured timeout."); }
        catch (IOException ex) { throw new ModbusException(ModbusErrorKind.PortError, "The serial port could not be read.", innerException: ex); }
        if (count < 5) throw new ModbusException(ModbusErrorKind.IncompleteResponse, "The Modbus response is incomplete."); logger.LogDebug("[Modbus] Response received."); return buffer[..count];
    }
    private static bool IsCompleteReadFrame(byte[] buffer, int count) => buffer[1] >= 1 && buffer[1] <= 4 ? count >= 5 + buffer[2] : count >= 8;
    public async ValueTask DisposeAsync() { await serialPort.DisposeAsync(); }
}
