namespace KromicStore.Contracts.V1.Auth;

/// <summary>
/// Represents a user registration request.
/// </summary>
public record RegisterRequest(
    /// <summary>
    /// The user's first name.
    /// </summary>
    string FirstName,
    
    /// <summary>
    /// The user's last name.
    /// </summary>
    string LastName,
    
    /// <summary>
    /// The user's email address (must be unique).
    /// </summary>
    string Email,
    
    /// <summary>
    /// The user's password (minimum 8 characters).
    /// </summary>
    string Password,
    
    /// <summary>
    /// Password confirmation - must match Password field.
    /// </summary>
    string ConfirmPassword);
