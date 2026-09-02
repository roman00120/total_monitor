using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotalMonitor.Core.Historical;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Security;
namespace TotalMonitor.Server.Controllers;
[ApiController, Route("api/v1/measurements"), Authorize(Policy = PermissionNames.HistoryView)]
public sealed class MeasurementsController(IHistoricalDataService history) : ControllerBase
{ [HttpGet] public async Task<ActionResult<HistoricalPage>> Get([FromQuery] HistoricalQueryDto query, CancellationToken ct) { try { var filter = new HistoricalQueryFilter(query.From, query.To, query.MeterId, query.Variable, query.Resolution, Math.Max(1, query.PageNumber), Math.Clamp(query.PageSize, 1, 5000)); return Ok(await history.QueryAsync(filter, ct)); } catch (ArgumentException ex) { return BadRequest(new ApiErrorResponse("invalid_query", ex.Message)); } } }
