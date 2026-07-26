// Copyright (c) KromicStore. All rights reserved.

namespace KromicStore.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration for Razorpay payment gateway proxy.
    /// </summary>
    public class PaymentProxyConfig
    {
        /// <summary>
        /// Gets or sets the Razorpay API endpoint URL.
        /// </summary>
        public string Endpoint { get; set; } = "https://api.razorpay.com/v1/";

        /// <summary>
        /// Gets or sets the Razorpay API key (KeyId).
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Razorpay API secret (KeySecret).
        /// </summary>
        public string ApiSecret { get; set; } = string.Empty;

        /// <summary>
        /// Validates that all required configuration values are set.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Endpoint) &&
                   !string.IsNullOrWhiteSpace(ApiKey) &&
                   !string.IsNullOrWhiteSpace(ApiSecret);
        }

        /// <summary>
        /// Gets validation error messages if configuration is invalid.
        /// </summary>
        /// <returns>List of validation error messages.</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Endpoint))
                errors.Add("PaymentProxyConfig.Endpoint is required");

            if (string.IsNullOrWhiteSpace(ApiKey))
                errors.Add("PaymentProxyConfig.ApiKey is required");

            if (string.IsNullOrWhiteSpace(ApiSecret))
                errors.Add("PaymentProxyConfig.ApiSecret is required");

            return errors;
        }
    }
}
