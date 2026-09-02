using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Modbus;
namespace TotalMonitor.Infrastructure.Modbus;
public sealed class ModbusRegisterReader(IModbusClient client) : IModbusRegisterReader
{
    public Task<ModbusResponse> ReadHoldingRegistersAsync(byte slave, ushort address, ushort quantity, CancellationToken ct = default) => ReadAsync(slave, 3, address, quantity, ct);
    public Task<ModbusResponse> ReadInputRegistersAsync(byte slave, ushort address, ushort quantity, CancellationToken ct = default) => ReadAsync(slave, 4, address, quantity, ct);
    public Task<ModbusResponse> ReadCoilsAsync(byte slave, ushort address, ushort quantity, CancellationToken ct = default) => ReadAsync(slave, 1, address, quantity, ct);
    public Task<ModbusResponse> ReadDiscreteInputsAsync(byte slave, ushort address, ushort quantity, CancellationToken ct = default) => ReadAsync(slave, 2, address, quantity, ct);
    private Task<ModbusResponse> ReadAsync(byte slave, byte function, ushort address, ushort quantity, CancellationToken ct) => client.SendAsync(new ModbusRequest(slave, function, [(byte)(address >> 8), (byte)address, (byte)(quantity >> 8), (byte)quantity]), ct);
}
