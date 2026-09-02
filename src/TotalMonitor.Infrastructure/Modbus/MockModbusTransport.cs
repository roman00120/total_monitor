using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.Infrastructure.Modbus;

/// <summary>Explicit simulator. It never represents a physical TOV452 connection.</summary>
public sealed class MockModbusTransport(Func<byte[], byte[]>? responder = null, bool timeout = false) : IModbusTransport
{
    public bool IsOpen { get; private set; }
    public List<byte[]> Requests { get; } = [];
    public Task OpenAsync(CancellationToken cancellationToken = default) { IsOpen = true; return Task.CompletedTask; }
    public Task CloseAsync() { IsOpen = false; return Task.CompletedTask; }
    public async Task<byte[]> ExchangeAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken = default)
    { if (!IsOpen) throw new InvalidOperationException("SIMULATOR is not open."); Requests.Add(request.ToArray()); if (timeout) await Task.Delay(Timeout.Infinite, cancellationToken); return responder?.Invoke(request.ToArray()) ?? []; }
    public ValueTask DisposeAsync() { IsOpen = false; return ValueTask.CompletedTask; }
}
