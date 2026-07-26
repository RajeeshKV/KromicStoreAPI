namespace KromicStore.API.Configuration;

/// <summary>
/// Static strongly-typed configuration class for accessing all configuration settings.
/// This provides a centralized, compile-time safe way to access configuration values.
/// </summary>
public static class ConfigurationSettings
{
    /// <summary>
    /// API Settings configuration
    /// </summary>
    public static class Api
    {
        public static string BaseUrl => GetConfigValue("API_BASE_URL", "https://api.example.com");
        public static string FrontendBaseUrl => GetConfigValue("FRONTEND_BASE_URL", "https://app.example.com");
        public static bool SwaggerEnabled => GetBoolConfigValue("SWAGGER_ENABLED", true);
        public static string CorsAllowedOrigins => GetConfigValue("CORS_ALLOWED_ORIGINS", "https://app.example.com");
    }

    /// <summary>
    /// Authentication and JWT settings
    /// </summary>
    public static class Authentication
    {
        public static string JwtSecret => GetConfigValue("JWT_SECRET", "");
        public static string JwtAuthority => GetConfigValue("JWT_AUTHORITY", "https://auth.example.com");
        public static string JwtAudience => GetConfigValue("JWT_AUDIENCE", "kromic-store-api");
        public static int JwtExpirationMinutes => GetIntConfigValue("JWT_EXPIRATION_MINUTES", 60);
        public static int RefreshTokenExpirationDays => GetIntConfigValue("REFRESH_TOKEN_EXPIRATION_DAYS", 7);
        public static int PasswordMinLength => GetIntConfigValue("PASSWORD_MIN_LENGTH", 8);
        public static bool PasswordRequireUppercase => GetBoolConfigValue("PASSWORD_REQUIRE_UPPERCASE", true);
        public static bool PasswordRequireNumbers => GetBoolConfigValue("PASSWORD_REQUIRE_NUMBERS", true);
        public static bool PasswordRequireSpecial => GetBoolConfigValue("PASSWORD_REQUIRE_SPECIAL", true);
    }

    /// <summary>
    /// Database connection settings
    /// </summary>
    public static class Database
    {
        public static string ConnectionUrl => GetConfigValue("DATABASE_URL", "postgresql://localhost");
        public static int ConnectionPoolMin => GetIntConfigValue("DB_CONNECTION_POOL_MIN", 5);
        public static int ConnectionPoolMax => GetIntConfigValue("DB_CONNECTION_POOL_MAX", 25);
        public static int ConnectionTimeoutSeconds => GetIntConfigValue("DB_CONNECTION_TIMEOUT_SECONDS", 30);
        public static int IdleTimeoutSeconds => GetIntConfigValue("DB_IDLE_TIMEOUT_SECONDS", 300);
        public static int MaxAgeSeconds => GetIntConfigValue("DB_MAX_AGE_SECONDS", 1800);
    }

    /// <summary>
    /// Redis caching settings
    /// </summary>
    public static class Cache
    {
        public static string RedisUrl => GetConfigValue("REDIS_URL", "localhost:6379");
        public static string RedisPassword => GetConfigValue("REDIS_PASSWORD", "");
        public static int RedisDb => GetIntConfigValue("REDIS_DB", 0);
        public static int RedisTimeoutMs => GetIntConfigValue("REDIS_TIMEOUT_MS", 5000);
        public static int ProductsCacheTtlMinutes => GetIntConfigValue("CACHE_TTL_PRODUCTS_MINUTES", 60);
        public static int OrdersCacheTtlMinutes => GetIntConfigValue("CACHE_TTL_ORDERS_MINUTES", 5);
        public static int ConfigCacheTtlMinutes => GetIntConfigValue("CACHE_TTL_CONFIG_MINUTES", 30);
    }

    /// <summary>
    /// Logging configuration
    /// </summary>
    public static class Logging
    {
        public static string LogLevel => GetConfigValue("LOG_LEVEL", "Information");
        public static string LogOutputFormat => GetConfigValue("LOG_OUTPUT_FORMAT", "json");
        public static string LogFilePath => GetConfigValue("LOG_FILE_PATH", "/var/log/app.log");
        public static int LogFileSizeMb => GetIntConfigValue("LOG_FILE_SIZE_MB", 100);
        public static int LogFilesToKeep => GetIntConfigValue("LOG_FILES_TO_KEEP", 10);
        public static bool CorrelationIdEnabled => GetBoolConfigValue("CORRELATION_ID_ENABLED", true);
    }

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public static class RateLimiting
    {
        public static bool Enabled => GetBoolConfigValue("RATE_LIMIT_ENABLED", true);
        public static int RequestsPerMinute => GetIntConfigValue("RATE_LIMIT_REQUESTS_PER_MINUTE", 100);
        public static string ByPlanJson => GetConfigValue("RATE_LIMIT_BY_PLAN", @"{""starter"":100,""professional"":500,""enterprise"":5000}");
        public static string CacheKeyPrefix => GetConfigValue("RATE_LIMIT_CACHE_KEY_PREFIX", "rate_limit");
    }

