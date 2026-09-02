using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Security;

namespace TotalMonitor.Server.Controllers;

[ApiController, Route("api/v1/acquisition"), Authorize]
public sealed class AcquisitionController(IDataAcquisitionService acquisition) : ControllerBase
{
    [HttpGet("status"), Authorize(Policy = PermissionNames.DashboardView)]
    public ActionResult<AcquisitionStatusDto> GetStatus()
    {
        var s = acquisition.GetStatusSummary();
        return Ok(ToDto(s));
    }

    [HttpPost("start"), Authorize(Policy = PermissionNames.SettingsEdit)]
    public async Task<ActionResult<AcquisitionStatusDto>> Start(CancellationToken ct)
    {
        await acquisition.StartAsync(ct);
        var s = acquisition.GetStatusSummary();
        return Ok(ToDto(s));
    }

    [HttpPost("stop"), Authorize(Policy = PermissionNames.SettingsEdit)]
    public async Task<ActionResult<AcquisitionStatusDto>> Stop()
    {
        await acquisition.StopAsync();
        var s = acquisition.GetStatusSummary();
        return Ok(ToDto(s));
    }

    private static AcquisitionStatusDto ToDto(AcquisitionStatusSummary s) =>
        new(
            s.State.ToString(),
            s.HardwareState.ToString(),
            s.HardwareStatusText,
            s.CurrentPort,
            s.ActiveMetersCount,
            s.LastAcquisitionTime,
            s.LastError,
            s.TotalReadingsProcessed);
}
