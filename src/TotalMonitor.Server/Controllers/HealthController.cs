using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TotalMonitor.Infrastructure.Persistence;
namespace TotalMonitor.Server.Controllers;
[ApiController, Route("api/health"), Route("api/v1/health")]
public sealed class HealthController(TotalMonitorDbContext db, IConfiguration configuration) : ControllerBase
{ [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) { var database = false; try { database = await db.Database.CanConnectAsync(ct); } catch { } return Ok(new { status = database ? "ready" : "degraded", version = typeof(HealthController).Assembly.GetName().Version?.ToString(), serverTime = DateTimeOffset.UtcNow, database, serverMode = configuration["Server:Mode"] ?? "Real" }); } }
