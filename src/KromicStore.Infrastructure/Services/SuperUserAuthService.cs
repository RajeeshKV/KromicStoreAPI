namespace KromicStore.Infrastructure.Services;

using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Auth;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

/// <summary>
/// Service for SuperUser authentication (platform admins).
/// </summary>
public class SuperUserAuthService : ISuperUserAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SuperUserAuthService> _logger;

    public SuperUserAuthService(AppDbContext context, IConfiguration configuration, ILogger<SuperUserAuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var superUser = await _context.SuperUsers
            .FirstOrDefaultAsync(su => su.Email.ToLower() == email.ToLower() && su.IsActive, cancellationToken);

        if (superUser == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Verify password (using BCrypt or similar - for now simple comparison)
        if (!VerifyPassword(password, superUser.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Record login
        superUser.RecordLogin();
        await _context.SaveChangesAsync(cancellationToken);

        // Generate tokens with token version
        var accessToken = GenerateAccessToken(superUser.Id, superUser.Email, new[] { "SuperUser" }, superUser.TokenVersion);
        var refreshToken = GenerateRefreshToken();

        await PersistRefreshTokenAsync(superUser.Id, refreshToken, cancellationToken);

        var response = new AuthResponse(
            UserId: superUser.Id,
            Email: superUser.Email,
            FirstName: superUser.FirstName,
            LastName: superUser.LastName,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );

        return response;
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token cannot be empty", nameof(refreshToken));

        var tokenHash = AuthRefreshToken.Hash(refreshToken);
        var storedToken = await _context.AuthRefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.PrincipalType == "SuperUser", cancellationToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid or expired");
        }

        var superUser = await _context.SuperUsers
            .FirstOrDefaultAsync(su => su.Id == storedToken.PrincipalId && su.IsActive, cancellationToken);

        if (superUser == null)
        {
            throw new UnauthorizedAccessException("Refresh token principal is invalid");
        }

        var newRefreshToken = GenerateRefreshToken();
        storedToken.Revoke(newRefreshToken);
        await PersistRefreshTokenAsync(superUser.Id, newRefreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var accessToken = GenerateAccessToken(superUser.Id, superUser.Email, new[] { "SuperUser" }, superUser.TokenVersion);

        return new AuthResponse(
            UserId: superUser.Id,
            Email: superUser.Email,
            FirstName: superUser.FirstName,
            LastName: superUser.LastName,
            AccessToken: accessToken,
            RefreshToken: newRefreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );
    }

    /// <inheritdoc />
    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET not configured"));
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = GetJwtIssuer(),
                ValidateAudience = true,
                ValidAudience = GetJwtAudience(),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return Task.FromResult(principal != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public string GenerateAccessToken(Guid superUserId, string email, string[] roles, int tokenVersion = 1)
    {
        var key = Encoding.ASCII.GetBytes(_configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET not configured"));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", superUserId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, superUserId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "SuperUser"),
                new Claim("type", "superuser"),
                new Claim("token_version", tokenVersion.ToString())
            }),
            Issuer = GetJwtIssuer(),
            Audience = GetJwtAudience(),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(Guid superUserId, CancellationToken cancellationToken = default)
    {
        var superUser = await _context.SuperUsers
            .FirstOrDefaultAsync(su => su.Id == superUserId, cancellationToken);

        if (superUser == null)
        {
            _logger.LogWarning("SuperUser logout failed - user not found: {SuperUserId}", superUserId);
            throw new InvalidOperationException("SuperUser not found");
        }

        // Increment token version to invalidate all existing tokens
        superUser.IncrementTokenVersion();
        var activeTokens = await _context.AuthRefreshTokens
            .Where(t => t.PrincipalId == superUserId && t.PrincipalType == "SuperUser" && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.Revoke();
        }
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SuperUser logged out successfully: {SuperUserId}, New Token Version: {TokenVersion}", superUserId, superUser.TokenVersion);
    }


    private async Task PersistRefreshTokenAsync(Guid superUserId, string refreshToken, CancellationToken cancellationToken)
    {
        var expiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());
        var token = AuthRefreshToken.Create(superUserId, "SuperUser", refreshToken, expiresAt);
        await _context.AuthRefreshTokens.AddAsync(token, cancellationToken);
    }

    private int GetRefreshTokenExpirationDays()
    {
        var configured = _configuration["Auth:RefreshTokenExpirationDays"]
            ?? Environment.GetEnvironmentVariable("REFRESH_TOKEN_EXPIRATION_DAYS");
        return int.TryParse(configured, out var days) && days > 0 ? days : 7;
    }
    private string GetJwtIssuer()
    {
        return Environment.GetEnvironmentVariable("SUPERUSER_JWT_ISSUER")
            ?? _configuration["Auth:Issuer"]
            ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? "KromicStore";
    }

    private string GetJwtAudience()
    {
        return Environment.GetEnvironmentVariable("SUPERUSER_JWT_AUDIENCE")
            ?? _configuration["Auth:Audience"]
            ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? "KromicStore";
    }
    private string GenerateRefreshToken()
    {
        // Generate a random refresh token
        return Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();
    }

    private bool VerifyPassword(string password, string hash)
    {
        // Simple verification - in production use BCrypt or similar
        // For now, this is a placeholder
        return password == hash; // TODO: Implement proper password hashing
    }
}
