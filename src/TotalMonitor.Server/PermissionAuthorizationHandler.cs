using Microsoft.AspNetCore.Authorization;
namespace TotalMonitor.Server;
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement { public string Permission { get; } = permission; }
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement> { protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement) { if (context.User.HasClaim("permission", requirement.Permission)) context.Succeed(requirement); return Task.CompletedTask; } }
