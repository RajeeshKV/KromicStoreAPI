// Copyright (c) KromicStore. All rights reserved.

namespace KromicStore.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration for Cloudinary media service proxy.
    /// </summary>
    public class MediaProxyConfig
    {
        /// <summary>
        /// Gets or sets the Cloudinary cloud name.
        /// </summary>
        public string CloudName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Cloudinary API key.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Cloudinary API secret.
        /// </summary>
        public string ApiSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base URL for Cloudinary API.
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.cloudinary.com";

        /// <summary>
        /// Gets or sets the folder path for storing assets in Cloudinary.
        /// </summary>
        public string FolderPath { get; set; } = "kromic-store";

        /// <summary>
        /// Gets or sets the default image quality. Default: "auto".
        /// </summary>
        public string Quality { get; set; } = "auto";

        /// <summary>
        /// Validates that all required configuration values are set.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(CloudName) &&
                   !string.IsNullOrWhiteSpace(ApiKey) &&
                   !string.IsNullOrWhiteSpace(ApiSecret) &&
                   !string.IsNullOrWhiteSpace(BaseUrl);
        }

        /// <summary>
        /// Gets validation error messages if configuration is invalid.
        /// </summary>
        /// <returns>List of validation error messages.</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(CloudName))
                errors.Add("MediaProxyConfig.CloudName is required");

            if (string.IsNullOrWhiteSpace(ApiKey))
                errors.Add("MediaProxyConfig.ApiKey is required");

            if (string.IsNullOrWhiteSpace(ApiSecret))
                errors.Add("MediaProxyConfig.ApiSecret is required");

            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("MediaProxyConfig.BaseUrl is required");

            return errors;
        }
    }
}
