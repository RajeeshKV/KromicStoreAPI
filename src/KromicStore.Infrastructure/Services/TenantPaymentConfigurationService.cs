// Copyright (c) KromicStore. All rights reserved.

using Microsoft.Extensions.Logging;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;

namespace KromicStore.Infrastructure.Services;

/// <summary>
/// Service for managing tenant payment configurations.
/// Stores and retrieves encrypted Razorpay credentials per tenant.
/// </summary>
public class TenantPaymentConfigurationService : ITenantPaymentConfigurationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly IRazorpayService _razorpayService;
    private readonly ILogger<TenantPaymentConfigurationService> _logger;

    /// <summary>
    /// Initializes a new instance of TenantPaymentConfigurationService.
    /// </summary>
    public TenantPaymentConfigurationService(
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService,
        IRazorpayService razorpayService,
        ILogger<TenantPaymentConfigurationService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _razorpayService = razorpayService ?? throw new ArgumentNullException(nameof(razorpayService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<TenantPaymentMethodDto>> SavePaymentConfigurationAsync(
        Guid tenantId,
        string razorpayKeyId,
        string razorpayKeySecret,
        string razorpayWebhookSecret,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (tenantId == Guid.Empty)
                return ServiceResult<TenantPaymentMethodDto>.FailureResult("Tenant ID is required");

            if (string.IsNullOrWhiteSpace(razorpayKeyId))
                return ServiceResult<TenantPaymentMethodDto>.FailureResult("Razorpay Key ID is required");

            if (string.IsNullOrWhiteSpace(razorpayKeySecret))
                return ServiceResult<TenantPaymentMethodDto>.FailureResult("Razorpay Key Secret is required");

            if (string.IsNullOrWhiteSpace(razorpayWebhookSecret))
                return ServiceResult<TenantPaymentMethodDto>.FailureResult("Razorpay Webhook Secret is required");

            // Check tenant exists
            var tenants = await _unitOfWork.Tenants.FindAsync(t => t.Id == tenantId, cancellationToken);
            var tenant = tenants.FirstOrDefault();
            if (tenant == null)
                return ServiceResult<TenantPaymentMethodDto>.FailureResult("Tenant not found");

            // Encrypt credentials
            var encryptedKeyId = await _encryptionService.EncryptAsync(razorpayKeyId, cancellationToken);
            var encryptedKeySecret = await _encryptionService.EncryptAsync(razorpayKeySecret, cancellationToken);
            var encryptedWebhookSecret = await _encryptionService.EncryptAsync(razorpayWebhookSecret, cancellationToken);

            // Check if configuration already exists
            var methods = await _unitOfWork.TenantPaymentMethods
                .FindAsync(m => m.TenantId == tenantId, cancellationToken);
            var existingMethod = methods.FirstOrDefault();

            if (existingMethod != null)
            {
                // Update existing
                existingMethod.UpdateCredentials(encryptedKeyId, encryptedKeySecret, encryptedWebhookSecret);
                _unitOfWork.TenantPaymentMethods.Update(existingMethod);
                _logger.LogInformation("Updated payment configuration for tenant {TenantId}", tenantId);
            }
            else
            {
                // Create new
                var newMethod = TenantPaymentMethod.Create(
                    tenantId,
                    "razorpay",
                    encryptedKeyId,
                    encryptedKeySecret,
                    encryptedWebhookSecret);
                
                await _unitOfWork.TenantPaymentMethods.AddAsync(newMethod, cancellationToken);
                _logger.LogInformation("Created payment configuration for tenant {TenantId}", tenantId);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var methodId = existingMethod?.Id ?? Guid.NewGuid();
            var isEnabled = existingMethod?.IsEnabled ?? true;
            var testMode = existingMethod?.TestModeEnabled ?? false;

            return ServiceResult<TenantPaymentMethodDto>.SuccessResult(
                new TenantPaymentMethodDto(
                    methodId,
                    tenantId,
                    true,
                    isEnabled,
                    testMode,
                    DateTime.UtcNow,
                    existingMethod?.LastTestedAt,
                    "Razorpay"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payment configuration for tenant {TenantId}", tenantId);
            return ServiceResult<TenantPaymentMethodDto>.FailureResult($"Error saving configuration: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<TenantPaymentMethodDto>> GetPaymentConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (tenantId == Guid.Empty)
                return ServiceResult<TenantPaymentMethodDto>.FailureResult("Tenant ID is required");

            var methods = await _unitOfWork.TenantPaymentMethods
                .FindAsync(m => m.TenantId == tenantId, cancellationToken);
            var method = methods.FirstOrDefault();

            if (method == null)
                return ServiceResult<TenantPaymentMethodDto>.FailureResult("No payment configuration found");

            _logger.LogInformation("Retrieved payment configuration for tenant {TenantId}", tenantId);

            return ServiceResult<TenantPaymentMethodDto>.SuccessResult(
                new TenantPaymentMethodDto(
                    method.Id,
                    tenantId,
                    true,
                    method.IsEnabled,
                    method.TestModeEnabled,
                    method.UpdatedAt,
                    method.LastTestedAt,
                    method.Provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment configuration for tenant {TenantId}", tenantId);
            return ServiceResult<TenantPaymentMethodDto>.FailureResult($"Error retrieving configuration: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> DeletePaymentConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (tenantId == Guid.Empty)
                return ServiceResult<bool>.FailureResult("Tenant ID is required");

            var methods = await _unitOfWork.TenantPaymentMethods
                .FindAsync(m => m.TenantId == tenantId, cancellationToken);
            var method = methods.FirstOrDefault();

            if (method == null)
                return ServiceResult<bool>.FailureResult("No payment configuration found");

            _unitOfWork.TenantPaymentMethods.Delete(method);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted payment configuration for tenant {TenantId}", tenantId);
            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment configuration for tenant {TenantId}", tenantId);
            return ServiceResult<bool>.FailureResult($"Error deleting configuration: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> ValidateCredentialsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (tenantId == Guid.Empty)
                return ServiceResult<bool>.FailureResult("Tenant ID is required");

            var methods = await _unitOfWork.TenantPaymentMethods
                .FindAsync(m => m.TenantId == tenantId, cancellationToken);
            var method = methods.FirstOrDefault();

            if (method == null)
                return ServiceResult<bool>.FailureResult("No payment configuration found");

            // Decrypt credentials
            var decryptedKeyId = await _encryptionService.DecryptAsync(method.EncryptedApiKey, cancellationToken);
            var decryptedKeySecret = await _encryptionService.DecryptAsync(method.EncryptedApiSecret, cancellationToken);

            _logger.LogInformation("Validating payment credentials for tenant {TenantId}", tenantId);

            // Test connection by attempting to retrieve a test plan or account details
            // For now, we'll use a simple test: try to create a test order with minimal amount
            try
            {
                var testAmount = 1m; // 1 rupee
                var testReceipt = $"TEST-{tenantId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                
                await _razorpayService.CreateOrderAsync(
                    testAmount,
                    "INR",
                    testReceipt,
                    new Dictionary<string, string> { { "test", "true" } },
                    decryptedKeyId,
                    decryptedKeySecret,
                    cancellationToken);

                // Mark as tested
                method.MarkAsTested();
                _unitOfWork.TenantPaymentMethods.Update(method);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment credentials validated successfully for tenant {TenantId}", tenantId);
                return ServiceResult<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payment credentials validation failed for tenant {TenantId}", tenantId);
                return ServiceResult<bool>.FailureResult($"Credentials validation failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating payment credentials for tenant {TenantId}", tenantId);
            return ServiceResult<bool>.FailureResult($"Error validating credentials: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> HasPaymentConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (tenantId == Guid.Empty)
                return ServiceResult<bool>.FailureResult("Tenant ID is required");

            var methods = await _unitOfWork.TenantPaymentMethods
                .FindAsync(m => m.TenantId == tenantId, cancellationToken);
            var hasConfig = methods.Any();

            _logger.LogInformation("Checked payment configuration existence for tenant {TenantId}: {HasConfig}", tenantId, hasConfig);

            return ServiceResult<bool>.SuccessResult(hasConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment configuration for tenant {TenantId}", tenantId);
            return ServiceResult<bool>.FailureResult($"Error checking configuration: {ex.Message}");
        }
    }
}
