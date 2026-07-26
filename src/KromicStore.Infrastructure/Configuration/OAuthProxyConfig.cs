// Copyright (c) KromicStore. All rights reserved.

namespace KromicStore.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration for Google OAuth proxy.
    /// </summary>
    public class OAuthProxyConfig
    {
        /// <summary>
        /// Gets or sets the Google OAuth token endpoint URL.
        /// </summary>
        public string TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";

        /// <summary>
        /// Gets or sets the Google OAuth user info endpoint URL.
        /// </summary>
        public string UserInfoEndpoint { get; set; } = "https://www.googleapis.com/oauth2/v2/userinfo";

        /// <summary>
        /// Gets or sets the Google OAuth client ID.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Google OAuth client secret.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the redirect URI for OAuth callback.
        /// </summary>
        public string RedirectUri { get; set; } = string.Empty;

        /// <summary>
        /// Validates that all required configuration values are set.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(TokenEndpoint) &&
                   !string.IsNullOrWhiteSpace(UserInfoEndpoint) &&
                   !string.IsNullOrWhiteSpace(ClientId) &&
                   !string.IsNullOrWhiteSpace(ClientSecret) &&
                   !string.IsNullOrWhiteSpace(RedirectUri);
        }

        /// <summary>
        /// Gets validation error messages if configuration is invalid.
        /// </summary>
        /// <returns>List of validation error messages.</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TokenEndpoint))
                errors.Add("OAuthProxyConfig.TokenEndpoint is required");

            if (string.IsNullOrWhiteSpace(UserInfoEndpoint))
                errors.Add("OAuthProxyConfig.UserInfoEndpoint is required");

            if (string.IsNullOrWhiteSpace(ClientId))
                errors.Add("OAuthProxyConfig.ClientId is required");

            if (string.IsNullOrWhiteSpace(ClientSecret))
                errors.Add("OAuthProxyConfig.ClientSecret is required");

            if (string.IsNullOrWhiteSpace(RedirectUri))
                errors.Add("OAuthProxyConfig.RedirectUri is required");

            return errors;
        }
    }
}
