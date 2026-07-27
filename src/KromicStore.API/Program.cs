using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;
using KromicStore.Infrastructure.Data;
using KromicStore.Infrastructure.Services;
using KromicStore.Infrastructure.Services.StorefrontServices;
using KromicStore.Infrastructure.Configuration;
using KromicStore.Infrastructure.Proxies;
using KromicStore.Infrastructure.BackgroundJobs;
using KromicStore.Infrastructure.Services.Webhooks;
using KromicStore.API.Configuration;
using KromicStore.API.Extensions;
using KromicStore.API.HealthChecks;
using KromicStore.Application.Interfaces;
using FluentValidation;
using KromicStore.Application.Validators;
using MediatR;
using KromicStore.API.Middleware;
using KromicStore.API.Authorization;
using KromicStore.API.Filters;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IO.Compression;
using Microsoft.AspNetCore.HttpOverrides;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using KromicStore.API;

var builder = WebApplication.CreateBuilder(args);

// Add HttpContextAccessor for Swagger document filter
builder.Services.AddHttpContextAccessor();

// Load environment variables
builder.Configuration.AddEnvironmentVariables();

// Disable file watching to avoid inotify issues on Render
builder.Host.ConfigureAppConfiguration((context, config) =>
{
    config.Sources.Clear();
    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
          .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
          .AddEnvironmentVariables();
});

// Validate required environment variables
var requiredEnvVars = new[] 
{ 
    "DATABASE_URL", 
    "JWT_SECRET", 
    "SECURITY_ENCRYPTION_KEY",
    "RAZORPAY_KEY_ID",
    "GOOGLE_CLIENT_ID",
    "CLOUDINARY_API_KEY",
    "BREVO_API_KEY"
};

var missingVars = new List<string>();
foreach (var envVar in requiredEnvVars)
{
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(envVar)))
    {
        missingVars.Add(envVar);
    }
}

if (missingVars.Count > 0)
{
    throw new InvalidOperationException(
        $"Missing required environment variables: {string.Join(", ", missingVars)}. " +
        "Please ensure all required variables are set. See docs/Environment-Setup.md for details.");
}

// Validate encryption key length
var encryptionKey = Environment.GetEnvironmentVariable("SECURITY_ENCRYPTION_KEY");
if (encryptionKey?.Length < 32)
{
    throw new InvalidOperationException(
        "SECURITY_ENCRYPTION_KEY must be at least 32 characters long for security. " +
        "Generate a secure key using: openssl rand -base64 32");
}

// Validate JWT secret length
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (jwtSecret?.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET must be at least 32 characters long for security. " +
        "Generate a secure key using: openssl rand -base64 32");
}

// Configure Serilog with structured logging
var environment = builder.Environment.EnvironmentName;
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "KromicStore.API")
    .Enrich.WithProperty("Environment", environment)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("KromicStore API starting up in {Environment} environment", environment);
}
catch { /* Logging not ready yet */ }

builder.Host.UseSerilog();

// Data - Build connection string from DATABASE_URL environment variable
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(databaseUrl))
{
    throw new InvalidOperationException("DATABASE_URL environment variable is required");
}

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(databaseUrl);
    if (builder.Environment.IsDevelopment())
    {
        opt.LogTo(Console.WriteLine)
            .EnableSensitiveDataLogging();
    }
});

builder.Services.AddMemoryCache();

// Cache
var redisConn = Environment.GetEnvironmentVariable("REDIS_URL");
var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
IConnectionMultiplexer? redis = null;

Log.Information("REDIS_URL from environment: {RedisUrl}", redisConn ?? "NULL");

