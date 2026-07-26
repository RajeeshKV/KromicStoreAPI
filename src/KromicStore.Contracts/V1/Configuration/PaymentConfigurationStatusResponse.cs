namespace KromicStore.Contracts.V1.Configuration;

/// <summary>
/// Response indicating payment configuration status.
/// </summary>
public record PaymentConfigurationStatusResponse(
    bool IsConfigured);
