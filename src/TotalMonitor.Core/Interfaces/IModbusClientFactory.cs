using TotalMonitor.Core.Entities;
namespace TotalMonitor.Core.Interfaces;
public interface IModbusClientFactory { IModbusClient Create(Meter meter); }
