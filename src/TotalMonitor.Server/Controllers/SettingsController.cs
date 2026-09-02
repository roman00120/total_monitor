using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Security;
using TotalMonitor.Infrastructure.Modbus;

namespace TotalMonitor.Server.Controllers;

[ApiController, Route("api/v1/settings"), Authorize]
public sealed class SettingsController(ICommunicationSettingsService settingsService) : ControllerBase
{
    [HttpGet("communication"), Authorize(Policy = PermissionNames.SettingsView)]
    public async Task<ActionResult<CommunicationSettingsDto>> GetCommunicationSettings(CancellationToken ct)
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        return Ok(ToDto(settings));
    }

    [HttpPut("communication"), Authorize(Policy = PermissionNames.SettingsEdit)]
    public async Task<ActionResult<CommunicationSettingsDto>> UpdateCommunicationSettings(
        [FromBody] CommunicationSettingsRequest request,
        CancellationToken ct)
    {
        try
        {
            var settings = new CommunicationSettings(
                request.ComPort,
                request.BaudRate,
                request.DataBits,
                request.Parity,
                request.StopBits,
                request.ReadTimeout,
                request.WriteTimeout,
                request.PollingInterval);

            var saved = await settingsService.SaveSettingsAsync(settings, ct);
            return Ok(ToDto(saved));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse("invalid_settings", ex.Message));
        }
    }

    [HttpGet("ports"), Authorize(Policy = PermissionNames.SettingsView)]
    public ActionResult<IReadOnlyList<string>> GetAvailablePorts()
    {
        return Ok(SerialPortService.GetAvailablePorts());
    }

    [HttpPost("test-connection"), Authorize(Policy = PermissionNames.SettingsView)]
    public async Task<ActionResult<TestConnectionResponse>> TestConnection(
        [FromBody] TestConnectionRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ComPort))
        {
            return BadRequest(new TestConnectionResponse(false, "✕ No se especificó ningún puerto COM.", MeterHardwareState.ESPERANDO_COM.ToString()));
        }

        var (success, message, state) = await settingsService.TestConnectionAsync(
            request.ComPort,
            request.SlaveAddress == 0 ? (byte)1 : request.SlaveAddress,
            request.BaudRate,
            request.Parity ?? "None",
            request.DataBits,
            request.StopBits ?? "One",
            request.Timeout,
            ct);

        var formattedMessage = success
            ? $"✓ {message}"
            : $"✕ {message}";

        return Ok(new TestConnectionResponse(success, formattedMessage, state.ToString()));
    }

    private static CommunicationSettingsDto ToDto(CommunicationSettings s) =>
        new(
            s.Id,
            s.ComPort,
            s.BaudRate,
            s.DataBits,
            s.Parity,
            s.StopBits,
            s.ReadTimeout,
            s.WriteTimeout,
            s.PollingInterval,
            s.UpdatedAt);
}
