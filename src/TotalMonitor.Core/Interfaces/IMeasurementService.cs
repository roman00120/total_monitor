using TotalMonitor.Core.Entities;
namespace TotalMonitor.Core.Interfaces;
public interface IMeasurementService { Task<IReadOnlyList<Measurement>> GetByMeterAsync(int meterId, CancellationToken cancellationToken = default); }
