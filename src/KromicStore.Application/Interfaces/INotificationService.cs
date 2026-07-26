namespace KromicStore.Application.Interfaces;

/// <summary>
/// Interface for sending notifications (email, SMS, etc.).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends an email notification.
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an SMS notification.
    /// </summary>
    Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a push notification.
    /// </summary>
    Task SendPushNotificationAsync(string userId, string title, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a templated email.
    /// </summary>
    Task SendTemplatedEmailAsync(string to, string templateId, Dictionary<string, string> variables, CancellationToken cancellationToken = default);
}
