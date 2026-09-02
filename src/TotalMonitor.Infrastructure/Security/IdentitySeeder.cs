using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Security;
using TotalMonitor.Infrastructure.Persistence;

namespace TotalMonitor.Infrastructure.Security;
public sealed class IdentitySeeder(TotalMonitorDbContext db, PasswordHasher hasher, ILogger<IdentitySeeder> logger)
{
    public async Task SeedRolesAndPermissionsAsync(CancellationToken ct = default)
    {
        var names = new[] { RoleNames.Administrator, RoleNames.Technician, RoleNames.Viewer }; foreach (var name in names) if (!await db.Roles.AnyAsync(x => x.Name == name, ct)) db.Roles.Add(new Role(name));
        var permissionNames = new[] { PermissionNames.DashboardView, PermissionNames.MetersView, PermissionNames.MetersCreate, PermissionNames.MetersEdit, PermissionNames.MetersDelete, PermissionNames.MonitoringView, PermissionNames.HistoryView, PermissionNames.ReportsView, PermissionNames.ReportsExport, PermissionNames.UsersView, PermissionNames.UsersCreate, PermissionNames.UsersEdit, PermissionNames.UsersDelete, PermissionNames.SettingsView, PermissionNames.SettingsEdit }; foreach (var name in permissionNames) if (!await db.Permissions.AnyAsync(x => x.Name == name, ct)) db.Permissions.Add(new Permission(name)); await db.SaveChangesAsync(ct);
        var roles = await db.Roles.ToDictionaryAsync(x => x.Name, ct); var permissions = await db.Permissions.ToDictionaryAsync(x => x.Name, ct); var technician = permissionNames.Where(x => x is not PermissionNames.UsersView and not PermissionNames.UsersCreate and not PermissionNames.UsersEdit and not PermissionNames.UsersDelete).ToArray(); var viewer = new[] { PermissionNames.DashboardView, PermissionNames.MonitoringView, PermissionNames.HistoryView, PermissionNames.ReportsView }; var all = permissionNames;
        foreach (var pair in new[] { (RoleNames.Administrator, all), (RoleNames.Technician, technician), (RoleNames.Viewer, viewer) }) foreach (var permission in pair.Item2) if (!await db.RolePermissions.AnyAsync(x => x.RoleId == roles[pair.Item1].Id && x.PermissionId == permissions[permission].Id, ct)) db.RolePermissions.Add(new RolePermission(roles[pair.Item1].Id, permissions[permission].Id)); await db.SaveChangesAsync(ct);
    }
    public async Task EnsureInitialAdministratorAsync(string username, string displayName, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Initial administrator was not created because InitialAdminUsername or InitialAdminPassword is not configured.");
            return;
        }

        await SeedRolesAndPermissionsAsync(ct);
        var administratorRole = await db.Roles.SingleAsync(x => x.Name == RoleNames.Administrator, ct);
        var administratorExists = await db.UserRoles.AnyAsync(
            x => x.RoleId == administratorRole.Id && db.Users.Any(user => user.Id == x.UserId),
            ct);
        if (administratorExists)
        {
            logger.LogInformation("Initial administrator seeding skipped because an administrator already exists.");
            return;
        }

        var credentials = hasher.Hash(password);
        var user = new User(username, displayName, credentials.Hash, credentials.Salt);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        db.UserRoles.Add(new UserRole(user.Id, administratorRole.Id));
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Initial administrator {Username} created.", user.UserName);
    }
}
