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
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementation of authentication service providing login, registration, token refresh, and OAuth integration.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;

    public AuthService(ILogger<AuthService> logger, IConfiguration configuration, IUnitOfWork unitOfWork, AppDbContext context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// Checks both TenantAdmin and User tables.
    /// </summary>
    public async Task<AuthResponse> LoginAsync(Guid tenantId, string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            _logger.LogInformation("User login attempt for email {Email} in tenant {TenantId}", email, tenantId);

            // Try TenantAdmin first (global email uniqueness)
            TenantAdmin? tenantAdmin = null;
            if (tenantId != Guid.Empty)
            {
                tenantAdmin = (await _unitOfWork.TenantAdmins.FindAsync(a => a.Email.ToLower() == email.ToLower() && a.TenantId == tenantId, cancellationToken)).FirstOrDefault();
            }
            else
            {
                // Look up by email alone and get their tenantId
                tenantAdmin = (await _unitOfWork.TenantAdmins.FindAsync(a => a.Email.ToLower() == email.ToLower(), cancellationToken)).FirstOrDefault();
                if (tenantAdmin != null)
                {
                    tenantId = tenantAdmin.TenantId;
                    _logger.LogInformation("Resolved tenant {TenantId} for TenantAdmin {Email}", tenantId, email);
                }
            }

            // If TenantAdmin found, authenticate as admin
            if (tenantAdmin != null)
            {
                if (!tenantAdmin.IsActive)
                {
                    _logger.LogWarning("TenantAdmin account is inactive: {Email}", email);
                    throw new UnauthorizedAccessException("Account is inactive");
                }

                // Verify password (simple comparison for now - TODO: implement BCrypt)
                if (tenantAdmin.PasswordHash != password)
                {
                    _logger.LogWarning("Invalid password for TenantAdmin: {Email}", email);
                    throw new UnauthorizedAccessException("Invalid email or password");
                }

                // Record login
                tenantAdmin.RecordLogin();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Generate JWT token
                var accessToken = GenerateAccessToken(tenantAdmin.Id, tenantId, email, new[] { "TenantAdmin" }, tenantAdmin.TokenVersion);
                var refreshToken = GenerateRefreshToken();

                await PersistRefreshTokenAsync(tenantAdmin.Id, "TenantAdmin", refreshToken, cancellationToken);

                var response = new AuthResponse(
                    UserId: tenantAdmin.Id,
                    Email: email,
                    FirstName: tenantAdmin.FirstName,
                    LastName: tenantAdmin.LastName,
                    AccessToken: accessToken,
                    RefreshToken: refreshToken,
                    ExpiresAt: DateTime.UtcNow.AddHours(1)
                );

                _logger.LogInformation("TenantAdmin {Email} logged in successfully in tenant {TenantId}", email, tenantId);
                return response;
            }

            // If not TenantAdmin, try User table (per-tenant email uniqueness)
            User? user = null;
            if (tenantId != Guid.Empty)
            {
                user = (await _unitOfWork.Users.FindAsync(u => u.Email.ToLower() == email.ToLower() && u.TenantId == tenantId, cancellationToken)).FirstOrDefault();
            }
            else
            {
                // Look up user by email alone and get their tenantId
                user = (await _unitOfWork.Users.FindAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken)).FirstOrDefault();
                if (user != null && user.TenantId.HasValue)
                {
                    tenantId = user.TenantId.Value;
                    _logger.LogInformation("Resolved tenant {TenantId} for User {Email}", tenantId, email);
                }
            }

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
            var userAccessToken = GenerateAccessToken(user.Id, tenantId, email, new[] { user.Role.ToString() }, user.TokenVersion);
            var userRefreshToken = GenerateRefreshToken();

            await PersistRefreshTokenAsync(user.Id, "User", userRefreshToken, cancellationToken);

            var userResponse = new AuthResponse(
                UserId: user.Id,
                Email: email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                AccessToken: userAccessToken,
                RefreshToken: userRefreshToken,
                ExpiresAt: DateTime.UtcNow.AddHours(1)
            );

            _logger.LogInformation("User {Email} logged in successfully in tenant {TenantId}", email, tenantId);
            return userResponse;
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw auth failures
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation errors
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", email);
            throw new InvalidOperationException("Login failed. Please try again.", ex);
        }
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(Guid tenantId, RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email cannot be empty", nameof(request.Email));

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password cannot be empty", nameof(request.Password));

        // If tenantId is empty, this is a new tenant registration - create a new tenant
        if (tenantId == Guid.Empty)
        {
            _logger.LogInformation("New tenant registration for email {Email}", request.Email);
            
            // Create new tenant using the factory method
            var newTenantId = Guid.NewGuid().ToString();
            var subdomain = GenerateSubdomainFromEmail(request.Email);
            var newTenant = KromicStore.Domain.Entities.Tenant.Create(
                tenantId: newTenantId,
                name: request.FirstName, // Use FirstName as company name for now
                subdomain: subdomain,
                description: "Auto-created tenant for registration",
                contactEmail: request.Email
            );
            
            _context.Tenants.Add(newTenant);
            await _context.SaveChangesAsync(cancellationToken);
            
            tenantId = Guid.Parse(newTenant.TenantId);
            _logger.LogInformation("Created new tenant {TenantId} for email {Email}", tenantId, request.Email);
        }

        _logger.LogInformation("User registration attempt for email {Email} in tenant {TenantId}", request.Email, tenantId);

        try
        {
            // Check if email already exists globally in TenantAdmins (one email = one TenantAdmin account)
            var existingAdmin = (await _unitOfWork.TenantAdmins.FindAsync(a => a.Email.ToLower() == request.Email.ToLower(), cancellationToken)).FirstOrDefault();
            if (existingAdmin != null)
            {
                _logger.LogWarning("Registration failed - email already exists as TenantAdmin: {Email}", request.Email);
                throw new InvalidOperationException("This email is already registered as a store administrator");
            }

            // Create TenantAdmin
            var tenantAdmin = TenantAdmin.Create(
                tenantId: tenantId,
                firstName: request.FirstName,
                lastName: request.LastName,
                email: request.Email,
                password: request.Password // TODO: Hash this password
            );

            await _unitOfWork.TenantAdmins.AddAsync(tenantAdmin, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("TenantAdmin {Email} registered successfully in tenant {TenantId}", request.Email, tenantId);

            // Generate JWT token
            var accessToken = GenerateAccessToken(tenantAdmin.Id, tenantId, request.Email, new[] { "TenantAdmin" }, tenantAdmin.TokenVersion);
            var refreshToken = GenerateRefreshToken();

            await PersistRefreshTokenAsync(tenantAdmin.Id, "TenantAdmin", refreshToken, cancellationToken);

            var response = new AuthResponse(
                UserId: tenantAdmin.Id,
                Email: request.Email,
                FirstName: tenantAdmin.FirstName,
                LastName: tenantAdmin.LastName,
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresAt: DateTime.UtcNow.AddHours(1)
            );

            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration validation failed for email: {Email}", request.Email);
            throw; // Re-throw for controller to handle
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email: {Email}", request.Email);
            throw new InvalidOperationException("Registration failed. Please try again.", ex);
        }
    }

    /// <summary>
    /// Refreshes the authentication token.
    /// </summary>
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token cannot be empty", nameof(refreshToken));

        _logger.LogInformation("Token refresh requested");

        var tokenHash = AuthRefreshToken.Hash(refreshToken);
        var storedToken = await _context.AuthRefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.PrincipalType == "User", cancellationToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            _logger.LogWarning("Refresh token rejected because it is missing, expired, or revoked");
            throw new UnauthorizedAccessException("Refresh token is invalid or expired");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == storedToken.PrincipalId && u.IsActive, cancellationToken);
        if (user == null || user.TenantId == null)
        {
            throw new UnauthorizedAccessException("Refresh token principal is invalid");
        }

        var newRefreshToken = GenerateRefreshToken();
        storedToken.Revoke(newRefreshToken);
        await PersistRefreshTokenAsync(user.Id, "User", newRefreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var accessToken = GenerateAccessToken(user.Id, user.TenantId.Value, user.Email, new[] { user.Role.ToString() }, user.TokenVersion);

        var response = new AuthResponse(
            UserId: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            AccessToken: accessToken,
            RefreshToken: newRefreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );

        _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

        return response;
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
                GetJwtSecret()));

            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = GetJwtIssuer(),
                ValidateAudience = true,
                ValidAudience = GetJwtAudience(),
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
        var activeTokens = await _context.AuthRefreshTokens
            .Where(t => t.PrincipalId == userId && t.PrincipalType == "User" && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.Revoke();
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged out successfully: {UserId}, New Token Version: {TokenVersion}", userId, user.TokenVersion);
    }

    /// <summary>
    /// Generates a JWT access token.
    /// </summary>
    public string GenerateAccessToken(Guid userId, Guid tenantId, string email, string[] roles, int tokenVersion = 1)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            GetJwtSecret()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("email", email),
            new Claim("token_version", tokenVersion.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: GetJwtIssuer(),
            audience: GetJwtAudience(),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    private async Task PersistRefreshTokenAsync(Guid principalId, string principalType, string refreshToken, CancellationToken cancellationToken)
    {
        var expiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());
        var token = AuthRefreshToken.Create(principalId, principalType, refreshToken, expiresAt);
        await _context.AuthRefreshTokens.AddAsync(token, cancellationToken);
    }

    private int GetRefreshTokenExpirationDays()
    {
        var configured = _configuration["Auth:RefreshTokenExpirationDays"]
            ?? Environment.GetEnvironmentVariable("REFRESH_TOKEN_EXPIRATION_DAYS");
        return int.TryParse(configured, out var days) && days > 0 ? days : 7;
    }
    private string GetJwtSecret()
    {
        // Prioritize environment variable directly for Render deployment
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? _configuration["Auth:JwtSecret"]
            ?? _configuration["JWT_SECRET"];
        
        _logger.LogInformation("JWT Secret retrieved. Length: {Length}, IsNullOrWhiteSpace: {IsNullOrWhiteSpace}", 
            secret?.Length ?? 0, string.IsNullOrWhiteSpace(secret));
        
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT_SECRET is not configured or is empty");
        }
        
        return secret;
    }

    private string GetJwtIssuer()
    {
        return _configuration["Auth:Issuer"]
            ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? "KromicStore";
    }

    private string GenerateSubdomainFromEmail(string email)
    {
        // Extract the part before @ and use it as subdomain
        var atIndex = email.IndexOf('@');
        if (atIndex > 0)
        {
            var localPart = email.Substring(0, atIndex).ToLower();
            // Remove special characters and replace with hyphens
            var subdomain = System.Text.RegularExpressions.Regex.Replace(localPart, "[^a-z0-9]", "-");
            // Remove consecutive hyphens
            subdomain = System.Text.RegularExpressions.Regex.Replace(subdomain, "-+", "-");
            // Remove leading/trailing hyphens
            subdomain = subdomain.Trim('-');
            return string.IsNullOrEmpty(subdomain) ? "tenant" : subdomain;
        }
        return "tenant";
    }

    private string GetJwtAudience()
    {
        return _configuration["Auth:Audience"]
            ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? "KromicStore";
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