if (!string.IsNullOrWhiteSpace(redisConn))
{
    try
    {
        // Log the connection string before parsing
        Log.Information("Attempting to connect to Redis with URL: {RedisUrl}", redisConn);
        
        var redisOpts = ConfigurationOptions.Parse(redisConn);
        
        // Fix: Remove database number from endpoint if it's being added as port
        // ConfigurationOptions.Parse("redis://host:port") sometimes adds :0 as port
        foreach (var endpoint in redisOpts.EndPoints)
        {
            var endpointStr = endpoint.ToString();
            Log.Information("Parsed endpoint before fix: {Endpoint}", endpointStr);
            
            // If endpoint contains :6379:6379 or :6379:0, fix it
            if (endpointStr.Contains(":6379:"))
            {
                redisOpts.EndPoints.Clear();
                // Extract just host:port
                var uri = new Uri(redisConn);
                redisOpts.EndPoints.Add(uri.Host, uri.Port);
                Log.Information("Fixed endpoint to: {Host}:{Port}", uri.Host, uri.Port);
                break;
            }
        }
        
        // Log parsed endpoints
        Log.Information("Parsed Redis endpoints: {Endpoints}", string.Join(", ", redisOpts.EndPoints.Select(e => e.ToString())));
        
        // Configure for Render's internal Redis
        redisOpts.AbortOnConnectFail = false;
        redisOpts.ConnectRetry = 5;
        redisOpts.ConnectTimeout = 10000;
        redisOpts.SyncTimeout = 5000;
        redisOpts.AsyncTimeout = 5000;
        
        // Add password if provided
        if (!string.IsNullOrWhiteSpace(redisPassword))
        {
            redisOpts.Password = redisPassword;
        }
        
        // For Render internal Redis, disable SSL if it's an internal URL
        if (redisConn.Contains("red-") && !redisConn.Contains("rediss://"))
        {
            redisOpts.Ssl = false;
        }
        
        Log.Information("Final Redis configuration - EndPoints: {Endpoints}, SSL: {Ssl}, AbortOnConnectFail: {Abort}", 
            string.Join(", ", redisOpts.EndPoints.Select(e => e.ToString())), 
            redisOpts.Ssl, 
            redisOpts.AbortOnConnectFail);
        
        redis = ConnectionMultiplexer.Connect(redisOpts);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        builder.Services.AddSingleton<ICacheService, CacheService>();
        Log.Information("Redis cache connected successfully");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to connect to Redis. Cache functionality will be disabled.");
        // Register a null cache service that gracefully handles cache misses
        builder.Services.AddSingleton<ICacheService>(new NullCacheService());
    }
}
else
{
    Log.Warning("REDIS_URL not configured. Cache functionality will be disabled.");
    builder.Services.AddSingleton<ICacheService>(new NullCacheService());
}

// UoW
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Crypto
var encKey = Environment.GetEnvironmentVariable("SECURITY_ENCRYPTION_KEY") ?? throw new InvalidOperationException("Missing SECURITY_ENCRYPTION_KEY");
builder.Services.AddScoped<IEncryptionService>(sp => new EncryptionService(encKey));

// Services
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISuperUserAuthService, SuperUserAuthService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ISubscriptionPaymentService, SubscriptionPaymentService>();
builder.Services.AddScoped<ITenantPaymentConfigurationService, TenantPaymentConfigurationService>();
builder.Services.AddScoped<TenantConfigurationSeeder>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<SendWelcomeEmailJob>();

// Razorpay Services
builder.Services.AddScoped<IRazorpayService, RazorpayService>();

// Order Payment Service
builder.Services.AddScoped<IOrderPaymentService, OrderPaymentService>();

// Razorpay Webhook Handlers
builder.Services.AddScoped<RazorpaySubscriptionWebhookHandler>();
builder.Services.AddScoped<RazorpayProductPaymentWebhookHandler>();

// Storefront Services
builder.Services.AddScoped<ThemeCloneService>();
builder.Services.AddScoped<DefaultDataPopulator>();
builder.Services.AddScoped<IStorefrontCreationService, StorefrontCreationService>();
builder.Services.AddScoped<IStoreBootstrapService, StoreBootstrapService>();

// Audit Logging
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Domain Verification
builder.Services.AddScoped<IDomainVerificationService, DomainVerificationService>();

// Team Invitations
builder.Services.AddScoped<ITeamInvitationService, TeamInvitationService>();

// Feature Flags
builder.Services.AddScoped<IFeatureFlagService, FeatureFlagService>();

// Notification Service
builder.Services.AddScoped<INotificationService, NotificationService>();

// API Key Management
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

// Store Export/Backup
builder.Services.AddScoped<IStoreExportService, StoreExportService>();

