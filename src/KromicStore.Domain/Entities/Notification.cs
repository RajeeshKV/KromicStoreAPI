// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using System;

/// <summary>
/// Represents a notification to be sent via various channels.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>
    /// Gets or sets the tenant ID (null for platform-wide notifications).
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the recipient user ID (null if not user-specific).
    /// </summary>
    public Guid? RecipientId { get; set; }

    /// <summary>
    /// Gets or sets the recipient email address.
    /// </summary>
    public string? RecipientEmail { get; set; }

    /// <summary>
    /// Gets or sets the recipient phone number.
    /// </summary>
    public string? RecipientPhone { get; set; }

    /// <summary>
    /// Gets or sets the notification type (Email, SMS, WhatsApp, Push, Webhook).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification template key.
    /// </summary>
    public string TemplateKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the notification body/content.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification data (JSON serialized for template variables).
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the notification status (Pending, Sent, Failed, Retrying, DeadLetter).
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the date when the notification was sent.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Gets or sets the error message if sending failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the scheduled send date (for delayed sending).
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Factory method to create a new notification.
    /// </summary>
    public static Notification Create(
        Guid? tenantId,
        Guid? recipientId,
        string? recipientEmail,
        string? recipientPhone,
        string type,
        string templateKey,
        string? subject,
        string body,
        string? data = null,
        DateTime? scheduledAt = null)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Notification type is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Notification body is required.", nameof(body));

        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientId = recipientId,
            RecipientEmail = recipientEmail,
            RecipientPhone = recipientPhone,
            Type = type,
            TemplateKey = templateKey,
            Subject = subject,
            Body = body,
            Data = data,
            Status = "Pending",
            RetryCount = 0,
            ScheduledAt = scheduledAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marks the notification as sent.
    /// </summary>
    public void MarkAsSent()
    {
        Status = "Sent";
        SentAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the notification as failed with retry.
    /// </summary>
    public void MarkAsFailedForRetry(string errorMessage)
    {
        Status = "Retrying";
        RetryCount++;
        ErrorMessage = errorMessage;
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the notification as permanently failed (dead letter).
    /// </summary>
    public void MarkAsDeadLetter(string errorMessage)
    {
        Status = "DeadLetter";
        ErrorMessage = errorMessage;
        UpdateTimestamp();
    }

    /// <summary>
    /// Checks if the notification can be retried.
    /// </summary>
    public bool CanRetry(int maxRetries = 3)
    {
        return Status == "Retrying" && RetryCount < maxRetries;
    }
}
