using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Security;
using TotalMonitor.Infrastructure.Persistence;

namespace TotalMonitor.Infrastructure.Security;

public sealed class PasswordHasher
{
    private const int SaltSize = 16, HashSize = 32, Iterations = 120_000;
    public (string Hash, string Salt) Hash(string password) { Validate(password); var salt = RandomNumberGenerator.GetBytes(SaltSize); var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, HashSize); return (Convert.ToBase64String(hash), Convert.ToBase64String(salt)); }
    public bool Verify(string password, string hash, string salt) { if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt)) return false; try { var expected = Convert.FromBase64String(hash); var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), Iterations, HashAlgorithmName.SHA512, HashSize); return CryptographicOperations.FixedTimeEquals(expected, actual); } catch (FormatException) { return false; } }
    public static void Validate(string password) { if (string.IsNullOrWhiteSpace(password) || password.Length < 10) throw new ArgumentException("La contraseña debe tener al menos 10 caracteres.", nameof(password)); }
}

public sealed class CurrentUserService : ICurrentUserService
{ public AuthenticatedUser? User { get; private set; } public bool IsAuthenticated => User is not null; public void SetUser(AuthenticatedUser user) => User = user; public void Clear() => User = null; }

public sealed class AuthorizationService(ICurrentUserService currentUser) : IAuthorizationService
{ public bool HasPermission(string permission) => currentUser.User?.Permissions.Contains(permission) == true; public bool HasAnyPermission(params string[] permissions) => permissions.Any(HasPermission); public bool HasAllPermissions(params string[] permissions) => permissions.All(HasPermission); public void Demand(string permission) { if (!HasPermission(permission)) throw new UnauthorizedAccessException("No tienes permisos para realizar esta acción."); } }

public sealed class AuthenticationService(TotalMonitorDbContext db, PasswordHasher hasher, ICurrentUserService currentUser, IAuditService audit, ILogger<AuthenticationService> logger) : IAuthenticationService
{
    public bool IsAuthenticated => currentUser.IsAuthenticated;
    public async Task<(bool Success, string Message, AuthenticatedUser? User)> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var normalized = username.Trim(); var user = await db.Users.SingleOrDefaultAsync(x => x.UserName == normalized, ct); var valid = user is not null && user!.IsActive && (user.LockoutUntil is null || user.LockoutUntil <= DateTimeOffset.UtcNow) && hasher.Verify(password, user.PasswordHash, user.PasswordSalt);
        if (!valid) { if (user is not null) { user.MarkLoginFailure(DateTimeOffset.UtcNow, user.FailedLoginCount >= 4 ? DateTimeOffset.UtcNow.AddMinutes(5) : null); await db.SaveChangesAsync(ct); } await audit.RecordAsync("LoginFailed", $"Usuario: {normalized}", ct); logger.LogWarning("Login failed for user {Username}.", normalized); return (false, "Usuario o contraseña incorrectos.", null); }
        var roles = await db.UserRoles.Where(x => x.UserId == user!.Id).Join(db.Roles, x => x.RoleId, x => x.Id, (_, role) => role.Name).ToListAsync(ct); var role = roles.FirstOrDefault() ?? RoleNames.Viewer; var permissionList = await db.UserRoles.Where(x => x.UserId == user!.Id).Join(db.RolePermissions, x => x.RoleId, y => y.RoleId, (_, y) => y.PermissionId).Join(db.Permissions, id => id, p => p.Id, (_, p) => p.Name).ToListAsync(ct); var permissions = permissionList.ToHashSet(); user!.MarkLoginSuccess(DateTimeOffset.UtcNow); await db.SaveChangesAsync(ct); var authenticated = new AuthenticatedUser(user.Id, user.UserName, user.DisplayName, role, permissions); currentUser.SetUser(authenticated); await audit.RecordAsync("LoginSucceeded", "Sesión iniciada", ct); return (true, "Sesión iniciada correctamente.", authenticated);
    }
    public async Task LogoutAsync(CancellationToken ct = default) { if (currentUser.User is not null) await audit.RecordAsync("Logout", "Sesión cerrada", ct); currentUser.Clear(); }
    public Task<AuthenticatedUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default) => Task.FromResult(currentUser.User);
}

public sealed class AuditService(TotalMonitorDbContext db, ICurrentUserService currentUser) : IAuditService
{ public async Task RecordAsync(string action, string description, CancellationToken ct = default) { db.AuditLogs.Add(new AuditLog(currentUser.User?.UserId, action, description)); await db.SaveChangesAsync(ct); } }

public sealed class UserAdminService(TotalMonitorDbContext db, PasswordHasher hasher, IAuthorizationService authorization, IAuditService audit) : IUserAdminService
{
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) { authorization.Demand(PermissionNames.UsersView); return await db.Users.AsNoTracking().OrderBy(x => x.UserName).ToListAsync(ct); }
    public async Task<User> CreateAsync(string username, string displayName, string password, string role, CancellationToken ct = default) { authorization.Demand(PermissionNames.UsersCreate); if (await db.Users.AnyAsync(x => x.UserName == username.Trim(), ct)) throw new InvalidOperationException("El nombre de usuario ya existe."); PasswordHasher.Validate(password); var credentials = hasher.Hash(password); var user = new User(username, displayName, credentials.Hash, credentials.Salt); db.Users.Add(user); await db.SaveChangesAsync(ct); var roleEntity = await db.Roles.SingleAsync(x => x.Name == role, ct); db.UserRoles.Add(new UserRole(user.Id, roleEntity.Id)); await db.SaveChangesAsync(ct); await audit.RecordAsync("UserCreated", $"Usuario: {user.UserName}", ct); return user; }
    public async Task SetActiveAsync(int userId, bool active, CancellationToken ct = default) { authorization.Demand(PermissionNames.UsersEdit); var user = await db.Users.FindAsync([userId], ct) ?? throw new KeyNotFoundException("Usuario no encontrado."); user.SetActive(active); await db.SaveChangesAsync(ct); await audit.RecordAsync(active ? "UserActivated" : "UserDeactivated", $"Usuario: {user.UserName}", ct); }
    public async Task ResetPasswordAsync(int userId, string temporaryPassword, CancellationToken ct = default) { authorization.Demand(PermissionNames.UsersEdit); PasswordHasher.Validate(temporaryPassword); var user = await db.Users.FindAsync([userId], ct) ?? throw new KeyNotFoundException("Usuario no encontrado."); var credentials = hasher.Hash(temporaryPassword); user.SetPassword(credentials.Hash, credentials.Salt); await db.SaveChangesAsync(ct); await audit.RecordAsync("PasswordReset", $"Usuario: {user.UserName}", ct); }
}
