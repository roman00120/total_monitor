using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Modbus;
namespace TotalMonitor.Infrastructure.Modbus;
public sealed class ModbusClientFactory(ILoggerFactory loggerFactory, ModbusConnectionOptions defaults) : IModbusClientFactory
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Channels = new(StringComparer.OrdinalIgnoreCase);
    public IModbusClient Create(Meter meter)
    {
        var options = new ModbusConnectionOptions { ComPort = meter.ComPort, BaudRate = meter.BaudRate, DataBits = defaults.DataBits, Parity = meter.Parity, StopBits = defaults.StopBits, ReadTimeout = defaults.ReadTimeout, WriteTimeout = defaults.WriteTimeout, RetryCount = defaults.RetryCount, RetryDelay = defaults.RetryDelay, PollingInterval = defaults.PollingInterval };
        var transport = new SerialModbusTransport(new SerialPortService(options), options, loggerFactory.CreateLogger<SerialModbusTransport>());
        return new ChannelModbusClient(new ModbusClient(transport, loggerFactory.CreateLogger<ModbusClient>()), transport, Channels.GetOrAdd(meter.ComPort, _ => new SemaphoreSlim(1, 1)));
    }
    private sealed class ChannelModbusClient(IModbusClient client, IModbusTransport transport, SemaphoreSlim gate) : IModbusClient
    {
        public async Task<ModbusResponse> SendAsync(ModbusRequest request, CancellationToken ct = default)
        { await gate.WaitAsync(ct); try { await transport.OpenAsync(ct); return await client.SendAsync(request, ct); } finally { await transport.CloseAsync(); gate.Release(); await transport.DisposeAsync(); } }
    }
}
