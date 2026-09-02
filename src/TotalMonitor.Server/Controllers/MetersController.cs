using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Security;

namespace TotalMonitor.Server.Controllers;

[ApiController, Route("api/v1/meters"), Authorize]
public sealed class MetersController(IMeterService meters) : ControllerBase
{
    [HttpGet, Authorize(Policy = PermissionNames.MetersView)]
    public async Task<ActionResult<IReadOnlyList<MeterDto>>> GetAll(CancellationToken ct) =>
        Ok((await meters.GetAllAsync(ct)).Select(ToDto));

    [HttpGet("{id:int}"), Authorize(Policy = PermissionNames.MetersView)]
    public async Task<ActionResult<MeterDto>> Get(int id, CancellationToken ct)
    {
        var meter = (await meters.GetAllAsync(ct)).SingleOrDefault(x => x.Id == id);
        return meter is null ? NotFound(new ApiErrorResponse("not_found", "Medidor no encontrado.")) : Ok(ToDto(meter));
    }

    [HttpPost, Authorize(Policy = PermissionNames.MetersCreate)]
    public async Task<ActionResult<MeterDto>> Create(MeterRequest request, CancellationToken ct)
    {
        try
        {
            var meter = await meters.CreateAsync(
                new Meter(request.Name, request.ModbusAddress, request.ComPort, request.BaudRate, request.Parity, request.IsEnabled, request.Model ?? "TOV452"),
                ct);
            return CreatedAtAction(nameof(Get), new { id = meter.Id }, ToDto(meter));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse("invalid_request", ex.Message));
        }
    }

    [HttpPut("{id:int}"), Authorize(Policy = PermissionNames.MetersEdit)]
    public async Task<IActionResult> Update(int id, MeterRequest request, CancellationToken ct) =>
        await meters.UpdateAsync(id, request.Name, request.ModbusAddress, request.ComPort, request.BaudRate, request.Parity, request.IsEnabled, request.Model ?? "TOV452", ct)
            ? NoContent()
            : NotFound(new ApiErrorResponse("not_found", "Medidor no encontrado."));

    [HttpDelete("{id:int}"), Authorize(Policy = PermissionNames.MetersDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await meters.DeleteAsync(id, ct) ? NoContent() : NotFound(new ApiErrorResponse("not_found", "Medidor no encontrado."));

    private static MeterDto ToDto(Meter meter) =>
        new(meter.Id, meter.Name, meter.Model, meter.ModbusAddress, meter.ComPort, meter.BaudRate, meter.Parity, meter.IsEnabled);
}

public sealed record MeterRequest(string Name, byte ModbusAddress, string ComPort, int BaudRate, string Parity, bool IsEnabled, string? Model = "TOV452");
