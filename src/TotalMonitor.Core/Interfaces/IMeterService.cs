using TotalMonitor.Core.Entities;

namespace TotalMonitor.Core.Interfaces;

public interface IMeterService
{
    Task<IReadOnlyList<Meter>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Meter> CreateAsync(Meter meter, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, string name, byte address, string comPort, int baudRate, string parity, bool enabled, string model = "TOV452", CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
