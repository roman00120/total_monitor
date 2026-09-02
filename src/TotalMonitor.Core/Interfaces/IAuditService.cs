using TotalMonitor.Core.Entities;
namespace TotalMonitor.Core.Interfaces;
public interface IAuditService { Task RecordAsync(string action, string description, CancellationToken cancellationToken = default); }
