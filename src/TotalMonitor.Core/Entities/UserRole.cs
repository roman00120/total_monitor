namespace TotalMonitor.Core.Entities;
public sealed class UserRole { public int UserId { get; private set; } public int RoleId { get; private set; } private UserRole() { } public UserRole(int userId, int roleId) { UserId = userId; RoleId = roleId; } }