    /// <summary>
    /// Razorpay payment gateway configuration
    /// </summary>
    public static class Razorpay
    {
        public static string KeyId => GetConfigValue("RAZORPAY_KEY_ID", "");
        public static string KeySecret => GetConfigValue("RAZORPAY_KEY_SECRET", "");
        public static string WebhookSecret => GetConfigValue("RAZORPAY_WEBHOOK_SECRET", "");
        public static int TimeoutSeconds => GetIntConfigValue("RAZORPAY_TIMEOUT_SECONDS", 30);
        public static bool RetryEnabled => GetBoolConfigValue("RAZORPAY_RETRY_ENABLED", true);
        public static int CircuitBreakerThreshold => GetIntConfigValue("RAZORPAY_CIRCUIT_BREAKER_THRESHOLD", 5);
    }

    /// <summary>
    /// Google OAuth configuration
    /// </summary>
    public static class GoogleOAuth
    {
        public static string ClientId => GetConfigValue("GOOGLE_CLIENT_ID", "");
        public static string ClientSecret => GetConfigValue("GOOGLE_CLIENT_SECRET", "");
        public static string RedirectUri => GetConfigValue("GOOGLE_REDIRECT_URI", "https://api.example.com/api/v1/auth/oauth/google/callback");
        public static string TokenEndpoint => GetConfigValue("GOOGLE_TOKEN_ENDPOINT", "https://oauth2.googleapis.com/token");
        public static string UserInfoEndpoint => GetConfigValue("GOOGLE_USER_INFO_ENDPOINT", "https://www.googleapis.com/oauth2/v2/userinfo");
    }

    /// <summary>
    /// Cloudinary media service configuration
    /// </summary>
    public static class Cloudinary
    {
        public static string CloudName => GetConfigValue("CLOUDINARY_CLOUD_NAME", "");
        public static string ApiKey => GetConfigValue("CLOUDINARY_API_KEY", "");
        public static string ApiSecret => GetConfigValue("CLOUDINARY_API_SECRET", "");
        public static string BaseUrl => GetConfigValue("CLOUDINARY_BASE_URL", "https://api.cloudinary.com");
        public static string FolderPath => GetConfigValue("CLOUDINARY_FOLDER_PATH", "kromic-store");
        public static string Quality => GetConfigValue("CLOUDINARY_QUALITY", "auto");
        public static int MaxFileSizeMb => GetIntConfigValue("CLOUDINARY_MAX_FILE_SIZE_MB", 100);
    }

    /// <summary>
    /// Brevo email notification service configuration
    /// </summary>
    public static class Brevo
    {
        public static string ApiKey => GetConfigValue("BREVO_API_KEY", "");
        public static string SenderEmail => GetConfigValue("BREVO_SENDER_EMAIL", "noreply@example.com");
        public static string SenderName => GetConfigValue("BREVO_SENDER_NAME", "KromicStore");
        public static string BaseUrl => GetConfigValue("BREVO_BASE_URL", "https://api.brevo.com");
        public static string ApiVersion => GetConfigValue("BREVO_API_VERSION", "v3");
        public static int WelcomeEmailTemplateId => GetIntConfigValue("BREVO_WELCOME_EMAIL_TEMPLATE_ID", 1);
        public static int OrderConfirmationTemplateId => GetIntConfigValue("BREVO_ORDER_CONFIRMATION_TEMPLATE_ID", 2);
        public static int ShipmentNotificationTemplateId => GetIntConfigValue("BREVO_SHIPMENT_NOTIFICATION_TEMPLATE_ID", 3);
        public static int PaymentFailureTemplateId => GetIntConfigValue("BREVO_PAYMENT_FAILURE_TEMPLATE_ID", 4);
    }

    /// <summary>
    /// Hangfire background job configuration
    /// </summary>
    public static class Hangfire
    {
        public static bool Enabled => GetBoolConfigValue("HANGFIRE_ENABLED", true);
        public static int WorkerCount => GetIntConfigValue("HANGFIRE_WORKER_COUNT", Environment.ProcessorCount);
        public static string Queues => GetConfigValue("HANGFIRE_QUEUES", "default,webhooks,scheduled");
        public static int SuccessJobExpiryMinutes => GetIntConfigValue("HANGFIRE_SUCCESS_JOB_EXPIRY_MINUTES", 60);
        public static int FailedJobExpiryDays => GetIntConfigValue("HANGFIRE_FAILED_JOB_EXPIRY_DAYS", 7);
    }

