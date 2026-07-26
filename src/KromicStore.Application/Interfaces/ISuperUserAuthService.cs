namespace KromicStore.Application.Interfaces;

using KromicStore.Contracts.V1.Auth;

/// <summary>
/// Interface for SuperUser authentication services (platform admins).
/// </summary>
public interface ISuperUserAuthService
{
    /// <summary>
    /// Authenticates a SuperUser with email and password.
    /// </summary>
    Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the SuperUser authentication token.
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a SuperUser JWT token.
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Generates a JWT access token for a SuperUser.
    /// </summary>
    string GenerateAccessToken(Guid superUserId, string email, string[] roles);
}
