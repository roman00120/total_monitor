using TotalMonitor.Core.Entities;
namespace TotalMonitor.Core.Interfaces;
public interface IUserService { Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default); }
