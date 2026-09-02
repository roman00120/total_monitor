using TotalMonitor.Core.Interfaces;
namespace TotalMonitor.Infrastructure.Historical;
public sealed class ReportService(IHistoricalDataService historical, ICurrentUserService currentUser, IAuthorizationService authorization, IHistoricalDataExporter exporter, IAuditService audit) : IReportService
{
    public async Task<ReportResult> GenerateAsync(ReportRequest request, CancellationToken ct = default) { authorization.Demand(TotalMonitor.Core.Security.PermissionNames.ReportsView); var data = await historical.QueryAsync(new(request.From, request.To, request.MeterId, request.Variable, request.Resolution, 1, request.PageSize), ct); return new ReportResult(request, data, DateTimeOffset.UtcNow, currentUser.User?.UserId); }
    public async Task ExportCsvAsync(ReportRequest request, string filePath, CancellationToken ct = default) { authorization.Demand(TotalMonitor.Core.Security.PermissionNames.ReportsExport); var report = await GenerateAsync(request, ct); await exporter.ExportCsvAsync(report.Data.Items, filePath, ct); await audit.RecordAsync("ReportExported", "Reporte histórico exportado", ct); }
    public Task<byte[]> CreateAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) => Task.FromResult(Array.Empty<byte>());
}
