namespace KromicStore.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Auth;

/// <summary>
/// Implementation of authentication service providing login, registration, token refresh, and OAuth integration.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(ILogger<AuthService> logger, IConfiguration configuration, IUnitOfWork unitOfWork)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    public async Task<AuthResponse> LoginAsync(Guid tenantId, string email, string password, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        _logger.LogInformation("User login attempt for email {Email} in tenant {TenantId}", email, tenantId);

        // Find user by email and tenantId
        var user = (await _unitOfWork.Users.FindAsync(u => u.Email.ToLower() == email.ToLower() && u.TenantId == tenantId, cancellationToken)).FirstOrDefault();
        
        if (user == null)
        {
            _logger.LogWarning("User not found: {Email} in tenant {TenantId}", email, tenantId);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("User account is inactive: {Email}", email);
            throw new UnauthorizedAccessException("Account is inactive");
        }

        // Verify password (simple comparison for now - TODO: implement BCrypt)
        if (user.PasswordHash != password)
        {
            _logger.LogWarning("Invalid password for user: {Email}", email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Record login
        user.RecordLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate JWT token with token version
        var accessToken = GenerateAccessToken(user.Id, tenantId, email, new[] { user.Role.ToString() }, user.TokenVersion);
        var refreshToken = GenerateRefreshToken();

        var response = new AuthResponse(
            UserId: user.Id,
            Email: email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );

        _logger.LogInformation("User {Email} logged in successfully in tenant {TenantId}", email, tenantId);

        return response;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(Guid tenantId, RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email cannot be empty", nameof(request.Email));

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password cannot be empty", nameof(request.Password));

        _logger.LogInformation("User registration attempt for email {Email} in tenant {TenantId}", request.Email, tenantId);

        // In real implementation, would:
        // 1. Check if email already exists
        // 2. Validate password strength
        // 3. Hash password
        // 4. Create user in database
        // 5. Generate JWT token
        // For now, return stub response

        var userId = Guid.NewGuid();
        var accessToken = GenerateAccessToken(userId, tenantId, request.Email, new[] { "User" });
        var refreshToken = GenerateRefreshToken();

        var response = new AuthResponse(
            UserId: userId,
            Email: request.Email,
            FirstName: request.FirstName ?? "User",
            LastName: request.LastName ?? "Account",
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );

        _logger.LogInformation("User {Email} registered successfully in tenant {TenantId}", request.Email, tenantId);

        return await Task.FromResult(response);
    }

    /// <summary>
    /// Refreshes the authentication token.
    /// </summary>
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token cannot be empty", nameof(refreshToken));

        _logger.LogInformation("Token refresh requested");

        // In real implementation, would:
        // 1. Validate refresh token
        // 2. Check if token has been revoked
        // 3. Generate new access token
        // 4. Optionally rotate refresh token
        // For now, return stub response

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var accessToken = GenerateAccessToken(userId, tenantId, "user@example.com", new[] { "User" });
        var newRefreshToken = GenerateRefreshToken();

        var response = new AuthResponse(
            UserId: userId,
            Email: "user@example.com",
            FirstName: "User",
            LastName: "Account",
            AccessToken: accessToken,
            RefreshToken: newRefreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );

        _logger.LogInformation("Token refreshed successfully");

        return await Task.FromResult(response);
    }

    /// <summary>
    /// Authenticates a user with external provider (Google, GitHub, etc.).
    /// </summary>
    public async Task<AuthResponse> OAuthLoginAsync(Guid tenantId, string provider, string token, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be empty", nameof(provider));

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty", nameof(token));

        _logger.LogInformation("OAuth login attempt via {Provider} for tenant {TenantId}", provider, tenantId);

        // In real implementation, would:
        // 1. Validate provider token (call Google API, etc.)
        // 2. Extract user info from provider
        // 3. Create or link user account
        // 4. Generate JWT token
        // For now, return stub response

        var userId = Guid.NewGuid();
        var accessToken = GenerateAccessToken(userId, tenantId, "user@example.com", new[] { "User" });
        var refreshToken = GenerateRefreshToken();

        var response = new AuthResponse(
            UserId: userId,
            Email: "user@example.com",
            FirstName: "User",
            LastName: "Account",
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );

        _logger.LogInformation("User authenticated via {Provider} for tenant {TenantId}", provider, tenantId);

        return await Task.FromResult(response);
    }

    /// <summary>
    /// Validates a JWT token.
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return await Task.FromResult(false);

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Auth:JwtSecret"] ?? "your-secret-key-change-this-in-production"));

            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return await Task.FromResult(validatedToken != null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return await Task.FromResult(false);
        }
    }

    /// <summary>
    /// Logs out a user by incrementing their token version to invalidate all existing tokens.
    /// </summary>
    public async Task LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = (await _unitOfWork.Users.FindAsync(u => u.Id == userId, cancellationToken)).FirstOrDefault();
        
        if (user == null)
        {
            _logger.LogWarning("Logout failed - user not found: {UserId}", userId);
            throw new InvalidOperationException("User not found");
        }

        // Increment token version to invalidate all existing tokens
        user.IncrementTokenVersion();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged out successfully: {UserId}, New Token Version: {TokenVersion}", userId, user.TokenVersion);
    }

    /// <summary>
    /// Generates a JWT access token.
    /// </summary>
    public string GenerateAccessToken(Guid userId, Guid tenantId, string email, string[] roles, int tokenVersion = 1)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Auth:JwtSecret"] ?? "your-secret-key-change-this-in-production"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("email", email),
            new Claim("token_version", tokenVersion.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Auth:Issuer"] ?? "KromicStore",
            audience: _configuration["Auth:Audience"] ?? "KromicStore",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a refresh token (32 random bytes, base64 encoded).
    /// </summary>
    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return Convert.ToBase64String(randomBytes);
    }
}
