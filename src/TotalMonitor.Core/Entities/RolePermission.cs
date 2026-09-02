namespace TotalMonitor.Core.Entities;
public sealed class RolePermission { public int RoleId { get; private set; } public int PermissionId { get; private set; } private RolePermission() { } public RolePermission(int roleId, int permissionId) { RoleId = roleId; PermissionId = permissionId; } }
