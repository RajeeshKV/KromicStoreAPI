// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Seeds default configuration settings for newly registered tenants.
/// Initializes configuration for notifications, webhooks, features, payment provider, and other settings.
/// Creates corresponding ConfigurationAuditLog entries with system user as changer.
/// </summary>
public class TenantConfigurationSeeder
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantConfigurationSeeder> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// System user ID for audit logging of initial configurations.
    /// Uses a well-known GUID for system-initiated changes.
    /// </summary>
    private static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Initializes a new instance of the TenantConfigurationSeeder class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work for data persistence</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <param name="configuration">Application configuration for reading default values</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
    public TenantConfigurationSeeder(
        IUnitOfWork unitOfWork,
        ILogger<TenantConfigurationSeeder> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Seeds default configuration for a newly registered tenant.
    /// Initializes settings for:
    /// - Notifications (email enabled/disabled)
    /// - Webhooks (enabled by default)
    /// - Features (all enabled for trial subscriptions)
    /// - Subscription limits based on plan
    /// - Email templates (Brevo template IDs)
    /// - Payment provider settings (Razorpay)
    /// - Currency (based on country if available, defaults to USD)
    /// - Timezone (if configured)
    /// 
    /// All configuration changes are logged to ConfigurationAuditLog with system user ID.
    /// </summary>
    /// <param name="tenantId">ID of the newly registered tenant</param>
    /// <param name="country">Optional country code for currency/timezone defaults (ISO 3166-1 alpha-2)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Task representing the async operation</returns>
    /// <exception cref="ArgumentException">Thrown when tenantId is empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when seeding fails or configuration is invalid</exception>
    public async Task SeedDefaultConfigurationAsync(
        Guid tenantId,
        string? country = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        _logger.LogInformation("Starting default configuration seeding for tenant: {TenantId}", tenantId);

        try
        {
            // Build default configuration dictionary
            var defaultConfigs = BuildDefaultConfigurations(tenantId, country);

            // Create and persist configuration entries
            foreach (var (key, value, isEncrypted) in defaultConfigs)
            {
                var config = TenantConfiguration.Create(
                    tenantId: tenantId,
                    configKey: key,
                    configValue: value,
                    scope: ConfigScope.Tenant,
                    isEncrypted: isEncrypted,
                    expiresAt: null);

                await _unitOfWork.TenantConfigurations.AddAsync(config, cancellationToken);

                // Create audit log entry for the initial configuration
                var auditLog = ConfigurationAuditLog.Create(
                    tenantId: tenantId,
                    configurationKey: key,
                    oldValue: null, // No previous value for initial config
                    newValue: value,
                    changedBy: SystemUserId,
                    reason: "Initial configuration on tenant registration");

                await _unitOfWork.ConfigurationAuditLogs.AddAsync(auditLog, cancellationToken);

                _logger.LogDebug("Configured {ConfigKey} for tenant {TenantId}", key, tenantId);
            }

            // Persist all configuration and audit entries
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Default configuration seeding completed successfully for tenant: {TenantId}. Total configs: {Count}",
                tenantId,
                defaultConfigs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Default configuration seeding failed for tenant: {TenantId}. Error: {Message}",
                tenantId,
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Builds the complete set of default configurations for a new tenant.
    /// Returns tuples of (ConfigKey, ConfigValue, IsEncrypted).
    /// </summary>
    private List<(string Key, string Value, bool IsEncrypted)> BuildDefaultConfigurations(Guid tenantId, string? country)
    {
        var configs = new List<(string, string, bool)>();

        // 1. Notification Settings
        configs.Add(("notifications:email_enabled", "true", false));
        configs.Add(("notifications:email_frequency", "immediate", false)); // immediate, daily, weekly
        configs.Add(("notifications:order_confirmation_enabled", "true", false));
        configs.Add(("notifications:shipment_notification_enabled", "true", false));
        configs.Add(("notifications:delivery_notification_enabled", "true", false));
        configs.Add(("notifications:payment_failure_notification_enabled", "true", false));
        configs.Add(("notifications:newsletter_enabled", "false", false));

        // Email template IDs from appsettings (Brevo)
        var welcomeTemplateId = _configuration["ExternalServices:Brevo:TemplateIds:WelcomeEmail"] ?? "0";
        var orderConfirmationTemplateId = _configuration["ExternalServices:Brevo:TemplateIds:OrderConfirmation"] ?? "0";
        var shipmentNotificationTemplateId = _configuration["ExternalServices:Brevo:TemplateIds:ShipmentNotification"] ?? "0";
        var paymentFailureTemplateId = _configuration["ExternalServices:Brevo:TemplateIds:PaymentFailure"] ?? "0";

        configs.Add(("notifications:welcome_email_template_id", welcomeTemplateId, false));
        configs.Add(("notifications:order_confirmation_template_id", orderConfirmationTemplateId, false));
        configs.Add(("notifications:shipment_notification_template_id", shipmentNotificationTemplateId, false));
        configs.Add(("notifications:payment_failure_template_id", paymentFailureTemplateId, false));

        // 2. Webhook Settings
        configs.Add(("webhooks:enabled", "true", false));
        configs.Add(("webhooks:retry_enabled", "true", false));
        configs.Add(("webhooks:max_retry_count", "5", false));
        configs.Add(("webhooks:retry_delays_ms", "1000,10000,100000,1000000,10000000", false)); // 1s, 10s, 100s, 1000s, 10000s
        configs.Add(("webhooks:delivery_timeout_seconds", "30", false));
        configs.Add(("webhooks:event_retention_days", "90", false));

        // 3. Feature Flags (all enabled for trial)
        configs.Add(("features:products_enabled", "true", false));
        configs.Add(("features:orders_enabled", "true", false));
        configs.Add(("features:customers_enabled", "true", false));
        configs.Add(("features:payments_enabled", "true", false));
        configs.Add(("features:webhooks_enabled", "true", false));
        configs.Add(("features:analytics_enabled", "false", false)); // Disabled for Starter plan
        configs.Add(("features:api_access_enabled", "true", false));
        configs.Add(("features:bulk_operations_enabled", "false", false)); // Premium feature

        // 4. Payment Provider Configuration (Razorpay)
        configs.Add(("payment:provider", "razorpay", false));
        configs.Add(("payment:razorpay_enabled", "true", false));

        var razorpayApiKey = _configuration["ExternalServices:Razorpay:ApiKey"] ?? "";
        var razorpayEndpoint = _configuration["ExternalServices:Razorpay:Endpoint"] ?? "https://api.razorpay.com/v1/";

        configs.Add(("payment:razorpay_endpoint", razorpayEndpoint, false));
        // Note: API keys should not be stored at tenant level - managed at platform level

        // 5. Currency Configuration (based on country or default to USD)
        var currency = GetCurrencyForCountry(country);
        configs.Add(("currency:default", currency, false));
        configs.Add(("currency:support_multiple", "false", false));

        // 6. Timezone Configuration
        var timezone = GetTimezoneForCountry(country) ?? "UTC";
        configs.Add(("timezone:default", timezone, false));

        // 7. API Rate Limiting (based on subscription plan)
        configs.Add(("api:rate_limit_per_minute", "100", false)); // Starter plan default
        configs.Add(("api:rate_limit_per_day", "10000", false)); // Approximately 100 requests/min * 100 mins
        configs.Add(("api:max_requests_per_call", "1000", false));

        // 8. Compliance & Security Settings
        configs.Add(("compliance:gdpr_enabled", "true", false));
        configs.Add(("compliance:data_retention_days", "365", false));
        configs.Add(("security:require_2fa", "false", false));
        configs.Add(("security:password_expiry_days", "90", false));

        // 9. Catalog Settings
        configs.Add(("catalog:product_image_compression_enabled", "true", false));
        configs.Add(("catalog:product_image_max_size_mb", "5", false));
        configs.Add(("catalog:product_variants_enabled", "false", false)); // Premium feature
        configs.Add(("catalog:bulk_import_enabled", "false", false)); // Premium feature

        // 10. Order & Fulfillment Settings
        configs.Add(("orders:default_shipping_carrier", "standard", false));
        configs.Add(("orders:auto_confirm_enabled", "false", false));
        configs.Add(("orders:inventory_tracking_enabled", "true", false));
        configs.Add(("orders:reorder_level_threshold", "5", false));

        // 11. Customer Settings
        configs.Add(("customers:email_verification_required", "false", false));
        configs.Add(("customers:newsletter_opt_in_default", "false", false));

        // 12. Analytics & Reporting (disabled for Starter)
        configs.Add(("analytics:enabled", "false", false));
        configs.Add(("analytics:retention_days", "30", false));
        configs.Add(("reporting:custom_reports_enabled", "false", false));

        // 13. Support & Documentation
        configs.Add(("support:email", _configuration["ExternalServices:Brevo:SenderEmail"] ?? "support@kromicstore.com", false));
        configs.Add(("support:chat_enabled", "false", false));

        // 14. Marketing & Communication
        configs.Add(("marketing:promotional_emails_enabled", "false", false));
        configs.Add(("marketing:abandoned_cart_email_enabled", "false", false));

        return configs;
    }

    /// <summary>
    /// Determines the currency code for a given country.
    /// Maps ISO 3166-1 alpha-2 country codes to ISO 4217 currency codes.
    /// </summary>
    private static string GetCurrencyForCountry(string? country)
    {
        return country?.ToUpper() switch
        {
            "US" => "USD", // United States
            "CA" => "CAD", // Canada
            "GB" => "GBP", // United Kingdom
            "EU" or "DE" or "FR" or "IT" or "ES" or "NL" or "BE" or "AT" or "PT" or "GR" => "EUR", // Eurozone
            "IN" => "INR", // India
            "AU" => "AUD", // Australia
            "JP" => "JPY", // Japan
            "CN" => "CNY", // China
            "BR" => "BRL", // Brazil
            "MX" => "MXN", // Mexico
            "SG" => "SGD", // Singapore
            "HK" => "HKD", // Hong Kong
            "NZ" => "NZD", // New Zealand
            "ZA" => "ZAR", // South Africa
            "CH" => "CHF", // Switzerland
            "SE" => "SEK", // Sweden
            "NO" => "NOK", // Norway
            "DK" => "DKK", // Denmark
            "PL" => "PLN", // Poland
            "CZ" => "CZK", // Czech Republic
            "TR" => "TRY", // Turkey
            "RU" => "RUB", // Russia
            "KR" => "KRW", // South Korea
            "TH" => "THB", // Thailand
            "MY" => "MYR", // Malaysia
            "PH" => "PHP", // Philippines
            "ID" => "IDR", // Indonesia
            "VN" => "VND", // Vietnam
            "PK" => "PKR", // Pakistan
            "BD" => "BDT", // Bangladesh
            "AE" => "AED", // United Arab Emirates
            "SA" => "SAR", // Saudi Arabia
            "IL" => "ILS", // Israel
            "NG" => "NGN", // Nigeria
            "EG" => "EGP", // Egypt
            _ => "USD" // Default to USD
        };
    }

    /// <summary>
    /// Determines the timezone for a given country.
    /// Maps ISO 3166-1 alpha-2 country codes to IANA timezone identifiers.
    /// </summary>
    private static string? GetTimezoneForCountry(string? country)
    {
        return country?.ToUpper() switch
        {
            "US" => "America/Chicago", // Central timezone as default
            "CA" => "America/Toronto",
            "GB" => "Europe/London",
            "DE" => "Europe/Berlin",
            "FR" => "Europe/Paris",
            "IT" => "Europe/Rome",
            "ES" => "Europe/Madrid",
            "NL" => "Europe/Amsterdam",
            "BE" => "Europe/Brussels",
            "AT" => "Europe/Vienna",
            "PT" => "Europe/Lisbon",
            "GR" => "Europe/Athens",
            "IN" => "Asia/Kolkata",
            "AU" => "Australia/Sydney",
            "JP" => "Asia/Tokyo",
            "CN" => "Asia/Shanghai",
            "BR" => "America/Sao_Paulo",
            "MX" => "America/Mexico_City",
            "SG" => "Asia/Singapore",
            "HK" => "Asia/Hong_Kong",
            "NZ" => "Pacific/Auckland",
            "ZA" => "Africa/Johannesburg",
            "CH" => "Europe/Zurich",
            "SE" => "Europe/Stockholm",
            "NO" => "Europe/Oslo",
            "DK" => "Europe/Copenhagen",
            "PL" => "Europe/Warsaw",
            "CZ" => "Europe/Prague",
            "TR" => "Europe/Istanbul",
            "RU" => "Europe/Moscow",
            "KR" => "Asia/Seoul",
            "TH" => "Asia/Bangkok",
            "MY" => "Asia/Kuala_Lumpur",
            "PH" => "Asia/Manila",
            "ID" => "Asia/Jakarta",
            "VN" => "Asia/Ho_Chi_Minh",
            "PK" => "Asia/Karachi",
            "BD" => "Asia/Dhaka",
            "AE" => "Asia/Dubai",
            "SA" => "Asia/Riyadh",
            "IL" => "Asia/Jerusalem",
            "NG" => "Africa/Lagos",
            "EG" => "Africa/Cairo",
            _ => null
        };
    }
}
