using TotalMonitor.Core.Historical;
namespace TotalMonitor.Core.Interfaces;
public sealed record ReportRequest(DateTimeOffset From, DateTimeOffset To, int? MeterId = null, string? Variable = null, HistoricalResolution Resolution = HistoricalResolution.Automatic, int PageSize = 5000);
public sealed record ReportResult(ReportRequest Request, HistoricalPage Data, DateTimeOffset GeneratedAt, int? GeneratedByUserId);
public interface IReportService { Task<ReportResult> GenerateAsync(ReportRequest request, CancellationToken cancellationToken = default); Task ExportCsvAsync(ReportRequest request, string filePath, CancellationToken cancellationToken = default); Task<byte[]> CreateAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default); }
