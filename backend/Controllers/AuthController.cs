using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PurchaseApproval.DTOs;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthTokenService _authTokenService;
    private readonly IAuditLogService _auditLogService;

    public AuthController(
        IUserService userService,
        IAuthTokenService authTokenService,
        IAuditLogService auditLogService)
    {
        _userService = userService;
        _authTokenService = authTokenService;
        _auditLogService = auditLogService;
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await _userService.LoginAsync(dto.Username.Trim(), dto.Password);
        if (user == null)
        {
            await _auditLogService.LogAsync(
                action: "Auth.Login",
                resourceType: "User",
                result: "Failed",
                details: $"username={dto.Username.Trim()}，账号或密码错误");

            return Unauthorized(new { message = "账号或密码错误" });
        }

        var token = _authTokenService.GenerateAccessToken(user);

        await _auditLogService.LogAsync(
            action: "Auth.Login",
            resourceType: "User",
            resourceId: user.Id.ToString(),
            result: "Success",
            details: $"username={user.Username};role={user.Role}",
            actorId: user.Id,
            actorUsername: user.Username,
            actorRole: user.Role.ToString());

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            displayName = user.DisplayName,
            role = user.Role.ToString(),
            roleValue = (int)user.Role,
            department = user.Department,
            token = token.AccessToken,
            tokenType = token.TokenType,
            expiresAt = token.ExpiresAt
        });
    }
}
