using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Security;
namespace TotalMonitor.Server.Controllers;
[ApiController, Route("api/v1/users"), Authorize]
public sealed class UsersController(IUserAdminService users) : ControllerBase
{
    [HttpGet, Authorize(Policy = PermissionNames.UsersView)] public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken ct) => Ok((await users.GetAllAsync(ct)).Select(x => new UserDto(x.Id, x.UserName, x.DisplayName, x.IsActive, x.LastLoginAt)));
    [HttpPost, Authorize(Policy = PermissionNames.UsersCreate)] public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct) { try { var user = await users.CreateAsync(request.Username, request.DisplayName, request.Password, request.Role, ct); return Ok(new UserDto(user.Id, user.UserName, user.DisplayName, user.IsActive, user.LastLoginAt)); } catch (ArgumentException ex) { return BadRequest(new ApiErrorResponse("invalid_request", ex.Message)); } catch (InvalidOperationException ex) { return Conflict(new ApiErrorResponse("conflict", ex.Message)); } }
    [HttpPut("{id:int}/active"), Authorize(Policy = PermissionNames.UsersEdit)] public async Task<IActionResult> SetActive(int id, SetActiveRequest request, CancellationToken ct) { await users.SetActiveAsync(id, request.Active, ct); return NoContent(); }
}
public sealed record CreateUserRequest(string Username, string DisplayName, string Password, string Role); public sealed record SetActiveRequest(bool Active);
