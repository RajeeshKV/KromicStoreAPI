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
using StackExchange.Redis;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IO.Compression;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

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

// Cache
var redisConn = Environment.GetEnvironmentVariable("REDIS_URL");
IConnectionMultiplexer? redis = null;

if (!string.IsNullOrWhiteSpace(redisConn))
{
    try
    {
        var redisOpts = ConfigurationOptions.Parse(redisConn);
        redisOpts.AbortOnConnectFail = false; // Allow retrying
        redisOpts.ConnectRetry = 3;
        redisOpts.ConnectTimeout = 5000;
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
builder.Services.AddScoped<IAuthService, AuthService>();
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

builder.Services.AddHttpContextAccessor();

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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.Authority = Environment.GetEnvironmentVariable("JWT_AUTHORITY");
        opt.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        opt.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization();

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

    // Include XML comments if file present
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "KromicStore.API.xml");
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

// CORS
builder.Services.AddCors(opt => opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

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

        // Parse as URI
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
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

var app = builder.Build();

// Pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KromicStore API v1");
    c.RoutePrefix = "swagger";
    c.DefaultModelsExpandDepth(1);
});

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("AllowAll");

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
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
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
