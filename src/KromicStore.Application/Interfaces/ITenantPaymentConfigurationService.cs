// Copyright (c) KromicStore. All rights reserved.

namespace KromicStore.Application.Interfaces;

/// <summary>
/// Interface for managing tenant payment configurations (Razorpay credentials).
/// </summary>
public interface ITenantPaymentConfigurationService
{
    /// <summary>
    /// Saves encrypted Razorpay credentials for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="razorpayKeyId">The Razorpay Key ID.</param>
    /// <param name="razorpayKeySecret">The Razorpay Key Secret.</param>
    /// <param name="razorpayWebhookSecret">The Razorpay Webhook Secret.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration saved successfully with DTO response.</returns>
    Task<ServiceResult<TenantPaymentMethodDto>> SavePaymentConfigurationAsync(
        Guid tenantId,
        string razorpayKeyId,
        string razorpayKeySecret,
        string razorpayWebhookSecret,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves payment configuration for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payment configuration details (without secrets).</returns>
    Task<ServiceResult<TenantPaymentMethodDto>> GetPaymentConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes payment configuration for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<ServiceResult<bool>> DeletePaymentConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates payment credentials with Razorpay.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if credentials are valid and connection successful.</returns>
    Task<ServiceResult<bool>> ValidateCredentialsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if tenant has configured payment method.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if tenant has payment configuration.</returns>
    Task<ServiceResult<bool>> HasPaymentConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for payment configuration response.
/// </summary>
public record TenantPaymentMethodDto(
    Guid Id,
    Guid TenantId,
    bool IsConfigured,
    bool IsEnabled,
    bool TestModeEnabled,
    DateTime? UpdatedAt,
    DateTime? LastTestedAt = null,
    string Provider = "Razorpay");
