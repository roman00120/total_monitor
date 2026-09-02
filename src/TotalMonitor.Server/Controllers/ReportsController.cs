using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Security;

namespace TotalMonitor.Server.Controllers;

[ApiController, Route("api/v1/reports"), Authorize(Policy = PermissionNames.ReportsView)]
public sealed class ReportsController(IReportService reports, IAuditService audit) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReportResponseDto>> Create(ReportRequestDto request, CancellationToken ct)
    {
        var result = await reports.GenerateAsync(new(request.From, request.To, request.MeterId, request.Variable, request.Resolution), ct);
        return Ok(new ReportResponseDto(result.Data.TotalCount, result.GeneratedAt, result.Data.Items.Select(x => new MeasurementDto(x.Id, x.MeterId, x.MeterName, x.Timestamp, x.Variable, x.Value, x.Unit)).ToList()));
    }

    [HttpPost("export"), Authorize(Policy = PermissionNames.ReportsExport)]
    public async Task<IActionResult> Export(ReportRequestDto request, CancellationToken ct)
    {
        var result = await reports.GenerateAsync(new(request.From, request.To, request.MeterId, request.Variable, request.Resolution), ct);
        var csv = new System.Text.StringBuilder("Timestamp,Meter,Variable,Value,Unit\n");
        foreach (var item in result.Data.Items)
            csv.AppendLine($"{item.Timestamp:O},\"{item.MeterName.Replace("\"", "\"\"")}\",\"{item.Variable.Replace("\"", "\"\"")}\",{item.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"{item.Unit.Replace("\"", "\"\"")}\"");
        await audit.RecordAsync("ReportExported", "Reporte exportado desde API", ct);
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "total-monitor-report.csv");
    }
}
