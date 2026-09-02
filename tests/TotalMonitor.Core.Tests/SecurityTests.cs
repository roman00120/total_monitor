using TotalMonitor.Core.Security;
using TotalMonitor.Infrastructure.Security;

namespace TotalMonitor.Core.Tests;
public sealed class SecurityTests
{
    [Fact] public void Password_hash_is_not_plain_text_and_verifies() { var hasher = new PasswordHasher(); var result = hasher.Hash("Strong-password-1"); Assert.NotEqual("Strong-password-1", result.Hash); Assert.True(hasher.Verify("Strong-password-1", result.Hash, result.Salt)); Assert.False(hasher.Verify("wrong-password", result.Hash, result.Salt)); }
    [Fact] public void Weak_password_is_rejected() => Assert.Throws<ArgumentException>(() => PasswordHasher.Validate("short"));
    [Fact] public void Viewer_permissions_are_granular() { var current = new CurrentUserService(); current.SetUser(new AuthenticatedUser(1, "viewer", "Viewer", RoleNames.Viewer, new HashSet<string> { PermissionNames.DashboardView, PermissionNames.ReportsView })); var auth = new AuthorizationService(current); Assert.True(auth.HasPermission(PermissionNames.ReportsView)); Assert.False(auth.HasPermission(PermissionNames.ReportsExport)); Assert.True(auth.HasAllPermissions(PermissionNames.DashboardView, PermissionNames.ReportsView)); }
}
