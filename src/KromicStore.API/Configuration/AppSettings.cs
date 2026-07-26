namespace KromicStore.API.Configuration;

/// <summary>
/// Strongly-typed settings model for dependency injection via IOptions pattern.
/// Populated from appsettings.json and environment variables.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// API-level settings
    /// </summary>
    public ApiSettings Api { get; set; } = new();

    /// <summary>
    /// Authentication settings
    /// </summary>
    public AuthSettings Auth { get; set; } = new();

    /// <summary>
    /// Database settings
    /// </summary>
    public DatabaseSettings Database { get; set; } = new();

    /// <summary>
    /// Cache/Redis settings
    /// </summary>
    public CacheSettings Cache { get; set; } = new();

    /// <summary>
    /// Logging settings
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Rate limiting settings
    /// </summary>
    public RateLimitingSettings RateLimiting { get; set; } = new();

    /// <summary>
    /// Razorpay payment settings
    /// </summary>
    public RazorpaySettings Razorpay { get; set; } = new();

    /// <summary>
    /// Google OAuth settings
    /// </summary>
    public GoogleOAuthSettings GoogleOAuth { get; set; } = new();

    /// <summary>
    /// Cloudinary media settings
    /// </summary>
    public CloudinarySettings Cloudinary { get; set; } = new();

    /// <summary>
    /// Brevo email notification settings
    /// </summary>
    public BrevoSettings Brevo { get; set; } = new();

    /// <summary>
    /// Hangfire background job settings
    /// </summary>
    public HangfireSettings Hangfire { get; set; } = new();

    /// <summary>
    /// Application-level settings
    /// </summary>
    public ApplicationSettings Application { get; set; } = new();

    /// <summary>
    /// Tenant settings
    /// </summary>
    public TenantSettings Tenant { get; set; } = new();

    /// <summary>
    /// Subscription plan settings
    /// </summary>
    public SubscriptionSettings Subscriptions { get; set; } = new();

    /// <summary>
    /// External service settings
    /// </summary>
    public ExternalServiceSettings ExternalServices { get; set; } = new();

    /// <summary>
    /// Monitoring settings
    /// </summary>
    public MonitoringSettings Monitoring { get; set; } = new();

    /// <summary>
    /// Security settings
    /// </summary>
    public SecuritySettings Security { get; set; } = new();
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = "https://api.example.com";
    public string FrontendBaseUrl { get; set; } = "https://app.example.com";
    public bool SwaggerEnabled { get; set; } = true;
    public string CorsAllowedOrigins { get; set; } = "https://app.example.com";
}

public class AuthSettings
{
    public string JwtSecret { get; set; } = string.Empty;
    public string JwtAuthority { get; set; } = "https://auth.example.com";
    public string JwtAudience { get; set; } = "kromic-store-api";
    public int JwtExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public int PasswordMinLength { get; set; } = 8;
    public bool PasswordRequireUppercase { get; set; } = true;
    public bool PasswordRequireNumbers { get; set; } = true;
    public bool PasswordRequireSpecial { get; set; } = true;
}

public class DatabaseSettings
{
    public string ConnectionUrl { get; set; } = "postgresql://localhost";
    public int ConnectionPoolMin { get; set; } = 5;
    public int ConnectionPoolMax { get; set; } = 25;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int IdleTimeoutSeconds { get; set; } = 300;
    public int MaxAgeSeconds { get; set; } = 1800;
}

public class CacheSettings
{
    public string RedisUrl { get; set; } = "localhost:6379";
    public string RedisPassword { get; set; } = string.Empty;
    public int RedisDb { get; set; } = 0;
    public int RedisTimeoutMs { get; set; } = 5000;
    public int ProductsCacheTtlMinutes { get; set; } = 60;
    public int OrdersCacheTtlMinutes { get; set; } = 5;
    public int ConfigCacheTtlMinutes { get; set; } = 30;
}

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    public string LogOutputFormat { get; set; } = "json";
    public string LogFilePath { get; set; } = "/var/log/app.log";
    public int LogFileSizeMb { get; set; } = 100;
    public int LogFilesToKeep { get; set; } = 10;
    public bool CorrelationIdEnabled { get; set; } = true;
}

