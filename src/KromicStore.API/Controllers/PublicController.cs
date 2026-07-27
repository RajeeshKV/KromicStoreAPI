using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KromicStore.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using KromicStore.Infrastructure.Proxies;

namespace KromicStore.API.Controllers;

/// <summary>
/// Public endpoints accessible without authentication.
/// </summary>
[ApiController]
[Route("api/v1/public")]
public class PublicController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PublicController> _logger;
    private readonly AppDbContext _context;
    private readonly NotificationProxy _notificationProxy;

    public PublicController(IConfiguration configuration, ILogger<PublicController> logger, AppDbContext context, NotificationProxy notificationProxy)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _notificationProxy = notificationProxy;
    }

    /// <summary>
    /// Get available subscription plans.
    /// </summary>
    /// <returns>List of available subscription plans with pricing and features.</returns>
    /// <response code="200">Subscription plans retrieved successfully.</response>
    /// <response code="500">Server error retrieving plans.</response>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public IActionResult GetSubscriptionPlans()
    {
        try
        {
            var plans = new[]
            {
                new
                {
                    Id = "starter",
                    Name = "Starter",
                    Price = _configuration.GetValue<decimal>("SUBSCRIPTION_PLAN_STARTER_PRICE"),
                    Currency = _configuration.GetValue<string>("SUBSCRIPTION_PLAN_CURRENCY") ?? "INR",
                    Features = new[]
                    {
                        "5 Users",
                        "100 Products",
                        "10,000 API Calls/month"
                    }
                },
                new
                {
                    Id = "professional",
                    Name = "Professional",
                    Price = _configuration.GetValue<decimal>("SUBSCRIPTION_PLAN_PROFESSIONAL_PRICE"),
                    Currency = _configuration.GetValue<string>("SUBSCRIPTION_PLAN_CURRENCY") ?? "INR",
                    Features = new[]
                    {
                        "50 Users",
                        "1,000 Products",
                        "100,000 API Calls/month"
                    }
                },
                new
                {
                    Id = "enterprise",
                    Name = "Enterprise",
                    Price = _configuration.GetValue<decimal>("SUBSCRIPTION_PLAN_ENTERPRISE_PRICE"),
                    Currency = _configuration.GetValue<string>("SUBSCRIPTION_PLAN_CURRENCY") ?? "INR",
                    Features = new[]
                    {
                        "Unlimited Users",
                        "Unlimited Products",
                        "Unlimited API Calls",
                        "Priority Support"
                    }
                }
            };

            return Ok(new { data = plans });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plans");
            return StatusCode(500, new { error = "Failed to retrieve subscription plans" });
        }
    }

    /// <summary>
    /// Get SuperUser configuration (contact details, etc).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Platform configuration including contact details and social media links.</returns>
    /// <response code="200">Configuration retrieved successfully.</response>
    /// <response code="500">Server error retrieving configuration.</response>
    [HttpGet("config")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSuperUserConfig(CancellationToken cancellationToken)
    {
        try
        {
            var configs = await _context.SuperUserConfigs
                .ToListAsync(cancellationToken);

            var configDict = configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);

            var response = new
            {
                data = new
                {
                    contactEmail = configDict.GetValueOrDefault("contact_email", "support@kromicstore.com"),
                    contactPhone = configDict.GetValueOrDefault("contact_phone", ""),
                    supportEmail = configDict.GetValueOrDefault("support_email", "support@kromicstore.com"),
                    companyName = configDict.GetValueOrDefault("company_name", "KromicStore"),
                    websiteUrl = configDict.GetValueOrDefault("website_url", "https://kromicstore.com"),
                    instagramUrl = configDict.GetValueOrDefault("instagram_url", "")
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SuperUser config");
            return StatusCode(500, new { error = "Failed to retrieve configuration" });
        }
    }

    /// <summary>
    /// Check if a subdomain is available for registration.
    /// </summary>
    /// <param name="subdomain">The subdomain to check availability for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Availability status with reason if unavailable.</returns>
    /// <response code="200">Subdomain availability check completed.</response>
    /// <response code="400">Subdomain parameter missing.</response>
    /// <response code="500">Server error checking availability.</response>
    [HttpGet("subdomain/check")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckSubdomainAvailability([FromQuery] string subdomain, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subdomain))
            {
                return BadRequest(new { error = "Subdomain is required" });
            }

            // Normalize to lowercase
            subdomain = subdomain.ToLowerInvariant();

            // Check against reserved subdomains
            var reservedSubdomains = _configuration.GetValue<string>("TENANT_RESERVED_SUBDOMAINS", "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToHashSet();

            if (reservedSubdomains.Contains(subdomain))
            {
                return Ok(new { available = false, reason = "Subdomain is reserved" });
            }

            // Check if subdomain already exists
            var exists = await _context.Tenants
                .AnyAsync(t => t.Subdomain.ToLower() == subdomain, cancellationToken);

            if (exists)
            {
                return Ok(new { available = false, reason = "Subdomain is already taken" });
            }

            // Validate subdomain format
            if (!System.Text.RegularExpressions.Regex.IsMatch(subdomain, @"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$"))
            {
                return Ok(new { available = false, reason = "Invalid subdomain format. Only alphanumeric characters and hyphens are allowed." });
            }

            return Ok(new { available = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subdomain availability: {Subdomain}", subdomain);
            return StatusCode(500, new { error = "Failed to check subdomain availability" });
        }
    }

    /// <summary>
    /// Submit contact us form - sends email to superuser.
    /// </summary>
    /// <param name="request">Contact form data including name, email, and message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message confirming form submission.</returns>
    /// <response code="200">Contact form submitted successfully.</response>
    /// <response code="400">Required fields missing (name, email, message).</response>
    /// <response code="500">Server error processing form or sending email.</response>
    [HttpPost("contactus")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ContactUs([FromBody] ContactUsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || 
                string.IsNullOrWhiteSpace(request.Email) || 
                string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Name, email, and message are required" });
            }

            var contactUsTemplateId = _configuration.GetValue<int>("BREVO_CONTACT_US_TEMPLATE_ID", 5);

            var emailRequest = new SendEmailRequest
            {
                To = "rajeeshkva2z@gmail.com",
                ToName = "SuperUser",
                Subject = $"Contact Us: {request.Subject ?? "New Inquiry from " + request.Name}",
                TemplateId = contactUsTemplateId,
                TemplateParameters = new Dictionary<string, string>
                {
                    { "name", request.Name },
                    { "email", request.Email },
                    { "phone", request.Phone ?? "Not provided" },
                    { "subject", request.Subject ?? "Contact Us Inquiry" },
                    { "message", request.Message },
                    { "submitted_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") }
                },
                Tag = "contactus"
            };

            var result = await _notificationProxy.SendEmailAsync(emailRequest, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to send contact us email: {Error}", result.Exception?.Message);
                return StatusCode(500, new { error = "Failed to send email" });
            }

            _logger.LogInformation("Contact us email sent successfully from {Email}", request.Email);
            return Ok(new { message = "Contact form submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing contact us form");
            return StatusCode(500, new { error = "Failed to process contact form" });
        }
    }
}

/// <summary>
/// Request model for contact us form.
/// </summary>
public class ContactUsRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
}
