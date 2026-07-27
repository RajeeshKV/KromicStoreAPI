// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of centralized notification service.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;
    private const int MaxRetries = 3;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending email to {To}", to);
            
            // In production, integrate with email service (SendGrid, SES, etc.)
            // For now, simulate sending
            await Task.Delay(100, cancellationToken);
            
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            throw;
        }
    }

    public async Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending SMS to {PhoneNumber}", phoneNumber);
            
            // In production, integrate with SMS service (Twilio, etc.)
            await Task.Delay(100, cancellationToken);
            
            _logger.LogInformation("SMS sent successfully to {PhoneNumber}", phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task SendPushNotificationAsync(string userId, string title, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending push notification to user {UserId}", userId);
            
            // In production, integrate with push notification service (Firebase, OneSignal, etc.)
            await Task.Delay(100, cancellationToken);
            
            _logger.LogInformation("Push notification sent successfully to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to user {UserId}", userId);
            throw;
        }
    }

    public async Task SendTemplatedEmailAsync(string to, string templateId, Dictionary<string, string> variables, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending templated email {TemplateId} to {To}", templateId, to);
            
            // In production, load template and replace variables
            var body = $"Template: {templateId}, Variables: {string.Join(", ", variables)}";
            
            await SendEmailAsync(to, $"Notification from Template {templateId}", body, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Templated email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending templated email to {To}", to);
            throw;
        }
    }

    public async Task<Notification> CreateNotificationAsync(
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
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            tenantId,
            recipientId,
            recipientEmail,
            recipientPhone,
            type,
            templateKey,
            subject,
            body,
            data,
            scheduledAt);

        await _context.Notifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification created: {Type} for recipient {RecipientId}",
            type, recipientId);

        return notification;
    }

    public async Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var pendingNotifications = await _context.Notifications
            .Where(n => n.Status == "Pending" && (n.ScheduledAt == null || n.ScheduledAt <= DateTime.UtcNow))
            .OrderBy(n => n.ScheduledAt)
            .Take(100) // Batch size
            .ToListAsync(cancellationToken);

        foreach (var notification in pendingNotifications)
        {
            try
            {
                await ProcessNotificationAsync(notification, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification {NotificationId}", notification.Id);
                notification.MarkAsFailedForRetry(ex.Message);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        if (pendingNotifications.Any())
        {
            _logger.LogInformation("Processed {Count} pending notifications", pendingNotifications.Count);
        }
    }

    public async Task RetryFailedNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var retryableNotifications = await _context.Notifications
            .Where(n => n.Status == "Retrying" && n.CanRetry(MaxRetries))
            .OrderBy(n => n.CreatedAt)
            .Take(50) // Batch size
            .ToListAsync(cancellationToken);

        foreach (var notification in retryableNotifications)
        {
            try
            {
                await ProcessNotificationAsync(notification, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying notification {NotificationId}", notification.Id);
                
                if (notification.RetryCount >= MaxRetries)
                {
                    notification.MarkAsDeadLetter(ex.Message);
                }
                else
                {
                    notification.MarkAsFailedForRetry(ex.Message);
                }
                
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        if (retryableNotifications.Any())
        {
            _logger.LogInformation("Retried {Count} failed notifications", retryableNotifications.Count);
        }
    }

    public async Task<IEnumerable<Notification>> GetNotificationsAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetTenantNotificationsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task ProcessNotificationAsync(Notification notification, CancellationToken cancellationToken)
    {
        switch (notification.Type.ToLowerInvariant())
        {
            case "email":
                if (!string.IsNullOrWhiteSpace(notification.RecipientEmail))
                {
                    await SendEmailAsync(
                        notification.RecipientEmail,
                        notification.Subject ?? "Notification",
                        notification.Body,
                        cancellationToken: cancellationToken);
                }
                break;

            case "sms":
                if (!string.IsNullOrWhiteSpace(notification.RecipientPhone))
                {
                    await SendSmsAsync(notification.RecipientPhone, notification.Body, cancellationToken);
                }
                break;

            case "push":
                if (notification.RecipientId.HasValue)
                {
                    await SendPushNotificationAsync(
                        notification.RecipientId.Value.ToString(),
                        notification.Subject ?? "Notification",
                        notification.Body,
                        cancellationToken);
                }
                break;

            case "webhook":
                // In production, send webhook notification
                await Task.CompletedTask;
                break;

            default:
                throw new ArgumentException($"Unsupported notification type: {notification.Type}");
        }

        notification.MarkAsSent();
    }
}
