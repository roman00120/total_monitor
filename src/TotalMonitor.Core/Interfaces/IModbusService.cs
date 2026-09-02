using TotalMonitor.Core.Modbus;
namespace TotalMonitor.Core.Interfaces;
public interface IModbusService { Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default); Task<ModbusResponse> SendAsync(ModbusRequest request, CancellationToken cancellationToken = default); }