    /// <summary>
    /// Application-level settings
    /// </summary>
    public static class Application
    {
        public static string EnvironmentName => GetConfigValue("ASPNETCORE_ENVIRONMENT", "Production");
        public static string Urls => GetConfigValue("ASPNETCORE_URLS", "http://+:8080");
        public static string InstanceId => GetConfigValue("APPLICATION_INSTANCE_ID", Guid.NewGuid().ToString());
        public static int MaxUploadSizeMb => GetIntConfigValue("MAX_UPLOAD_SIZE_MB", 100);
        public static int SessionTimeoutMinutes => GetIntConfigValue("SESSION_TIMEOUT_MINUTES", 30);
        public static int PaginationDefaultPageSize => GetIntConfigValue("PAGINATION_DEFAULT_PAGE_SIZE", 20);
        public static int PaginationMaxPageSize => GetIntConfigValue("PAGINATION_MAX_PAGE_SIZE", 100);
    }

    /// <summary>
    /// Tenant-level configuration
    /// </summary>
    public static class Tenant
    {
        public static int TrialDurationDays => GetIntConfigValue("TENANT_TRIAL_DURATION_DAYS", 14);
        public static int MaxUsersStarter => GetIntConfigValue("TENANT_MAX_USERS_STARTER", 5);
        public static int MaxUsersProfessional => GetIntConfigValue("TENANT_MAX_USERS_PROFESSIONAL", 50);
        public static int MaxProductsStarter => GetIntConfigValue("TENANT_MAX_PRODUCTS_STARTER", 100);
        public static int MaxProductsProfessional => GetIntConfigValue("TENANT_MAX_PRODUCTS_PROFESSIONAL", 1000);
        public static int MaxApiCallsStarter => GetIntConfigValue("TENANT_MAX_API_CALLS_STARTER", 10000);
        public static int MaxApiCallsProfessional => GetIntConfigValue("TENANT_MAX_API_CALLS_PROFESSIONAL", 100000);
    }

    /// <summary>
    /// Subscription plan pricing
    /// </summary>
    public static class Subscriptions
    {
        public static decimal PlanStarterPrice => GetDecimalConfigValue("SUBSCRIPTION_PLAN_STARTER_PRICE", 9.99m);
        public static decimal PlanProfessionalPrice => GetDecimalConfigValue("SUBSCRIPTION_PLAN_PROFESSIONAL_PRICE", 29.99m);
        public static decimal PlanEnterprisePrice => GetDecimalConfigValue("SUBSCRIPTION_PLAN_ENTERPRISE_PRICE", 99.99m);
        public static string Currency => GetConfigValue("SUBSCRIPTION_PLAN_CURRENCY", "USD");
    }

    /// <summary>
    /// External service default settings
    /// </summary>
    public static class ExternalServices
    {
        public static int TimeoutSeconds => GetIntConfigValue("EXTERNAL_SERVICE_TIMEOUT_SECONDS", 30);
        public static int MaxRetries => GetIntConfigValue("EXTERNAL_SERVICE_MAX_RETRIES", 4);
        public static string RetryDelaysMs => GetConfigValue("EXTERNAL_SERVICE_RETRY_DELAYS_MS", "[100,1000,10000,30000]");
        public static int CircuitBreakerFailureThreshold => GetIntConfigValue("CIRCUIT_BREAKER_FAILURE_THRESHOLD", 5);
        public static int CircuitBreakerTimeoutSeconds => GetIntConfigValue("CIRCUIT_BREAKER_TIMEOUT_SECONDS", 30);
    }

    /// <summary>
    /// Monitoring and Application Insights
    /// </summary>
    public static class Monitoring
    {
        public static bool Enabled => GetBoolConfigValue("MONITORING_ENABLED", false);
        public static string InstrumentationKey => GetConfigValue("MONITORING_INSTRUMENTATION_KEY", "");
        public static string LogLevel => GetConfigValue("MONITORING_LOG_LEVEL", "Information");
        public static int ResponseTimeThresholdMs => GetIntConfigValue("MONITORING_RESPONSE_TIME_THRESHOLD_MS", 500);
        public static int DbQueryThresholdMs => GetIntConfigValue("MONITORING_DB_QUERY_THRESHOLD_MS", 100);
    }

    /// <summary>
    /// Security settings
    /// </summary>
    public static class Security
    {
        public static bool RequireHttps => GetBoolConfigValue("SECURITY_REQUIRE_HTTPS", true);
        public static string EncryptionKey => GetConfigValue("SECURITY_ENCRYPTION_KEY", "");
        public static bool CorsAllowCredentials => GetBoolConfigValue("CORS_ALLOW_CREDENTIALS", true);
        public static bool CorsAllowAnyOrigin => GetBoolConfigValue("CORS_ALLOW_ANY_ORIGIN", false);
    }

    // Helper methods for configuration retrieval
    private static string GetConfigValue(string key, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    private static int GetIntConfigValue(string key, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static bool GetBoolConfigValue(string key, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key)?.ToLower();
        return value switch
        {
            "true" or "1" or "yes" or "on" => true,
            "false" or "0" or "no" or "off" => false,
            _ => defaultValue
        };
    }

    private static decimal GetDecimalConfigValue(string key, decimal defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }
}