public class RateLimitingSettings
{
    public bool Enabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 100;
    public string ByPlanJson { get; set; } = @"{""starter"":100,""professional"":500,""enterprise"":5000}";
    public string CacheKeyPrefix { get; set; } = "rate_limit";
}

public class RazorpaySettings
{
    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool RetryEnabled { get; set; } = true;
    public int CircuitBreakerThreshold { get; set; } = 5;
}

public class GoogleOAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "https://api.example.com/api/v1/auth/oauth/google/callback";
    public string TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";
    public string UserInfoEndpoint { get; set; } = "https://www.googleapis.com/oauth2/v2/userinfo";
}

public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.cloudinary.com";
    public string FolderPath { get; set; } = "kromic-store";
    public string Quality { get; set; } = "auto";
    public int MaxFileSizeMb { get; set; } = 100;
}

public class BrevoSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = "noreply@example.com";
    public string SenderName { get; set; } = "KromicStore";
    public string BaseUrl { get; set; } = "https://api.brevo.com";
    public string ApiVersion { get; set; } = "v3";
    public int WelcomeEmailTemplateId { get; set; } = 1;
    public int OrderConfirmationTemplateId { get; set; } = 2;
    public int ShipmentNotificationTemplateId { get; set; } = 3;
    public int PaymentFailureTemplateId { get; set; } = 4;
}

public class HangfireSettings
{
    public bool Enabled { get; set; } = true;
    public int WorkerCount { get; set; } = Environment.ProcessorCount;
    public string[] Queues { get; set; } = ["default", "webhooks", "scheduled"];
    public int SuccessJobExpiryMinutes { get; set; } = 60;
    public int FailedJobExpiryDays { get; set; } = 7;
}

public class ApplicationSettings
{
    public string EnvironmentName { get; set; } = "Production";
    public string Urls { get; set; } = "http://+:8080";
    public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    public int MaxUploadSizeMb { get; set; } = 100;
    public int SessionTimeoutMinutes { get; set; } = 30;
    public int PaginationDefaultPageSize { get; set; } = 20;
    public int PaginationMaxPageSize { get; set; } = 100;
}

public class TenantSettings
{
    public int TrialDurationDays { get; set; } = 14;
    public int MaxUsersStarter { get; set; } = 5;
    public int MaxUsersProfessional { get; set; } = 50;
    public int MaxProductsStarter { get; set; } = 100;
    public int MaxProductsProfessional { get; set; } = 1000;
    public int MaxApiCallsStarter { get; set; } = 10000;
    public int MaxApiCallsProfessional { get; set; } = 100000;
}

public class SubscriptionSettings
{
    public decimal PlanStarterPrice { get; set; } = 9.99m;
    public decimal PlanProfessionalPrice { get; set; } = 29.99m;
    public decimal PlanEnterprisePrice { get; set; } = 99.99m;
    public string Currency { get; set; } = "USD";
}

public class ExternalServiceSettings
{
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 4;
    public string RetryDelaysMs { get; set; } = "[100,1000,10000,30000]";
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerTimeoutSeconds { get; set; } = 30;
}

public class MonitoringSettings
{
    public bool Enabled { get; set; } = false;
    public string InstrumentationKey { get; set; } = string.Empty;
    public string LogLevel { get; set; } = "Information";
    public int ResponseTimeThresholdMs { get; set; } = 500;
    public int DbQueryThresholdMs { get; set; } = 100;
}

public class SecuritySettings
{
    public bool RequireHttps { get; set; } = true;
    public string EncryptionKey { get; set; } = string.Empty;
    public bool CorsAllowCredentials { get; set; } = true;
    public bool CorsAllowAnyOrigin { get; set; } = false;
}
