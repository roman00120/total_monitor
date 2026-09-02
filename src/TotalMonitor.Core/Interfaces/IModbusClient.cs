using TotalMonitor.Core.Modbus;
namespace TotalMonitor.Core.Interfaces;
public interface IModbusClient { Task<ModbusResponse> SendAsync(ModbusRequest request, CancellationToken cancellationToken = default); }
