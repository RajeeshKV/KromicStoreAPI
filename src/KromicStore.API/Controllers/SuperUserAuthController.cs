using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Auth;

namespace KromicStore.API.Controllers;

/// <summary>
/// Controller for SuperUser authentication (platform admins).
/// </summary>
[ApiController]
[Route("api/v1/superuser/auth")]
public class SuperUserAuthController : ControllerBase
{
    private readonly ISuperUserAuthService _authService;
    private readonly ILogger<SuperUserAuthController> _logger;

    public SuperUserAuthController(ISuperUserAuthService authService, ILogger<SuperUserAuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Login as SuperUser (platform admin).
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
            _logger.LogInformation("SuperUser login successful: {Email}", request.Email);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("SuperUser login failed: {Email}, Reason: {Reason}", request.Email, ex.Message);
            return Unauthorized(new { error = "Invalid email or password" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SuperUser login error: {Email}", request.Email);
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Refresh SuperUser authentication token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("SuperUser token refresh failed: {Reason}", ex.Message);
            return Unauthorized(new { error = "Invalid refresh token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SuperUser token refresh error");
            return StatusCode(500, new { error = "An error occurred during token refresh" });
        }
    }
}
