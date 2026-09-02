namespace TotalMonitor.Core.Entities;
public sealed class Role { public int Id { get; private set; } public string Name { get; private set; } = string.Empty; private Role() { } public Role(string name) { if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Role name is required.", nameof(name)); Name = name.Trim(); } }
