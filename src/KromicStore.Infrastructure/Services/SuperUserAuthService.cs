namespace KromicStore.Infrastructure.Services;

using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Auth;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    public SuperUserAuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

        // Generate tokens
        var accessToken = GenerateAccessToken(superUser.Id, superUser.Email, new[] { "SuperUser" });
        var refreshToken = GenerateRefreshToken();

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
        // For simplicity, validate refresh token and issue new access token
        // In production, store refresh tokens in database with expiration
        throw new NotImplementedException("Refresh token flow not yet implemented");
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
                ValidateIssuer = false,
                ValidateAudience = false,
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
    public string GenerateAccessToken(Guid superUserId, string email, string[] roles)
    {
        var key = Encoding.ASCII.GetBytes(_configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET not configured"));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, superUserId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "SuperUser"),
                new Claim("type", "superuser")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
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
