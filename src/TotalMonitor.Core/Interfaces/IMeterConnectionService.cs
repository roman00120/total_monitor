using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Modbus;
namespace TotalMonitor.Core.Interfaces;
public enum MeterConnectionState { Disconnected, Connecting, Connected, CommunicationError, Timeout, InvalidResponse }
public sealed record ConnectionTestResult(bool Success, MeterConnectionState State, ModbusErrorKind? ErrorKind, string Message);
public interface IMeterConnectionService { Task<ConnectionTestResult> TestConnectionAsync(Meter meter, ModbusConnectionOptions options, CancellationToken cancellationToken = default); }
