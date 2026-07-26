namespace KromicStore.Application.Interfaces;

using KromicStore.Contracts.V1.Auth;

/// <summary>
/// Interface for authentication services.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    Task<AuthResponse> LoginAsync(Guid tenantId, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user.
    /// </summary>
    Task<AuthResponse> RegisterAsync(Guid tenantId, RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the authentication token.
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user with external provider (Google, GitHub, etc.).
    /// </summary>
    Task<AuthResponse> OAuthLoginAsync(Guid tenantId, string provider, string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token.
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Generates a JWT access token for a user.
    /// </summary>
    string GenerateAccessToken(Guid userId, Guid tenantId, string email, string[] roles);
}