// Usage Reporting
builder.Services.AddScoped<IUsageReportingService, UsageReportingService>();

// SuperUser Analytics
builder.Services.AddScoped<ISuperUserAnalyticsService, SuperUserAnalyticsService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// HTTP clients
builder.Services.AddExternalServiceHttpClients(builder.Configuration);

// Proxies
builder.Services.AddScoped<PaymentProxy>(sp => new PaymentProxy(
    sp.GetRequiredService<ILogger<PaymentProxy>>(),
    new CircuitBreaker(),
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(PaymentProxy)),
    sp.GetRequiredService<IConfiguration>()));

builder.Services.AddScoped<OAuthProxy>(sp => new OAuthProxy(
    sp.GetRequiredService<ILogger<OAuthProxy>>(),
    new CircuitBreaker(),
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OAuthProxy)),
    sp.GetRequiredService<IConfiguration>()));

builder.Services.AddScoped<MediaProxy>(sp => new MediaProxy(
    sp.GetRequiredService<ILogger<MediaProxy>>(),
    new CircuitBreaker(),
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(MediaProxy))));

builder.Services.AddScoped<NotificationProxy>(sp => new NotificationProxy(
    sp.GetRequiredService<ILogger<NotificationProxy>>(),
    new CircuitBreaker(),
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(NotificationProxy))));

// Validation
builder.Services.AddValidatorsFromAssemblyContaining(typeof(LoginRequestValidator));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AuthService).Assembly));

// Middleware config
builder.Services.Configure<CorrelationIdOptions>(builder.Configuration.GetSection("Middleware:CorrelationId"));
builder.Services.Configure<TenantResolutionOptions>(builder.Configuration.GetSection("Middleware:TenantResolution"));
builder.Services.Configure<ErrorHandlingOptions>(builder.Configuration.GetSection("Middleware:ErrorHandling"));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("Middleware:RateLimiting"));

// Auth
var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!));
var jwtIssuer = builder.Configuration["Auth:Issuer"]
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? "KromicStore";
var jwtAudience = builder.Configuration["Auth:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "KromicStore";
var superUserJwtIssuer = Environment.GetEnvironmentVariable("SUPERUSER_JWT_ISSUER") ?? jwtIssuer;
var superUserJwtAudience = Environment.GetEnvironmentVariable("SUPERUSER_JWT_AUDIENCE") ?? jwtAudience;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtSigningKey,
            ValidateIssuer = true,
            ValidIssuers = new[] { jwtIssuer, superUserJwtIssuer }.Distinct(),
            ValidateAudience = true,
            ValidAudiences = new[] { jwtAudience, superUserJwtAudience }.Distinct(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = "sub",
            RoleClaimType = ClaimTypes.Role
        };
        opt.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var userIdClaim = principal?.FindFirst("sub")?.Value
                    ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var tokenVersionClaim = principal?.FindFirst("token_version")?.Value;

                if (!Guid.TryParse(userIdClaim, out var userId) ||
                    !int.TryParse(tokenVersionClaim, out var tokenVersion))
                {
                    context.Fail("Token is missing required identity claims.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var isSuperUser = principal?.FindFirst("type")?.Value == "superuser";

                var isValid = isSuperUser
                    ? await db.SuperUsers
                        .AsNoTracking()
                        .AnyAsync(su => su.Id == userId && su.IsActive && su.TokenVersion == tokenVersion)
                    : await db.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Id == userId && u.IsActive && u.TokenVersion == tokenVersion);

                if (!isValid)
                {
                    context.Fail("Token has been revoked or the account is inactive.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperUserOnly", policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim("type", "superuser")
            .RequireRole("SuperUser"));

    foreach (var permission in new[]
    {
        Permissions.ProductsRead, Permissions.ProductsWrite,
        Permissions.OrdersRead, Permissions.OrdersWrite,
        Permissions.CustomersRead, Permissions.CustomersWrite,
        Permissions.ThemesRead, Permissions.ThemesWrite,
        Permissions.StoreRead, Permissions.StoreWrite,
        Permissions.BillingRead, Permissions.BillingWrite,
        Permissions.AnalyticsRead,
        Permissions.StaffRead, Permissions.StaffWrite,
        Permissions.SettingsRead, Permissions.SettingsWrite,
        Permissions.DomainsRead, Permissions.DomainsWrite
    })
    {
        options.AddPolicy(permission, policy =>
            policy.RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission)));
    }
});

