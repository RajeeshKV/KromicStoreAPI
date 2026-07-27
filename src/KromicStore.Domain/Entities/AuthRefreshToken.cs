namespace KromicStore.Domain.Entities;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Stores hashed refresh tokens for revocation, rotation, and session tracking.
/// </summary>
public class AuthRefreshToken : BaseEntity
{
    public Guid PrincipalId { get; private set; }
    public string PrincipalType { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    private AuthRefreshToken()
    {
    }

    public static AuthRefreshToken Create(
        Guid principalId,
        string principalType,
        string refreshToken,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (principalId == Guid.Empty)
            throw new ArgumentException("Principal ID is required.", nameof(principalId));
        if (string.IsNullOrWhiteSpace(principalType))
            throw new ArgumentException("Principal type is required.", nameof(principalType));
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));

        return new AuthRefreshToken
        {
            PrincipalId = principalId,
            PrincipalType = principalType.Trim(),
            TokenHash = Hash(refreshToken),
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    public void Revoke(string? replacementRefreshToken = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = string.IsNullOrWhiteSpace(replacementRefreshToken) ? null : Hash(replacementRefreshToken);
        UpdateTimestamp();
    }

    public bool Matches(string refreshToken)
    {
        return string.Equals(TokenHash, Hash(refreshToken), StringComparison.Ordinal);
    }

    public static string Hash(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}