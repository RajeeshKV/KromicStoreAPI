namespace KromicStore.Contracts.V1.Configuration;

/// <summary>
/// Response from payment configuration validation.
/// </summary>
public record PaymentConfigurationValidationResponse(
    bool Success,
    string Message);