// Compression
builder.Services.AddResponseCompression(opt =>
{
    opt.Providers.Add<GzipCompressionProvider>();
    opt.Providers.Add<BrotliCompressionProvider>();
    opt.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json", "application/xml", "text/plain", "text/xml"
    }).ToArray();
});

builder.Services.Configure<GzipCompressionProviderOptions>(opt => opt.Level = System.IO.Compression.CompressionLevel.Optimal);
builder.Services.Configure<BrotliCompressionProviderOptions>(opt => opt.Level = System.IO.Compression.CompressionLevel.Optimal);

// Swagger
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalModelStateValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "KromicStore API",
        Version = "v1",
        Description = "Multi-tenant e-commerce API — manage products, orders, customers, payments, and webhooks.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "KromicStore Support",
            Email = "support@kromicstore.com",
            Url = new Uri("https://support.kromicstore.com")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter the JWT token from POST /api/v1/auth/login. Format: {token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Use full type names for schema IDs to avoid conflicts
    options.CustomSchemaIds(type => type.FullName);

    // Include XML comments if file present
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "KromicStore.API.xml");
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    // Add document filter to set server URL dynamically
    options.DocumentFilter<SwaggerDocumentFilter>();
});

// CORS
var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");

Log.Information("CORS_ALLOWED_ORIGINS from environment: {Origins}", allowedOrigins ?? "NULL");

var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

Log.Information("Parsed CORS origins: {Origins}", string.Join(", ", origins));

// Check if any origin contains a wildcard pattern
var hasWildcard = origins.Any(o => o.Contains("*"));

Log.Information("Has wildcard pattern: {HasWildcard}", hasWildcard);

builder.Services.AddCors(opt => 
{
    if (hasWildcard)
    {
        // Use SetIsOriginAllowed for wildcard support
        opt.AddPolicy("AllowAll", p => p
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) 
                {
                    Log.Information("CORS: Rejected - origin is null or whitespace");
                    return false;
                }
                
                Log.Information("CORS: Checking origin: {Origin}", origin);
                
                // Check against wildcard patterns
                foreach (var allowed in origins)
                {
                    if (allowed.Contains("*"))
                    {
                        // Convert wildcard pattern to regex
                        var pattern = allowed
                            .Replace(".", "\\.")
                            .Replace("*", ".*");
                        var isMatch = System.Text.RegularExpressions.Regex.IsMatch(origin, $"^{pattern}$");
                        Log.Information("CORS: Pattern {Pattern} matched {Origin}: {Match}", pattern, origin, isMatch);
                        if (isMatch)
                            return true;
                    }
                    else if (origin.Equals(allowed, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Information("CORS: Exact match found for {Origin}", origin);
                        return true;
                    }
                }
                Log.Information("CORS: Rejected origin {Origin} - no match found", origin);
                return false;
            }));
    }
    else
    {
        // Use WithOrigins for exact matches (more performant)
        opt.AddPolicy("AllowAll", p => p
            .WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
    }
});

// Hangfire
var hgConfig = builder.Configuration.GetSection("Hangfire");
if (hgConfig.GetValue<bool>("Enabled"))
{
    var hangfireDatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrWhiteSpace(hangfireDatabaseUrl))
    {
        throw new InvalidOperationException("DATABASE_URL is required for Hangfire");
    }

    // Convert DATABASE_URL from URL format to Npgsql connection string format
    var hgConnStr = ConvertDatabaseUrlToConnectionString(hangfireDatabaseUrl);
    
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hgConnStr)));
    
    var workers = hgConfig.GetValue<int>("WorkerCount", Environment.ProcessorCount);
    builder.Services.AddHangfireServer(opt =>
    {
        opt.WorkerCount = workers;
        opt.Queues = hgConfig.GetSection("Queues").Get<string[]>() ?? new[] { "default", "webhooks" };
    });
}

