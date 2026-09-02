using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TotalMonitor.Core.Interfaces;
namespace TotalMonitor.Server.Controllers;
[ApiController, Route("api/v1/auth")]
public sealed class AuthController(IAuthenticationService authentication, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) { var result = await authentication.LoginAsync(request.Username, request.Password, ct); if (!result.Success || result.User is null) return Unauthorized(new ApiErrorResponse("invalid_credentials", result.Message)); var expires = DateTimeOffset.UtcNow.AddHours(8); var secret = configuration["Authentication:SecretKey"]; if (string.IsNullOrWhiteSpace(secret) || secret.StartsWith("CHANGE_ME", StringComparison.Ordinal) || secret.Length < 32) return Problem("JWT secret is not configured securely.", statusCode: 500); var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, result.User.UserId.ToString()), new(ClaimTypes.Name, result.User.Username), new("display_name", result.User.DisplayName), new(ClaimTypes.Role, result.User.Role) }; claims.AddRange(result.User.Permissions.Select(x => new Claim("permission", x))); var token = new JwtSecurityToken(configuration["Authentication:Issuer"] ?? "TotalMonitor", configuration["Authentication:Audience"] ?? "TotalMonitor.Client", claims, expires: expires.UtcDateTime, signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256)); return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), result.User.UserId, result.User.Username, result.User.DisplayName, result.User.Role, result.User.Permissions, expires)); }
    [Authorize, HttpPost("logout")] public async Task<IActionResult> Logout(CancellationToken ct) { await authentication.LogoutAsync(ct); return NoContent(); }
}
