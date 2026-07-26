namespace KromicStore.Contracts.V1.Auth;

/// <summary>
/// Represents a token refresh request used to obtain a new access token.
/// </summary>
public record RefreshTokenRequest(
    /// <summary>
    /// The refresh token received from a previous authentication response.
    /// </summary>
    string RefreshToken);
