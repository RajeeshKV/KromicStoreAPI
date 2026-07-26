using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Auth;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
    private readonly AppDbContext _context;

    public SuperUserAuthController(ISuperUserAuthService authService, ILogger<SuperUserAuthController> logger, AppDbContext context)
    {
        _authService = authService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Register a new SuperUser (platform admin).
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SuperUserRegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if email already exists
            var existingSuperUser = await _context.SuperUsers
                .FirstOrDefaultAsync(su => su.Email.ToLower() == request.Email.ToLower(), cancellationToken);

            if (existingSuperUser != null)
            {
                _logger.LogWarning("SuperUser registration failed: Email {Email} already exists", request.Email);
                return BadRequest(new { error = "Email address is already registered" });
            }

            // Create SuperUser (using simple password hash for now - TODO: implement BCrypt)
            var passwordHash = HashPassword(request.Password);
            var superUser = SuperUser.Create(request.Email, passwordHash);

            await _context.SuperUsers.AddAsync(superUser, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("SuperUser registered successfully: {Email}", request.Email);

            var response = new
            {
                data = new
                {
                    id = superUser.Id,
                    email = superUser.Email,
                    firstName = superUser.FirstName,
                    lastName = superUser.LastName,
                    isActive = superUser.IsActive,
                    createdAt = superUser.CreatedAt
                }
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("SuperUser registration validation failed: {Email}, Reason: {Reason}", request.Email, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SuperUser registration error: {Email}", request.Email);
            return StatusCode(500, new { error = "An error occurred during registration" });
        }
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

    /// <summary>
    /// Logout SuperUser by invalidating their tokens.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            // Get SuperUser ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var superUserId))
            {
                _logger.LogWarning("SuperUser logout failed - invalid user claim");
                return Unauthorized(new { error = "Invalid authentication token" });
            }

            _logger.LogInformation("SuperUser logout request for SuperUserId: {SuperUserId}", superUserId);

            // Call auth service to increment token version
            await _authService.LogoutAsync(superUserId, cancellationToken);

            _logger.LogInformation("SuperUser logout successful for SuperUserId: {SuperUserId}", superUserId);

            return Ok(new { message = "Logout successful" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("SuperUser logout failed: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SuperUser logout error");
            return StatusCode(500, new { error = "An error occurred during logout" });
        }
    }

    private string HashPassword(string password)
    {
        // Simple hash for now - TODO: Implement BCrypt
        return password; // WARNING: This is not secure, implement proper hashing
    }
}

/// <summary>
/// Request model for SuperUser registration.
/// </summary>
public class SuperUserRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
