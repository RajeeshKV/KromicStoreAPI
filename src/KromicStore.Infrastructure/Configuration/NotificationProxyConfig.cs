// Copyright (c) KromicStore. All rights reserved.

namespace KromicStore.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration for Brevo notification service proxy.
    /// </summary>
    public class NotificationProxyConfig
    {
        /// <summary>
        /// Gets or sets the Brevo API key for authentication.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender email address for outgoing emails.
        /// </summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender name for outgoing emails.
        /// </summary>
        public string SenderName { get; set; } = "KromicStore";

        /// <summary>
        /// Gets or sets the base URL for Brevo API.
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.brevo.com";

        /// <summary>
        /// Gets or sets the Brevo API version. Default: "v3".
        /// </summary>
        public string ApiVersion { get; set; } = "v3";

        /// <summary>
        /// Validates that all required configuration values are set.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ApiKey) &&
                   !string.IsNullOrWhiteSpace(SenderEmail) &&
                   !string.IsNullOrWhiteSpace(BaseUrl);
        }

        /// <summary>
        /// Gets validation error messages if configuration is invalid.
        /// </summary>
        /// <returns>List of validation error messages.</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ApiKey))
                errors.Add("NotificationProxyConfig.ApiKey is required");

            if (string.IsNullOrWhiteSpace(SenderEmail))
                errors.Add("NotificationProxyConfig.SenderEmail is required");

            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("NotificationProxyConfig.BaseUrl is required");

            return errors;
        }
    }
}
