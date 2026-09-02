namespace TotalMonitor.Core.Interfaces;
public interface IModbusTransport : IAsyncDisposable { bool IsOpen { get; } Task OpenAsync(CancellationToken cancellationToken = default); Task CloseAsync(); Task<byte[]> ExchangeAsync(ReadOnlyMemory<byte> request, CancellationToken cancellationToken = default); }
