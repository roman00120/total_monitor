namespace TotalMonitor.Core.Entities;
public sealed class Permission { public int Id { get; private set; } public string Name { get; private set; } = string.Empty; private Permission() { } public Permission(string name) { if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Permission name is required.", nameof(name)); Name = name.Trim(); } }
