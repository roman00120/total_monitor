using TotalMonitor.Core.Entities;
namespace TotalMonitor.Core.Interfaces;
public interface IUserAdminService { Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default); Task<User> CreateAsync(string username, string displayName, string password, string role, CancellationToken cancellationToken = default); Task SetActiveAsync(int userId, bool active, CancellationToken cancellationToken = default); Task ResetPasswordAsync(int userId, string temporaryPassword, CancellationToken cancellationToken = default); }
