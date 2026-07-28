// Copyright (c) KromicStore. All rights reserved.

namespace KromicStore.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration for Brevo notification service proxy.
    /// All values must be configured via environment variables.
    /// </summary>
    public class NotificationProxyConfig
    {
        /// <summary>
        /// Gets or sets the Brevo API key for authentication.
        /// Environment variable: BREVO_API_KEY
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender email address for outgoing emails.
        /// Environment variable: BREVO_SENDER_EMAIL
        /// </summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender name for outgoing emails.
        /// Environment variable: BREVO_SENDER_NAME
        /// </summary>
        public string SenderName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base URL for Brevo API.
        /// Environment variable: BREVO_BASE_URL
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Brevo API version. Default: "v3".
        /// Environment variable: BREVO_API_VERSION
        /// </summary>
        public string ApiVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Brevo template ID for Order Placed email.
        /// Environment variable: BREVO_TEMPLATE_ORDER_PLACED
        /// </summary>
        public int TemplateOrderPlaced { get; set; }

        /// <summary>
        /// Gets or sets the Brevo template ID for Order Confirmed email.
        /// Environment variable: BREVO_TEMPLATE_ORDER_CONFIRMED
        /// </summary>
        public int TemplateOrderConfirmed { get; set; }

        /// <summary>
        /// Gets or sets the Brevo template ID for Order Dispatched email.
        /// Environment variable: BREVO_TEMPLATE_ORDER_DISPATCHED
        /// </summary>
        public int TemplateOrderDispatched { get; set; }

        /// <summary>
        /// Validates that all required configuration values are set.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ApiKey) &&
                   !string.IsNullOrWhiteSpace(SenderEmail) &&
                   !string.IsNullOrWhiteSpace(BaseUrl) &&
                   !string.IsNullOrWhiteSpace(SenderName) &&
                   !string.IsNullOrWhiteSpace(ApiVersion) &&
                   TemplateOrderPlaced > 0 &&
                   TemplateOrderConfirmed > 0 &&
                   TemplateOrderDispatched > 0;
        }

        /// <summary>
        /// Gets validation error messages if configuration is invalid.
        /// </summary>
        /// <returns>List of validation error messages.</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ApiKey))
                errors.Add("NotificationProxyConfig.ApiKey (BREVO_API_KEY) is required");

            if (string.IsNullOrWhiteSpace(SenderEmail))
                errors.Add("NotificationProxyConfig.SenderEmail (BREVO_SENDER_EMAIL) is required");

            if (string.IsNullOrWhiteSpace(SenderName))
                errors.Add("NotificationProxyConfig.SenderName (BREVO_SENDER_NAME) is required");

            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("NotificationProxyConfig.BaseUrl (BREVO_BASE_URL) is required");

            if (string.IsNullOrWhiteSpace(ApiVersion))
                errors.Add("NotificationProxyConfig.ApiVersion (BREVO_API_VERSION) is required");

            if (TemplateOrderPlaced <= 0)
                errors.Add("NotificationProxyConfig.TemplateOrderPlaced (BREVO_TEMPLATE_ORDER_PLACED) is required");

            if (TemplateOrderConfirmed <= 0)
                errors.Add("NotificationProxyConfig.TemplateOrderConfirmed (BREVO_TEMPLATE_ORDER_CONFIRMED) is required");

            if (TemplateOrderDispatched <= 0)
                errors.Add("NotificationProxyConfig.TemplateOrderDispatched (BREVO_TEMPLATE_ORDER_DISPATCHED) is required");

            return errors;
        }
    }
}
