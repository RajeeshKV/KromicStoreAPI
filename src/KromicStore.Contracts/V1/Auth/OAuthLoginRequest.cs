namespace KromicStore.Contracts.V1.Auth;

/// <summary>
/// Represents an OAuth login request for third-party authentication (e.g., Google).
/// </summary>
public record OAuthLoginRequest(
    /// <summary>
    /// The OAuth provider name (e.g., "Google", "Github").
    /// </summary>
    string Provider,
    
    /// <summary>
    /// The authorization token/code obtained from the OAuth provider.
    /// </summary>
    string Token);
