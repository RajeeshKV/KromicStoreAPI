namespace KromicStore.Contracts.V1.Auth;

/// <summary>
/// Represents an authentication response returned after successful login or registration.
/// Includes authentication tokens and user information.
/// </summary>
public record AuthResponse(
    /// <summary>
    /// The unique identifier of the authenticated user.
    /// </summary>
    Guid UserId,
    
    /// <summary>
    /// The user's email address.
    /// </summary>
    string Email,
    
    /// <summary>
    /// The user's first name.
    /// </summary>
    string FirstName,
    
    /// <summary>
    /// The user's last name.
    /// </summary>
    string LastName,
    
    /// <summary>
    /// JWT access token for API authentication (typically valid for 1 hour).
    /// </summary>
    string AccessToken,
    
    /// <summary>
    /// Refresh token used to obtain a new access token (typically valid for 30 days).
    /// </summary>
    string RefreshToken,
    
    /// <summary>
    /// The UTC timestamp when the access token expires.
    /// </summary>
    DateTime ExpiresAt);
