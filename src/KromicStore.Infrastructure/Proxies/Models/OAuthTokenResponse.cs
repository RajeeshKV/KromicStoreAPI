#nullable enable

using System.Text.Json.Serialization;

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Represents an OAuth token response from Google OAuth 2.0 endpoint.
/// Contains access token, refresh token, and expiration information.
/// </summary>
public class OAuthTokenResponse
{
    /// <summary>
    /// The access token obtained from the authorization server.
    /// Used to access protected resources on behalf of the user.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The lifetime in seconds of the access token.
    /// Typically 3600 (1 hour) for Google OAuth.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// The type of the token issued. Usually "Bearer".
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// The refresh token used to obtain a new access token.
    /// Only returned on first authorization. Persists across token refreshes.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Scope of access granted. Space-separated list of permissions.
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// Indicates if this token is being rotated (used for secure token rotation).
    /// </summary>
    public bool IsRotated { get; set; }

    /// <summary>
    /// Timestamp when the token was obtained (UTC).
    /// Used to calculate expiration without relying on client clock.
    /// </summary>
    public DateTime ObtainedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calculates the expiration time for this token.
    /// Includes a 5-minute buffer for proactive refresh before actual expiration.
    /// </summary>
    /// <returns>DateTime when token should be considered expired</returns>
    public DateTime CalculateExpirationTime(int bufferSeconds = 300)
    {
        return ObtainedAt.AddSeconds(ExpiresIn - bufferSeconds);
    }

    /// <summary>
    /// Determines if the token is considered expired (including buffer).
    /// </summary>
    /// <returns>True if token is expired or expiration is imminent</returns>
    public bool IsExpired(int bufferSeconds = 300)
    {
        return DateTime.UtcNow >= CalculateExpirationTime(bufferSeconds);
    }
}
