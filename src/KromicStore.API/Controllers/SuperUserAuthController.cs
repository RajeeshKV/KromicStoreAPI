using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Auth;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

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
    /// <param name="request">Registration request containing email and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created SuperUser data with ID, email, name, and activation status.</returns>
    /// <response code="200">SuperUser registered successfully.</response>
    /// <response code="400">Validation failed or email already registered.</response>
    /// <response code="500">Server error during registration.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] SuperUserRegisterRequest request, CancellationToken cancellationToken)
    {
        // Validate model state
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            _logger.LogWarning("SuperUser registration validation failed: {Errors}", string.Join(", ", errors));
            return BadRequest(new { error = "Validation failed", details = errors });
        }

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
    /// <param name="request">Login request containing email and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with access token, refresh token, and user info.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid email or password.</response>
    /// <response code="500">Server error during login.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
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
    /// <param name="request">Refresh token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New authentication response with fresh tokens.</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    /// <response code="500">Server error during token refresh.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message confirming logout.</returns>
    /// <response code="200">Logout successful.</response>
    /// <response code="401">Invalid authentication token or SuperUser not found.</response>
    /// <response code="500">Server error during logout.</response>
    [Authorize(Policy = "SuperUserOnly")]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
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
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
}
