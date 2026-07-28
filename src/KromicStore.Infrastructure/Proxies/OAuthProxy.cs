using System.Text;
using System.Text.Json;
using KromicStore.Infrastructure.Proxies.Models;
using Microsoft.Extensions.Logging;

namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// Proxy for Google OAuth 2.0 integration
/// Handles authorization code exchange for access tokens, user profile retrieval, and token refresh with fault tolerance
/// All configuration is loaded from environment variables
/// </summary>
public class OAuthProxy : ServiceProxy<OAuthTokenResponse>
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private readonly string _tokenEndpoint;
    private readonly string _userInfoEndpoint;

    /// <summary>
    /// Initializes a new instance of the OAuthProxy class with environment variables
    /// </summary>
    public OAuthProxy(
        ILogger<OAuthProxy> logger,
        ICircuitBreaker circuitBreaker,
        HttpClient httpClient)
        : base(logger, circuitBreaker, timeoutSeconds: 15)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
            ?? throw new ArgumentException("GOOGLE_CLIENT_ID environment variable not configured");
        _clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
            ?? throw new ArgumentException("GOOGLE_CLIENT_SECRET environment variable not configured");
        _redirectUri = Environment.GetEnvironmentVariable("GOOGLE_REDIRECT_URI")
            ?? throw new ArgumentException("GOOGLE_REDIRECT_URI environment variable not configured");
        _tokenEndpoint = Environment.GetEnvironmentVariable("GOOGLE_TOKEN_ENDPOINT")
            ?? throw new ArgumentException("GOOGLE_TOKEN_ENDPOINT environment variable not configured");
        _userInfoEndpoint = Environment.GetEnvironmentVariable("GOOGLE_USER_INFO_ENDPOINT")
            ?? throw new ArgumentException("GOOGLE_USER_INFO_ENDPOINT environment variable not configured");
    }

    /// <summary>
    /// Exchanges a Google authorization code for access and refresh tokens
    /// </summary>
    /// <param name="authorizationCode">The authorization code from Google OAuth flow</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProxyResult containing token response with access and refresh tokens</returns>
    public async Task<ProxyResult<OAuthTokenResponse>> ExchangeCodeForTokenAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(authorizationCode))
            throw new ArgumentException("Authorization code is required", nameof(authorizationCode));

        Logger.LogInformation(
            "Exchanging authorization code for access token with Google OAuth");

        return await ExecuteAsync(async () =>
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", authorizationCode },
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "grant_type", "authorization_code" },
                { "redirect_uri", _redirectUri }
            });

            var response = await _httpClient.PostAsync(
                _tokenEndpoint,
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogWarning(
                    "Google OAuth token exchange failed ({StatusCode}): {ErrorContent}",
                    response.StatusCode,
                    errorContent);

                throw new ProxyException(
                    $"Google OAuth token exchange failed: {response.StatusCode}",
                    "GOOGLE_OAUTH_TOKEN_FAILED");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenData = JsonSerializer.Deserialize<OAuthTokenResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenData == null)
                throw new ProxyException("Failed to deserialize Google OAuth response", "DESERIALIZATION_ERROR");

            Logger.LogInformation(
                "Successfully exchanged authorization code for access token | ExpiresIn: {ExpiresIn}s",
                tokenData.ExpiresIn);

            return tokenData;
        },
        "ExchangeCodeForToken",
        cancellationToken);
    }

    /// <summary>
    /// Retrieves user profile information using an access token
    /// Note: This method requires a different proxy type since it returns GoogleUserProfile, not OAuthTokenResponse
    /// </summary>
    /// <param name="accessToken">The access token from OAuth flow</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>GoogleUserProfile containing user information</returns>
    public async Task<GoogleUserProfile> GetUserProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(accessToken))
            throw new ArgumentException("Access token is required", nameof(accessToken));

        Logger.LogInformation("Retrieving user profile from Google using access token");

        var request = new HttpRequestMessage(HttpMethod.Get, _userInfoEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(15));

        try
        {
            var response = await _httpClient.SendAsync(request, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogWarning(
                    "Failed to retrieve user profile ({StatusCode}): {ErrorContent}",
                    response.StatusCode,
                    errorContent);

                // Check if token is expired
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new ProxyException(
                        "Access token expired or invalid",
                        "GOOGLE_TOKEN_EXPIRED");
                }

                throw new ProxyException(
                    $"Failed to retrieve user profile: {response.StatusCode}",
                    "GOOGLE_PROFILE_FETCH_FAILED");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var userProfile = JsonSerializer.Deserialize<GoogleUserProfile>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userProfile == null)
                throw new ProxyException("Failed to deserialize user profile response", "DESERIALIZATION_ERROR");

            Logger.LogInformation(
                "Successfully retrieved user profile | Email: {Email}, Name: {Name}",
                userProfile.Email,
                userProfile.Name);

            return userProfile;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("GetUserProfile timed out after 15 seconds");
            throw new ProxyException("Google user profile request timed out", "GOOGLE_TIMEOUT");
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token from previous OAuth flow</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProxyResult containing new token response</returns>
    public async Task<ProxyResult<OAuthTokenResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new ArgumentException("Refresh token is required", nameof(refreshToken));

        Logger.LogInformation("Refreshing access token using Google OAuth refresh token");

        return await ExecuteAsync(async () =>
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            });

            var response = await _httpClient.PostAsync(
                _tokenEndpoint,
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogWarning(
                    "Google OAuth token refresh failed ({StatusCode}): {ErrorContent}",
                    response.StatusCode,
                    errorContent);

                throw new ProxyException(
                    $"Google OAuth token refresh failed: {response.StatusCode}",
                    "GOOGLE_REFRESH_TOKEN_FAILED");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenData = JsonSerializer.Deserialize<OAuthTokenResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenData == null)
                throw new ProxyException("Failed to deserialize token refresh response", "DESERIALIZATION_ERROR");

            Logger.LogInformation(
                "Successfully refreshed access token | ExpiresIn: {ExpiresIn}s",
                tokenData.ExpiresIn);

            return tokenData;
        },
        "RefreshToken",
        cancellationToken);
    }
}
