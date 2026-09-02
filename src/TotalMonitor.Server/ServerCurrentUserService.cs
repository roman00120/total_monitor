using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Security;
namespace TotalMonitor.Server;
public sealed class ServerCurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private AuthenticatedUser? fallback;
    public AuthenticatedUser? User { get { var principal = accessor.HttpContext?.User; if (principal?.Identity?.IsAuthenticated != true) return fallback; var permissions = principal.FindAll("permission").Select(x => x.Value).ToHashSet(); return new AuthenticatedUser(int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0, principal.Identity.Name ?? "", principal.FindFirstValue("display_name") ?? principal.Identity.Name ?? "", principal.FindFirstValue(ClaimTypes.Role) ?? RoleNames.Viewer, permissions); } }
    public bool IsAuthenticated => User is not null;
    public void SetUser(AuthenticatedUser user) => fallback = user;
    public void Clear() => fallback = null;
}
