namespace KromicStore.Contracts.V1.Auth;

/// <summary>
/// Represents a user login request containing email and password credentials.
/// </summary>
public record LoginRequest(
    /// <summary>
    /// The user's email address.
    /// </summary>
    string Email,
    
    /// <summary>
    /// The user's password.
    /// </summary>
    string Password);
