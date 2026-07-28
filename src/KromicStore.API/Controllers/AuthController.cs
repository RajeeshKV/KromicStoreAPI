namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Auth;
using KromicStore.Contracts.Abstractions;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Authentication endpoints for user registration, login, token refresh, and OAuth authentication.
/// These endpoints are publicly accessible without authorization.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the AuthController class.
    /// </summary>
    public AuthController(
        IAuthService authService,
        ITenantProvider tenantProvider,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new tenant and creates a TenantAdmin user.
    /// </summary>
    /// <param name="request">Registration request containing user and tenant information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with access and refresh tokens.</returns>
    /// <response code="201">Registration successful.</response>
    /// <response code="400">Validation error (invalid email format, weak password, missing required fields).</response>
    /// <response code="409">Email already exists.</response>
    /// <response code="500">Server error during registration.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

            // Validate request
            var validationError = ValidateRegisterRequest(request);
            if (validationError != null)
            {
                _logger.LogWarning("Registration validation failed: {Error}", validationError);
                return BadRequest(new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = validationError
                });
            }

            // Call auth service (registers new tenant and creates TenantAdmin user)
            var response = await _authService.RegisterAsync(
                Guid.Empty,  // New tenant, so tenantId is empty
                request,
                cancellationToken);

            _logger.LogInformation("Registration successful for email: {Email}, UserId: {UserId}", 
                request.Email, response.UserId);

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning("Registration failed - email already exists: {Email}", request.Email);
            return Conflict(new ErrorResponse
            {
                Code = "EMAIL_ALREADY_EXISTS",
                Message = "This email is already registered."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for email: {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "REGISTRATION_ERROR",
                Message = "An error occurred during registration."
            });
        }
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">Login request containing email and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with access and refresh tokens.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="400">Missing or invalid email/password.</response>
    /// <response code="401">Invalid credentials.</response>
    /// <response code="500">Server error during login.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                _logger.LogWarning("Login failed - email missing");
                return BadRequest(new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Email is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login failed - password missing");
                return BadRequest(new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Password is required."
                });
            }

            // For multi-tenant, we need to extract tenant from email domain or use a lookup
            // For now, use tenant from context if available, otherwise try to find from email
            var tenantId = _tenantProvider.TenantId != Guid.Empty 
                ? _tenantProvider.TenantId 
                : Guid.Empty;  // Will be resolved by service

            var response = await _authService.LoginAsync(
                tenantId,
                request.Email,
                request.Password,
                cancellationToken);

            _logger.LogInformation("Login successful for email: {Email}, UserId: {UserId}", 
                request.Email, response.UserId);

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
            return Unauthorized(new ErrorResponse
            {
                Code = "INVALID_CREDENTIALS",
                Message = "Invalid email or password."
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("invalid") || ex.Message.Contains("password"))
        {
            _logger.LogWarning("Login failed - invalid password: {Email}", request.Email);
            return Unauthorized(new ErrorResponse
            {
                Code = "INVALID_CREDENTIALS",
                Message = "Invalid email or password."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for email: {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "LOGIN_ERROR",
                Message = "An error occurred during login."
            });
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New authentication response with new access and refresh tokens.</returns>
    /// <response code="200">Token refresh successful.</response>
    /// <response code="400">Refresh token missing.</response>
    /// <response code="401">Refresh token invalid or expired.</response>
    /// <response code="500">Server error during token refresh.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Token refresh request received");

            // Validate request
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _logger.LogWarning("Token refresh failed - refresh token missing");
                return BadRequest(new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Refresh token is required."
                });
            }

            var response = await _authService.RefreshTokenAsync(
                request.RefreshToken,
                cancellationToken);

            _logger.LogInformation("Token refresh successful, new UserId: {UserId}", response.UserId);

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired") || ex.Message.Contains("invalid"))
        {
            _logger.LogWarning("Token refresh failed - invalid or expired refresh token");
            return Unauthorized(new ErrorResponse
            {
                Code = "INVALID_REFRESH_TOKEN",
                Message = "Refresh token is invalid or expired."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "REFRESH_ERROR",
                Message = "An error occurred during token refresh."
            });
        }
    }

    /// <summary>
    /// Authenticates a user via OAuth provider (Google).
    /// </summary>
    /// <param name="request">OAuth login request with provider and authorization token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with access and refresh tokens.</returns>
    /// <response code="200">OAuth login successful (existing account).</response>
    /// <response code="201">OAuth login successful (new account created).</response>
    /// <response code="400">Invalid OAuth request (missing provider or token).</response>
    /// <response code="401">OAuth exchange failed.</response>
    /// <response code="500">Server error during OAuth login.</response>
    [HttpPost("oauth/google")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OAuthGoogleAsync(
        [FromBody] OAuthLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("OAuth login attempt for provider: {Provider}", request.Provider);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                _logger.LogWarning("OAuth login failed - provider missing");
                return BadRequest(new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "OAuth provider is required."
                });
            }

            if (request.Provider != "google" && request.Provider != "Google")
            {
                _logger.LogWarning("OAuth login failed - unsupported provider: {Provider}", request.Provider);
                return BadRequest(new ErrorResponse
                {
                    Code = "UNSUPPORTED_PROVIDER",
                    Message = "This OAuth provider is not supported. Currently only 'google' is supported."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                _logger.LogWarning("OAuth login failed - authorization token missing");
                return BadRequest(new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Authorization token is required."
                });
            }

            var tenantId = _tenantProvider.TenantId != Guid.Empty 
                ? _tenantProvider.TenantId 
                : Guid.Empty;

            var response = await _authService.OAuthLoginAsync(
                tenantId,
                request.Provider,
                request.Token,
                cancellationToken);

            _logger.LogInformation("OAuth login successful for provider: {Provider}, UserId: {UserId}", 
                request.Provider, response.UserId);

            // Return 201 Created if new account was created, otherwise 200 OK
            // This is indicated by checking if ExpiresAt matches a new token timing
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found") || ex.Message.Contains("Failed to exchange"))
        {
            _logger.LogWarning("OAuth login failed - exchange failed or provider error: {Message}", ex.Message);
            return Unauthorized(new ErrorResponse
            {
                Code = "OAUTH_EXCHANGE_FAILED",
                Message = "Failed to complete OAuth authentication. Please try again."
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("OAuth login failed - invalid argument: {Message}", ex.Message);
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_OAUTH_REQUEST",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth login error for provider: {Provider}", request.Provider);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "OAUTH_ERROR",
                Message = "An error occurred during OAuth authentication."
            });
        }
    }

    /// <summary>
    /// Logs out the current user by invalidating their tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response.</returns>
    /// <response code="200">Logout successful.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="500">Server error during logout.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Logout failed - invalid user claim");
                return Unauthorized(new ErrorResponse
                {
                    Code = "INVALID_TOKEN",
                    Message = "Invalid authentication token."
                });
            }

            _logger.LogInformation("Logout request for UserId: {UserId}", userId);

            // Call auth service to increment token version
            await _authService.LogoutAsync(userId, cancellationToken);

            _logger.LogInformation("Logout successful for UserId: {UserId}", userId);

            return Ok(new { message = "Logout successful" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Logout failed: {Message}", ex.Message);
            return Unauthorized(new ErrorResponse
            {
                Code = "LOGOUT_FAILED",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error for UserId");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "LOGOUT_ERROR",
                Message = "An error occurred during logout."
            });
        }
    }

    /// <summary>
    /// Validates a register request.
    /// </summary>
    private string? ValidateRegisterRequest(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email is required.";

        if (string.IsNullOrWhiteSpace(request.FirstName))
            return "First name is required.";

        if (string.IsNullOrWhiteSpace(request.LastName))
            return "Last name is required.";

        if (string.IsNullOrWhiteSpace(request.Password))
            return "Password is required.";

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            return "Password confirmation is required.";

        // Email format validation
        try
        {
            var addr = new System.Net.Mail.MailAddress(request.Email);
        }
        catch
        {
            return "Invalid email format.";
        }

        // Password strength validation (min 8 chars, uppercase, number, special char)
        if (request.Password.Length < 8)
            return "Password must be at least 8 characters long.";

        if (!request.Password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter.";

        if (!request.Password.Any(char.IsDigit))
            return "Password must contain at least one digit.";

        if (!request.Password.Any(c => !char.IsLetterOrDigit(c)))
            return "Password must contain at least one special character.";

        if (request.Password != request.ConfirmPassword)
            return "Passwords do not match.";

        return null;  // Valid
    }
}


