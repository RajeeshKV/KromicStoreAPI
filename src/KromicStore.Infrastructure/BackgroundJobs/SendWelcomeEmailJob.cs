using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KromicStore.Infrastructure.Proxies;
using Hangfire;

namespace KromicStore.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job for sending welcome email to newly registered tenants.
/// Executes asynchronously via Hangfire to avoid blocking registration flow.
/// Implements retry logic with exponential backoff for transient failures.
/// </summary>
public class SendWelcomeEmailJob
{
    private readonly NotificationProxy _notificationProxy;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendWelcomeEmailJob> _logger;

    /// <summary>
    /// Initializes SendWelcomeEmailJob with required dependencies.
    /// </summary>
    /// <param name="notificationProxy">Proxy for Brevo email service</param>
    /// <param name="configuration">Application configuration for template IDs and support email</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
    public SendWelcomeEmailJob(
        NotificationProxy notificationProxy,
        IConfiguration configuration,
        ILogger<SendWelcomeEmailJob> logger)
    {
        _notificationProxy = notificationProxy ?? throw new ArgumentNullException(nameof(notificationProxy));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends welcome email to newly registered tenant.
    /// Includes onboarding instructions, API documentation link, and support contact.
    /// </summary>
    /// <param name="tenantId">Unique identifier of the tenant</param>
    /// <param name="companyName">Tenant company name to include in email</param>
    /// <param name="tenantEmail">Email address of the tenant owner</param>
    /// <param name="tenantAdminName">Name of the tenant administrator</param>
    /// <param name="trialEndDate">Date when trial period ends</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <exception cref="ArgumentException">Thrown when required parameters are null/empty</exception>
    public async Task ExecuteAsync(
        Guid tenantId,
        string companyName,
        string tenantEmail,
        string tenantAdminName,
        DateTime trialEndDate,
        CancellationToken cancellationToken = default)
    {
        // Validate input parameters
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name cannot be empty", nameof(companyName));

        if (string.IsNullOrWhiteSpace(tenantEmail))
            throw new ArgumentException("Tenant email cannot be empty", nameof(tenantEmail));

        if (string.IsNullOrWhiteSpace(tenantAdminName))
            throw new ArgumentException("Tenant admin name cannot be empty", nameof(tenantAdminName));

        _logger.LogInformation(
            "Starting welcome email job for TenantId={TenantId}, Email={Email}, Company={Company}",
            tenantId,
            tenantEmail,
            companyName);

        try
        {
            // Get configuration values
            var templateId = _configuration["ExternalServices:Brevo:TemplateIds:WelcomeEmail"];
            var supportEmail = _configuration["Notifications:SupportEmail"];
            var dashboardUrl = _configuration["Application:DashboardUrl"] ?? "https://app.kromicstore.com";

            // Validate configuration
            if (string.IsNullOrWhiteSpace(templateId))
            {
                _logger.LogError("Welcome email template ID not configured");
                throw new InvalidOperationException("Welcome email template ID not configured in Brevo settings");
            }

            if (string.IsNullOrWhiteSpace(supportEmail))
            {
                _logger.LogWarning("Support email not configured, using default");
                supportEmail = "support@kromicstore.com";
            }

            // Construct dashboard URL with tenant subdomain
            var dashboardUrlForTenant = $"{dashboardUrl}/dashboard/{tenantId}";

            // Construct API docs link
            var apiDocsUrl = $"{dashboardUrl}/docs/api";

            // Prepare template parameters
            var templateParameters = new Dictionary<string, string>
            {
                { "CompanyName", companyName },
                { "AdminName", tenantAdminName },
                { "DashboardUrl", dashboardUrlForTenant },
                { "ApiDocsUrl", apiDocsUrl },
                { "TrialEndDate", trialEndDate.ToString("MMMM dd, yyyy") },
                { "SupportEmail", supportEmail },
                { "FirstStepsGuide", "1. Create product categories for your store\n2. Add your products with images and prices\n3. Configure payment settings\n4. Set up webhook integrations" }
            };

            // Prepare email request
            var emailRequest = new SendEmailRequest
            {
                To = tenantEmail,
                ToName = tenantAdminName,
                Subject = "Welcome to KromicStore - Get Started with Your Trial",
                TemplateId = int.Parse(templateId),
                TemplateParameters = templateParameters,
                Tag = "welcome",
                CustomHeaders = new Dictionary<string, string>
                {
                    { "X-Tenant-ID", tenantId.ToString() }
                }
            };

            // Send email via NotificationProxy with retry logic handled by proxy
            var result = await _notificationProxy.SendEmailAsync(emailRequest, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Welcome email sent successfully for TenantId={TenantId}, MessageId={MessageId}",
                    tenantId,
                    result.Data?.MessageId);
            }
            else
            {
                _logger.LogError(
                    "Welcome email delivery failed for TenantId={TenantId}, Error={Error}",
                    tenantId,
                    result.Exception?.Message ?? "Unknown error");

                // Throw exception to trigger Hangfire retry
                throw new InvalidOperationException(
                    $"Failed to send welcome email: {result.Exception?.Message ?? "Unknown error"}",
                    result.Exception);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Welcome email job failed for TenantId={TenantId}, Email={Email}",
                tenantId,
                tenantEmail);

            // Re-throw to allow Hangfire to retry
            throw;
        }
    }
}
