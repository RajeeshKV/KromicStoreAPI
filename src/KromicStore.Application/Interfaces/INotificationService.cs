namespace KromicStore.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for centralized notification service supporting Email, SMS, WhatsApp, Push and Webhooks with templates, retries and dead-letter handling.
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

    /// <summary>
    /// Creates a notification record for later processing.
    /// </summary>
    Task<Domain.Entities.Notification> CreateNotificationAsync(
        Guid? tenantId,
        Guid? recipientId,
        string? recipientEmail,
        string? recipientPhone,
        string type,
        string templateKey,
        string? subject,
        string body,
        string? data = null,
        DateTime? scheduledAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes pending notifications.
    /// </summary>
    Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries failed notifications.
    /// </summary>
    Task RetryFailedNotificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a recipient.
    /// </summary>
    Task<IEnumerable<Domain.Entities.Notification>> GetNotificationsAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a tenant.
    /// </summary>
    Task<IEnumerable<Domain.Entities.Notification>> GetTenantNotificationsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