/// <summary>
/// Converts DATABASE_URL from URL format to Npgsql connection string format.
/// Example: postgresql://user:password@host:port/db -> Host=host;Port=port;Database=db;Username=user;Password=password
/// If already in connection string format, returns as-is.
/// Handles passwords with special characters by using proper URI parsing.
/// </summary>
static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
{
    try
    {
        // Check if it's already in connection string format (contains '=')
        if (databaseUrl.Contains('='))
        {
            return databaseUrl;
        }

        // Parse as URI - handle passwords with special characters
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo;
        
        // Split on last ':' to handle passwords containing ':'
        var lastColonIndex = userInfo.LastIndexOf(':');
        var username = lastColonIndex > 0 ? userInfo.Substring(0, lastColonIndex) : userInfo;
        var password = lastColonIndex > 0 ? userInfo.Substring(lastColonIndex + 1) : "";
        
        // URL decode the password to handle special characters like @
        password = System.Net.WebUtility.UrlDecode(password);
        
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to parse DATABASE_URL: {ex.Message}. Value: {databaseUrl}", ex);
    }
}

// App Insights
var aiConfig = builder.Configuration.GetSection("ApplicationInsights");
if (aiConfig.GetValue<bool>("Enabled"))
{
    var aiKey = aiConfig.GetValue<string>("InstrumentationKey");
    if (!string.IsNullOrEmpty(aiKey))
    {
#pragma warning disable CS0618
        builder.Services.AddApplicationInsightsTelemetry(aiKey);
#pragma warning restore CS0618
    }
}

// Health
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", failureStatus: HealthStatus.Unhealthy, tags: new[] { "database", "ready" })
    .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Degraded, tags: new[] { "cache", "ready" });

// Configure forwarded headers for reverse proxy (Render)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.RequireHeaderSymmetry = false;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// Pipeline
app.UseMiddleware<SubdomainRoutingMiddleware>();
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KromicStore API v1");
    c.RoutePrefix = "swagger";
    c.DefaultModelsExpandDepth(1);
});

app.UseHttpsRedirection();
app.UseResponseCompression();

if (hgConfig.GetValue<bool>("Enabled"))
{
    app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
    });
}

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = WriteHealthResponse,
    Predicate = _ => false // Liveness check - always returns 200 if app is running
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = WriteReadinessResponse,
    Predicate = check => check.Tags.Contains("ready") // Readiness check - verifies dependencies
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<DomainTenantResolutionMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Migrate DB
using (var scope = app.Services.CreateScope())
{
    try
    {
        Log.Information("Starting database migrations...");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        Log.Information("Database migrations completed successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Database migration failed. Application cannot start.");
        throw;
    }
}

Log.Information("KromicStore API started successfully. Version: 1.0.0, Environment: {Environment}", 
    builder.Environment.EnvironmentName);
Log.Information("Application ready to receive requests");

app.Run();

static Task WriteHealthResponse(HttpContext ctx, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    ctx.Response.Headers["X-Response-Time"] = DateTime.UtcNow.ToString("O");

    var liveness = new
    {
        status = "Healthy"
    };

    return ctx.Response.WriteAsJsonAsync(liveness);
}

static Task WriteReadinessResponse(HttpContext ctx, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    ctx.Response.Headers["X-Response-Time"] = DateTime.UtcNow.ToString("O");

    // Map check status to lowercase for JSON
    var checks = new Dictionary<string, object>();
    foreach (var entry in report.Entries)
    {
        checks[entry.Key] = new
        {
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            duration = entry.Value.Duration.TotalMilliseconds,
            data = entry.Value.Data?.Count > 0 ? entry.Value.Data : null
        };
    }

    var readiness = new
    {
        status = report.Status.ToString(),
        checks = checks,
        totalDuration = report.TotalDuration.TotalMilliseconds
    };

    // Set HTTP status code based on overall health
    if (report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy)
    {
        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    }
    else if (report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
    {
        ctx.Response.StatusCode = StatusCodes.Status200OK; // Still return 200 for degraded
    }

    return ctx.Response.WriteAsJsonAsync(readiness);
}
