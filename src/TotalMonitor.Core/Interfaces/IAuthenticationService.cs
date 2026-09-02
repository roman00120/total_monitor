using TotalMonitor.Core.Security;
namespace TotalMonitor.Core.Interfaces;
public interface IAuthenticationService { Task<(bool Success, string Message, AuthenticatedUser? User)> LoginAsync(string username, string password, CancellationToken cancellationToken = default); Task LogoutAsync(CancellationToken cancellationToken = default); Task<AuthenticatedUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default); bool IsAuthenticated { get; } }
public interface ICurrentUserService { AuthenticatedUser? User { get; } bool IsAuthenticated { get; } void SetUser(AuthenticatedUser user); void Clear(); }
public interface IAuthorizationService { bool HasPermission(string permission); bool HasAnyPermission(params string[] permissions); bool HasAllPermissions(params string[] permissions); void Demand(string permission); }
