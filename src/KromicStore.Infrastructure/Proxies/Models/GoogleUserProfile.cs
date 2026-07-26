#nullable enable

using System.Text.Json.Serialization;

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Represents a user profile retrieved from Google OAuth 2.0 API.
/// Contains user identification and profile information.
/// </summary>
public class GoogleUserProfile
{
    /// <summary>
    /// The unique Google user ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The user's email address
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Whether the email address has been verified by Google
    /// </summary>
    [JsonPropertyName("verified_email")]
    public bool VerifiedEmail { get; set; }

    /// <summary>
    /// The user's full name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL to the user's profile picture
    /// </summary>
    [JsonPropertyName("picture")]
    public string? Picture { get; set; }

    /// <summary>
    /// The user's first name
    /// </summary>
    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    /// <summary>
    /// The user's last name
    /// </summary>
    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }

    /// <summary>
    /// Locale preference (e.g., "en")
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }
}
