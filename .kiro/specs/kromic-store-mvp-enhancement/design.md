# KromicStore MVP Enhancement - Design Document

## Overview

This design document provides the technical architecture and implementation patterns for KromicStore MVP Enhancement. The enhancement moves the platform from foundational architecture to production-ready MVP by introducing DTO separation, external service integration via proxy patterns, webhook system, configuration management, performance optimization, and core MVP features. All components follow SOLID principles with emphasis on testability, maintainability, and scalability.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Feature 1: DTO Separation & Contracts](#feature-1-dto-separation--contracts)
4. [Feature 2: Proxy Pattern & Service Integration](#feature-2-proxy-pattern--service-integration)
5. [Feature 3: Webhook System](#feature-3-webhook-system)
6. [Feature 4: Configuration Management](#feature-4-configuration-management)
7. [Feature 5: Performance Optimization](#feature-5-performance-optimization)
8. [Feature 6: MVP Features](#feature-6-mvp-features)
9. [Cross-Cutting Concerns](#cross-cutting-concerns)
10. [Dependency Injection Setup](#dependency-injection-setup)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      API Layer (HTTP)                            │
│  KromicStore.API - Controllers, Middleware, Routing             │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Contracts Layer (DTO Separation)                │
│  KromicStore.Contracts - API Request/Response DTOs              │
└────────────────────┬────────────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
┌──────────────────┐    ┌────────────────────────────────┐
│ Application Layer │    │  Infrastructure Layer          │
│  DTOs, Validators,    │  Data Access, External Svcs   │
│  Interfaces       │    │  Repositories, Unit of Work   │
└──────────┬────────┘    └────────────┬──────────────────┘
           │                         │
           └────────────┬────────────┘
                        ▼
        ┌───────────────────────────────────┐
        │       Domain Layer                 │
        │  Entities, Value Objects, Enums   │
        └───────────────────────────────────┘

Proxy Pattern (ServiceProxy<T>):
  ┌─────────────────────────────────┐
  │   ServiceProxy<TResponse>        │
  │   - Retry Logic (Exponential)    │
  │   - Circuit Breaker              │
  │   - Timeout Handling             │
  │   - Error Mapping                │
  └─────────────┬─────────────────────┘
                │
    ┌───────────┼───────────┬──────────────┐
    ▼           ▼           ▼              ▼
PaymentProxy OAuthProxy MediaProxy  NotificationProxy
(Razorpay)  (Google)   (Cloudinary)  (Brevo)
```

---

## Project Structure

### New Projects to Create

```
KromicStore/
├── src/
│   ├── KromicStore.Contracts/          [NEW]
│   │   ├── Abstractions/
│   │   │   ├── PagedResponse.cs
│   │   │   ├── ErrorResponse.cs
│   │   │   └── CollectionResponse.cs
│   │   ├── V1/
│   │   │   ├── Auth/
│   │   │   ├── Products/
│   │   │   ├── Orders/
│   │   │   ├── Customers/
│   │   │   ├── Webhooks/
│   │   │   ├── Configuration/
│   │   │   └── Common/
│   │   └── KromicStore.Contracts.csproj
│   │
│   ├── KromicStore.API/                [UPDATED]
│   │   ├── Proxies/                    [NEW]
│   │   │   ├── ServiceProxy.cs
│   │   │   ├── PaymentProxy.cs
│   │   │   ├── OAuthProxy.cs
│   │   │   ├── MediaProxy.cs
│   │   │   └── NotificationProxy.cs
│   │   ├── Middleware/                 [NEW]
│   │   │   ├── TenantResolutionMiddleware.cs
│   │   │   ├── ErrorHandlingMiddleware.cs
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   └── RateLimitingMiddleware.cs
│   │   ├── Configuration/              [NEW]
│   │   │   ├── ServiceProxyConfig.cs
│   │   │   ├── WebhookConfig.cs
│   │   │   └── PerformanceConfig.cs
│   │   └── Program.cs                  [UPDATED]
│   │
│   ├── KromicStore.Infrastructure/     [UPDATED]
│   │   ├── Services/
│   │   │   ├── WebhookService.cs       [NEW]
│   │   │   ├── ConfigurationService.cs [NEW]
│   │   │   ├── CacheService.cs         [UPDATED]
│   │   │   └── ...
│   │   ├── Data/
│   │   │   ├── Entities/               [NEW - Webhook, Config]
│   │   │   ├── AppDbContext.cs         [UPDATED]
│   │   │   └── ...
│   │   └── BackgroundJobs/             [NEW]
│   │       └── WebhookDeliveryJob.cs
│   │
│   ├── KromicStore.Application/        [UPDATED - minimal changes]
│   └── KromicStore.Domain/             [UPDATED - new enums/entities]
│
└── tests/
    ├── KromicStore.Tests.Unit/         [NEW]
    ├── KromicStore.Tests.Integration/  [NEW]
    └── KromicStore.Tests.Performance/  [NEW]
```

---

## Feature 1: DTO Separation & Contracts

### 1.1 Contracts Project Architecture

**KromicStore.Contracts** - New dedicated project containing all API DTOs.

```csharp
Namespace Structure:
KromicStore.Contracts
├── Abstractions
│   ├── PagedResponse<T>
│   ├── ErrorResponse
│   ├── CollectionResponse<T>
│   └── ApiResponse<T>
├── V1.Auth
│   ├── LoginRequest
│   ├── LoginResponse
│   ├── RegisterRequest
│   ├── RegisterResponse
│   ├── RefreshTokenRequest
│   └── RefreshTokenResponse
├── V1.Products
│   ├── CreateProductRequest
│   ├── UpdateProductRequest
│   ├── ProductResponse
│   ├── ProductListResponse
│   ├── CreateCategoryRequest
│   └── CategoryResponse
├── V1.Orders
│   ├── CreateOrderRequest
│   ├── OrderItemRequest
│   ├── OrderResponse
│   ├── OrderListResponse
│   ├── UpdateOrderStatusRequest
│   └── OrderDetailResponse
├── V1.Customers
│   ├── CreateCustomerRequest
│   ├── UpdateCustomerRequest
│   ├── CustomerResponse
│   ├── CustomerListResponse
│   └── AddressRequest
├── V1.Webhooks
│   ├── WebhookConfigurationRequest
│   ├── WebhookEventResponse
│   └── WebhookDeliveryLogResponse
├── V1.Configuration
│   ├── SystemConfigurationResponse
│   ├── TenantConfigurationRequest
│   └── ConfigurationAuditLogResponse
└── V1.Common
    └── PaginationRequest
```

### 1.2 DTO Patterns

```csharp
// Abstract Response Base Class
public abstract class ApiResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Generic Paged Response
public class PagedResponse<T> : ApiResponse
{
    public IReadOnlyList<T> Data { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

// Error Response
public class ErrorResponse : ApiResponse
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public IDictionary<string, string[]> Details { get; set; }
    public string TraceId { get; set; }
}

// Collection Response (non-paged)
public class CollectionResponse<T> : ApiResponse
{
    public IReadOnlyList<T> Data { get; set; }
    public int Count { get; set; }
}
```

### 1.3 DTO Organization by Module

**Pattern**: Each module has dedicated folders with Request/Response/List response DTOs

```csharp
// Products Module Example
KromicStore.Contracts/V1/Products/
├── CreateProductRequest.cs
├── UpdateProductRequest.cs
├── ProductResponse.cs
├── ProductListResponse.cs
├── ProductDetailResponse.cs
├── CreateCategoryRequest.cs
├── CategoryResponse.cs
└── CategoryListResponse.cs

// Each DTO includes validation attributes and XML documentation
public class CreateProductRequest
{
    /// <summary>
    /// Stock Keeping Unit - unique identifier within tenant
    /// </summary>
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50)]
    public string Sku { get; set; }

    /// <summary>
    /// Product name visible to customers
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    /// <summary>
    /// Detailed product description
    /// </summary>
    [StringLength(2000)]
    public string Description { get; set; }

    /// <summary>
    /// Price in base currency
    /// </summary>
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    /// <summary>
    /// Available quantity
    /// </summary>
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    /// <summary>
    /// Category ID for product classification
    /// </summary>
    public Guid? CategoryId { get; set; }
}

public class ProductResponse : ApiResponse
{
    public Guid Id { get; set; }
    public string Sku { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

---

## Feature 2: Proxy Pattern & Service Integration

### 2.1 ServiceProxy Base Class

Abstract generic base class for all external service integrations with retry, circuit breaker, and timeout handling.

```csharp
public abstract class ServiceProxy<TResponse>
{
    protected readonly ILogger<ServiceProxy<TResponse>> Logger;
    protected readonly ICircuitBreaker CircuitBreaker;
    protected readonly int TimeoutSeconds;
    protected readonly int MaxRetries;
    
    // Retry policy: exponential backoff starting at 100ms
    protected readonly int[] RetryDelaysMs = new[] { 100, 1000, 10000, 30000 };

    protected ServiceProxy(
        ILogger<ServiceProxy<TResponse>> logger,
        ICircuitBreaker circuitBreaker,
        int timeoutSeconds = 30,
        int maxRetries = 4)
    {
        Logger = logger;
        CircuitBreaker = circuitBreaker;
        TimeoutSeconds = timeoutSeconds;
        MaxRetries = maxRetries;
    }

    /// <summary>
    /// Core execute method with retry and circuit breaker logic
    /// </summary>
    protected async Task<ProxyResult<TResponse>> ExecuteAsync(
        Func<Task<TResponse>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        if (CircuitBreaker.IsOpen)
        {
            Logger.LogWarning($"Circuit breaker open for {operationName}");
            return ProxyResult<TResponse>.CircuitBreakerOpen();
        }

        int retryCount = 0;
        Exception lastException = null;

        while (retryCount <= MaxRetries)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

                Logger.LogInformation($"Executing {operationName}, attempt {retryCount + 1}/{MaxRetries + 1}");
                
                var result = await operation();
                CircuitBreaker.RecordSuccess();
                return ProxyResult<TResponse>.Success(result);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // Actual cancellation, not timeout
                throw;
            }
            catch (OperationCanceledException)
            {
                // Timeout occurred
                lastException = new TimeoutException(
                    $"{operationName} timed out after {TimeoutSeconds} seconds", ex);
                Logger.LogWarning(lastException, $"{operationName} timed out");
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                Logger.LogWarning(ex, $"{operationName} failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{operationName} failed unexpectedly");
                CircuitBreaker.RecordFailure();
                throw;
            }

            retryCount++;
            
            if (retryCount <= MaxRetries)
            {
                int delayMs = RetryDelaysMs[Math.Min(retryCount - 1, RetryDelaysMs.Length - 1)];
                Logger.LogInformation($"Retrying {operationName} after {delayMs}ms");
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        CircuitBreaker.RecordFailure();
        return ProxyResult<TResponse>.Failed(lastException ?? new ProxyException(
            $"{operationName} failed after {MaxRetries + 1} attempts", lastException));
    }
}

/// <summary>
/// Result wrapper for proxy operations
/// </summary>
public class ProxyResult<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsCircuitBreakerOpen { get; private set; }
    public T Data { get; private set; }
    public Exception Exception { get; private set; }

    public static ProxyResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static ProxyResult<T> Failed(Exception ex) => new() { IsSuccess = false, Exception = ex };
    public static ProxyResult<T> CircuitBreakerOpen() => new() 
    { 
        IsSuccess = false, 
        IsCircuitBreakerOpen = true, 
        Exception = new ProxyException("Circuit breaker is open") 
    };
}

public class ProxyException : Exception
{
    public string ErrorCode { get; set; } = "SERVICE_UNAVAILABLE";
    public ProxyException(string message, Exception inner = null) 
        : base(message, inner) { }
}
```

### 2.2 Circuit Breaker Implementation

```csharp
public interface ICircuitBreaker
{
    bool IsOpen { get; }
    void RecordSuccess();
    void RecordFailure();
}

public class CircuitBreaker : ICircuitBreaker
{
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;

    private readonly int _failureThreshold = 5;  // Open after 5 failures
    private readonly int _resetTimeoutSeconds = 30;  // Attempt half-open after 30s

    public bool IsOpen => _state == CircuitBreakerState.Open && 
                          DateTime.UtcNow - _lastFailureTime < TimeSpan.FromSeconds(_resetTimeoutSeconds);

    public void RecordSuccess()
    {
        _failureCount = 0;
        _state = CircuitBreakerState.Closed;
    }

    public void RecordFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;

        if (_failureCount >= _failureThreshold)
        {
            _state = CircuitBreakerState.Open;
        }
    }
}

public enum CircuitBreakerState
{
    Closed,     // Normal operation
    Open,       // Too many failures, rejecting calls
    HalfOpen    // Testing if service recovered
}
```

### 2.3 Payment Proxy (Razorpay)

```csharp
public class PaymentProxy : ServiceProxy<PaymentResponse>
{
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly HttpClient _httpClient;

    public PaymentProxy(
        ILogger<PaymentProxy> logger,
        ICircuitBreaker circuitBreaker,
        IConfiguration config,
        HttpClient httpClient)
        : base(logger, circuitBreaker)
    {
        _apiKey = config["ExternalServices:Razorpay:ApiKey"];
        _apiSecret = config["ExternalServices:Razorpay:ApiSecret"];
        _httpClient = httpClient;
    }

    /// <summary>
    /// Create payment with idempotency support
    /// </summary>
    public async Task<ProxyResult<PaymentResponse>> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePaymentRequest(request);

        return await ExecuteAsync(async () =>
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "amount", request.Amount.ToString() },
                { "currency", request.Currency ?? "INR" },
                { "receipt", request.IdempotencyKey },
                { "description", request.Description }
            });

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, 
                "https://api.razorpay.com/v1/orders")
            {
                Content = content
            };

            // Add idempotency key for safe retry
            httpRequest.Headers.Add("Idempotency-Key", request.IdempotencyKey);
            httpRequest.Headers.Add("Authorization", 
                $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_apiKey}:{_apiSecret}"))}");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var paymentData = JsonSerializer.Deserialize<PaymentResponse>(jsonContent);

            return paymentData;
        },
        "CreatePayment",
        cancellationToken);
    }

    /// <summary>
    /// Verify payment with Razorpay
    /// </summary>
    public async Task<ProxyResult<VerifyPaymentResponse>> VerifyPaymentAsync(
        VerifyPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            var response = await _httpClient.GetAsync(
                $"https://api.razorpay.com/v1/payments/{request.PaymentId}",
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<VerifyPaymentResponse>(jsonContent);
        },
        "VerifyPayment",
        cancellationToken);
    }

    private void ValidatePaymentRequest(CreatePaymentRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be positive");
        if (string.IsNullOrEmpty(request.IdempotencyKey))
            throw new ArgumentException("IdempotencyKey is required");
    }
}

public class CreatePaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Description { get; set; }
    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString();
}

public class PaymentResponse
{
    public string Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 2.4 OAuth Proxy (Google)

```csharp
public class OAuthProxy : ServiceProxy<OAuthTokenResponse>
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private readonly HttpClient _httpClient;

    public OAuthProxy(
        ILogger<OAuthProxy> logger,
        ICircuitBreaker circuitBreaker,
        IConfiguration config,
        HttpClient httpClient)
        : base(logger, circuitBreaker)
    {
        _clientId = config["ExternalServices:Google:ClientId"];
        _clientSecret = config["ExternalServices:Google:ClientSecret"];
        _redirectUri = config["ExternalServices:Google:RedirectUri"];
        _httpClient = httpClient;
    }

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    public async Task<ProxyResult<OAuthTokenResponse>> ExchangeCodeForTokenAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", authorizationCode },
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "grant_type", "authorization_code" },
                { "redirect_uri", _redirectUri }
            });

            var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<OAuthTokenResponse>(jsonContent);
        },
        "ExchangeCodeForToken",
        cancellationToken);
    }

    /// <summary>
    /// Retrieve user profile using access token
    /// </summary>
    public async Task<ProxyResult<GoogleUserProfile>> GetUserProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://www.googleapis.com/oauth2/v2/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<GoogleUserProfile>(jsonContent);
        },
        "GetUserProfile",
        cancellationToken);
    }
}

public class OAuthTokenResponse
{
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; }
    public string RefreshToken { get; set; }
}

public class GoogleUserProfile
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string Picture { get; set; }
}
```

### 2.5 Media Proxy (Cloudinary)

```csharp
public class MediaProxy : ServiceProxy<CloudinaryUploadResponse>
{
    private readonly string _cloudName;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly HttpClient _httpClient;

    public MediaProxy(
        ILogger<MediaProxy> logger,
        ICircuitBreaker circuitBreaker,
        IConfiguration config,
        HttpClient httpClient)
        : base(logger, circuitBreaker)
    {
        _cloudName = config["ExternalServices:Cloudinary:CloudName"];
        _apiKey = config["ExternalServices:Cloudinary:ApiKey"];
        _apiSecret = config["ExternalServices:Cloudinary:ApiSecret"];
        _httpClient = httpClient;
    }

    /// <summary>
    /// Upload file to Cloudinary with transformations
    /// </summary>
    public async Task<ProxyResult<CloudinaryUploadResponse>> UploadAsync(
        Stream fileStream,
        string fileName,
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", fileName);
            content.Add(new StringContent(folderPath), "folder");
            content.Add(new StringContent("auto"), "quality");
            content.Add(new StringContent("true"), "eager");
            content.Add(new StringContent("300x300,150x150"), "eager_width");

            var response = await _httpClient.PostAsync(
                $"https://api.cloudinary.com/v1_1/{_cloudName}/upload",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CloudinaryUploadResponse>(jsonContent);
        },
        "UploadToCloudinary",
        cancellationToken);
    }

    /// <summary>
    /// Delete file from Cloudinary
    /// </summary>
    public async Task<ProxyResult<CloudinaryDeleteResponse>> DeleteAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async () =>
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "public_id", publicId }
            });

            var response = await _httpClient.PostAsync(
                $"https://api.cloudinary.com/v1_1/{_cloudName}/destroy",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CloudinaryDeleteResponse>(jsonContent);
        },
        "DeleteFromCloudinary",
        cancellationToken);
    }

    /// <summary>
    /// Generate optimized URL for different contexts
    /// </summary>
    public string GenerateUrl(string publicId, int width = 0, int height = 0, 
        string transformation = "")
    {
        var url = $"https://res.cloudinary.com/{_cloudName}/image/upload/";
        
        if (!string.IsNullOrEmpty(transformation))
            url += $"{transformation}/";
        else if (width > 0 || height > 0)
            url += $"w_{width},h_{height},c_fill,q_auto/";

        url += publicId;
        return url;
    }
}

public class CloudinaryUploadResponse
{
    public string PublicId { get; set; }
    public string Url { get; set; }
    public string SecureUrl { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CloudinaryDeleteResponse
{
    public string Result { get; set; }
}
```

### 2.6 Notification Proxy (Brevo)

```csharp
public class NotificationProxy : ServiceProxy<BrevoSendResponse>
{
    private readonly string _apiKey;
    private readonly string _senderEmail;
    private readonly HttpClient _httpClient;

    public NotificationProxy(
        ILogger<NotificationProxy> logger,
        ICircuitBreaker circuitBreaker,
        IConfiguration config,
        HttpClient httpClient)
        : base(logger, circuitBreaker, timeoutSeconds: 15)
    {
        _apiKey = config["ExternalServices:Brevo:ApiKey"];
        _senderEmail = config["ExternalServices:Brevo:SenderEmail"];
        _httpClient = httpClient;
    }

    /// <summary>
    /// Send transactional email via Brevo
    /// </summary>
    public async Task<ProxyResult<BrevoSendResponse>> SendEmailAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateEmailRequest(request);

        return await ExecuteAsync(async () =>
        {
            var payload = new
            {
                to = new[] { new { email = request.To, name = request.ToName } },
                sender = new { email = _senderEmail, name = "KromicStore" },
                subject = request.Subject,
                templateId = request.TemplateId,
                params = request.TemplateParameters
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                "https://api.brevo.com/v3/smtp/email")
            {
                Content = content
            };
            httpRequest.Headers.Add("api-key", _apiKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<BrevoSendResponse>(jsonContent);
        },
        "SendEmail",
        cancellationToken);
    }

    private void ValidateEmailRequest(SendEmailRequest request)
    {
        if (string.IsNullOrEmpty(request.To))
            throw new ArgumentException("Recipient email is required");
        if (request.TemplateId <= 0)
            throw new ArgumentException("Valid template ID is required");
    }
}

public class SendEmailRequest
{
    public string To { get; set; }
    public string ToName { get; set; }
    public string Subject { get; set; }
    public int TemplateId { get; set; }
    public Dictionary<string, string> TemplateParameters { get; set; }
}

public class BrevoSendResponse
{
    public string MessageId { get; set; }
}
```

---

## Feature 3: Webhook System

### 3.1 Webhook Domain Entities

```csharp
// Infrastructure/Data/Entities/WebhookConfiguration.cs
public class WebhookConfiguration : BaseEntity
{
    public Guid TenantId { get; set; }
    public string EndpointUrl { get; set; }
    public WebhookEventType[] EventTypes { get; set; }
    public string Secret { get; set; }  // For signature generation
    public bool IsActive { get; set; } = true;
    public int RetryCount { get; set; } = 0;
    public DateTime? LastAttemptAt { get; set; }
    public string AuthenticationHeader { get; set; }  // Optional: custom auth

    public static WebhookConfiguration Create(
        Guid tenantId,
        string endpointUrl,
        WebhookEventType[] eventTypes)
    {
        return new WebhookConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EndpointUrl = endpointUrl,
            EventTypes = eventTypes,
            Secret = GenerateSecret(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string GenerateSecret()
    {
        using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
        {
            byte[] tokenData = new byte[32];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData);
        }
    }
}

// Infrastructure/Data/Entities/WebhookEventLog.cs
public class WebhookEventLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }  // For deduplication
    public WebhookEventType EventType { get; set; }
    public string Payload { get; set; }  // JSON serialized
    public DateTime OccurredAt { get; set; }
    public string IdempotencyKey { get; set; }

    public static WebhookEventLog Create(
        Guid tenantId,
        WebhookEventType eventType,
        object payload)
    {
        return new WebhookEventLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventId = Guid.NewGuid(),
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAt = DateTime.UtcNow,
            IdempotencyKey = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };
    }
}

// Infrastructure/Data/Entities/WebhookDeliveryLog.cs
public class WebhookDeliveryLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid WebhookConfigurationId { get; set; }
    public Guid WebhookEventLogId { get; set; }
    public WebhookEventType EventType { get; set; }
    public string EndpointUrl { get; set; }
    public int HttpStatusCode { get; set; }
    public string Response { get; set; }
    public int RetryCount { get; set; }
    public bool IsSuccess { get; set; }
    public DateTime DeliveryAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }

    // Retry delays: 1s, 10s, 100s, 1000s (17+ minutes total)
    public static int[] RetryDelaysSeconds = new[] { 1, 10, 100, 1000, 10000 };

    public DateTime? CalculateNextRetry()
    {
        if (RetryCount >= RetryDelaysSeconds.Length)
            return null;  // Max retries exceeded

        int delaySeconds = RetryDelaysSeconds[RetryCount];
        return DateTime.UtcNow.AddSeconds(delaySeconds);
    }
}
```

### 3.2 Webhook Event Types

```csharp
public enum WebhookEventType
{
    OrderCreated = 1,
    OrderStatusChanged = 2,
    OrderCancelled = 3,
    PaymentProcessed = 4,
    PaymentFailed = 5,
    TenantCreated = 6,
    SubscriptionChanged = 7,
    SubscriptionCancelled = 8,
    ProductPublished = 9,
    ProductUnpublished = 10,
    CustomerCreated = 11
}

// Webhook event envelope sent to external systems
public class WebhookEvent
{
    public Guid EventId { get; set; }
    public WebhookEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid TenantId { get; set; }
    public string IdempotencyKey { get; set; }
    public object Payload { get; set; }
    public int ApiVersion { get; set; } = 1;
}
```

### 3.3 Webhook Service

```csharp
public interface IWebhookService
{
    Task<WebhookConfiguration> RegisterWebhookAsync(
        Guid tenantId,
        string endpointUrl,
        WebhookEventType[] eventTypes,
        CancellationToken cancellationToken = default);

    Task PublishEventAsync(
        Guid tenantId,
        WebhookEventType eventType,
        object payload,
        CancellationToken cancellationToken = default);

    Task RetryDeliveryAsync(
        Guid deliveryLogId,
        CancellationToken cancellationToken = default);
}

public class WebhookService : IWebhookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobs,
        ILogger<WebhookService> logger)
    {
        _unitOfWork = unitOfWork;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task<WebhookConfiguration> RegisterWebhookAsync(
        Guid tenantId,
        string endpointUrl,
        WebhookEventType[] eventTypes,
        CancellationToken cancellationToken = default)
    {
        // Validate endpoint is reachable
        using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
        {
            try
            {
                var response = await client.HeadAsync(endpointUrl, cancellationToken);
                if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.MethodNotAllowed)
                    throw new InvalidOperationException($"Endpoint returned {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook endpoint validation failed");
                throw new InvalidOperationException("Cannot reach webhook endpoint", ex);
            }
        }

        var config = WebhookConfiguration.Create(tenantId, endpointUrl, eventTypes);
        await _unitOfWork.WebhookConfigurations.AddAsync(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return config;
    }

    public async Task PublishEventAsync(
        Guid tenantId,
        WebhookEventType eventType,
        object payload,
        CancellationToken cancellationToken = default)
    {
        // Store event log for audit and replay
        var eventLog = WebhookEventLog.Create(tenantId, eventType, payload);
        await _unitOfWork.WebhookEventLogs.AddAsync(eventLog);

        // Find matching webhook configurations
        var configs = await _unitOfWork.WebhookConfigurations
            .FindAsync(w => w.TenantId == tenantId && w.IsActive &&
                           w.EventTypes.Contains(eventType), cancellationToken);

        // Queue delivery jobs
        foreach (var config in configs)
        {
            var webhookEvent = new WebhookEvent
            {
                EventId = eventLog.EventId,
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                TenantId = tenantId,
                IdempotencyKey = eventLog.IdempotencyKey,
                Payload = payload
            };

            _backgroundJobs.Enqueue<WebhookDeliveryJob>(
                job => job.DeliverAsync(config.Id, webhookEvent, CancellationToken.None));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

### 3.4 Webhook Delivery Job (Hangfire)

```csharp
public class WebhookDeliveryJob
{
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WebhookDeliveryJob> _logger;

    public WebhookDeliveryJob(
        HttpClient httpClient,
        IUnitOfWork unitOfWork,
        ILogger<WebhookDeliveryJob> logger)
    {
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task DeliverAsync(
        Guid webhookConfigId,
        WebhookEvent webhookEvent,
        CancellationToken cancellationToken = default)
    {
        var config = await _unitOfWork.WebhookConfigurations.GetByIdAsync(webhookConfigId);
        if (config == null)
        {
            _logger.LogWarning($"Webhook configuration {webhookConfigId} not found");
            return;
        }

        if (!config.IsActive)
        {
            _logger.LogInformation($"Webhook {webhookConfigId} is inactive");
            return;
        }

        try
        {
            var payload = JsonSerializer.Serialize(webhookEvent);
            var signature = GenerateSignature(payload, config.Secret);
            var timestamp = DateTime.UtcNow.ToString("O");

            var request = new HttpRequestMessage(HttpMethod.Post, config.EndpointUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            // Add signature verification headers
            request.Headers.Add("X-KromicStore-Signature", signature);
            request.Headers.Add("X-KromicStore-Timestamp", timestamp);
            request.Headers.Add("X-KromicStore-Event", webhookEvent.EventType.ToString());

            if (!string.IsNullOrEmpty(config.AuthenticationHeader))
                request.Headers.Add("Authorization", config.AuthenticationHeader);

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var response = await _httpClient.SendAsync(request, cts.Token);

            var deliveryLog = new WebhookDeliveryLog
            {
                Id = Guid.NewGuid(),
                TenantId = config.TenantId,
                WebhookConfigurationId = webhookConfigId,
                WebhookEventLogId = webhookEvent.EventId,
                EventType = webhookEvent.EventType,
                EndpointUrl = config.EndpointUrl,
                HttpStatusCode = (int)response.StatusCode,
                Response = await response.Content.ReadAsStringAsync(cancellationToken),
                RetryCount = 0,
                IsSuccess = response.IsSuccessStatusCode,
                DeliveryAttemptAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            if (!response.IsSuccessStatusCode)
            {
                deliveryLog.NextRetryAt = deliveryLog.CalculateNextRetry();
            }

            await _unitOfWork.WebhookDeliveryLogs.AddAsync(deliveryLog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Webhook delivery returned {response.StatusCode}: {deliveryLog.Response}");
            }

            _logger.LogInformation($"Webhook delivered successfully to {config.EndpointUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Webhook delivery failed for {webhookConfigId}");
            
            // Reschedule with exponential backoff
            var delay = TimeSpan.FromSeconds(WebhookDeliveryLog.RetryDelaysSeconds[0]);
            throw new InvalidOperationException($"Webhook delivery failed, will retry in {delay.TotalSeconds}s", ex);
        }
    }

    private string GenerateSignature(string payload, string secret)
    {
        using (var hmac = new System.Security.Cryptography.HMACSHA256(
            Encoding.UTF8.GetBytes(secret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return "sha256=" + Convert.ToHexString(hash).ToLower();
        }
    }
}
```

### 3.5 Webhook Signature Verification (Consumer Guide)

```csharp
// Example for external webhook consumer to verify signature
public class WebhookSignatureValidator
{
    public static bool VerifySignature(
        string payload,
        string signature,
        string secret,
        string timestamp,
        int maxAgeSeconds = 300)  // 5 minute window
    {
        // Verify timestamp to prevent replay attacks
        if (!DateTime.TryParse(timestamp, out var eventTime))
            return false;

        var age = DateTime.UtcNow - eventTime;
        if (age.TotalSeconds > maxAgeSeconds)
            return false;  // Request is too old

        // Verify signature
        using (var hmac = new System.Security.Cryptography.HMACSHA256(
            Encoding.UTF8.GetBytes(secret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expectedSignature = "sha256=" + Convert.ToHexString(hash).ToLower();
            return expectedSignature == signature;
        }
    }
}
```

---

## Feature 4: Configuration Management

### 4.1 Configuration Entities

```csharp
// Infrastructure/Data/Entities/TenantConfiguration.cs
public class TenantConfiguration : BaseEntity
{
    public Guid TenantId { get; set; }
    public string ConfigKey { get; set; }  // e.g., "notifications:email_enabled"
    public string ConfigValue { get; set; }  // JSON serialized
    public ConfigScope Scope { get; set; }  // Tenant or Platform
    public bool IsEncrypted { get; set; }
    public string Description { get; set; }
    public DateTime? ExpiresAt { get; set; }  // For temporary overrides

    public static TenantConfiguration Create(
        Guid tenantId,
        string key,
        object value,
        bool encrypt = false)
    {
        return new TenantConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConfigKey = key,
            ConfigValue = JsonSerializer.Serialize(value),
            Scope = ConfigScope.Tenant,
            IsEncrypted = encrypt,
            CreatedAt = DateTime.UtcNow
        };
    }
}

// Infrastructure/Data/Entities/ConfigurationAuditLog.cs
public class ConfigurationAuditLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public string ConfigurationKey { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public Guid ChangedBy { get; set; }  // UserId
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; }

    public static ConfigurationAuditLog Create(
        Guid tenantId,
        string key,
        string oldValue,
        string newValue,
        Guid changedBy,
        string reason = null)
    {
        return new ConfigurationAuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConfigurationKey = key,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum ConfigScope
{
    Platform = 1,  // SuperUser only
    Tenant = 2     // TenantAdmin only
}
```

### 4.2 Configuration Service

```csharp
public interface IConfigurationService
{
    Task<T> GetAsync<T>(string key, T defaultValue = null, Guid? tenantId = null);
    Task SetAsync<T>(string key, T value, Guid tenantId, string reason = null);
    Task<IDictionary<string, object>> GetSectionAsync(string sectionPrefix, Guid? tenantId = null);
    Task InvalidateCacheAsync(string key);
    Task<IEnumerable<ConfigurationAuditLog>> GetAuditLogAsync(
        Guid tenantId,
        DateTime? from = null,
        DateTime? to = null);
}

public class ConfigurationService : IConfigurationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _config;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IEncryptionService _encryption;

    public ConfigurationService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IConfiguration config,
        ITenantProvider tenantProvider,
        ILogger<ConfigurationService> logger,
        IEncryptionService encryption)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _config = config;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _encryption = encryption;
    }

    public async Task<T> GetAsync<T>(
        string key,
        T defaultValue = null,
        Guid? tenantId = null)
    {
        tenantId ??= _tenantProvider.TenantId;

        // Try cache first
        var cacheKey = $"{tenantId}:config:{key}";
        var cached = await _cacheService.GetAsync<T>(cacheKey);
        if (cached != null)
            return cached;

        // Try database
        var config = await _unitOfWork.TenantConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ConfigKey == key);

        if (config != null)
        {
            var value = config.IsEncrypted
                ? _encryption.Decrypt(config.ConfigValue)
                : config.ConfigValue;

            var result = JsonSerializer.Deserialize<T>(value);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }

        // Try appsettings as fallback
        var appValue = _config.GetValue<T>(key);
        if (appValue != null)
        {
            await _cacheService.SetAsync(cacheKey, appValue, TimeSpan.FromMinutes(30));
            return appValue;
        }

        return defaultValue;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        Guid tenantId,
        string reason = null)
    {
        var existing = await _unitOfWork.TenantConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ConfigKey == key);

        var oldValue = existing?.ConfigValue;

        if (existing != null)
        {
            // Update existing
            var serialized = JsonSerializer.Serialize(value);
            existing.ConfigValue = serialized;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new
            var newConfig = TenantConfiguration.Create(tenantId, key, value);
            await _unitOfWork.TenantConfigurations.AddAsync(newConfig);
        }

        // Create audit log
        var auditLog = ConfigurationAuditLog.Create(
            tenantId,
            key,
            oldValue,
            JsonSerializer.Serialize(value),
            _tenantProvider.UserId,
            reason);

        await _unitOfWork.ConfigurationAuditLogs.AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate cache
        await InvalidateCacheAsync(key, tenantId);

        _logger.LogInformation($"Configuration {key} updated for tenant {tenantId}");
    }

    private async Task InvalidateCacheAsync(string key, Guid? tenantId = null)
    {
        tenantId ??= _tenantProvider.TenantId;
        var cacheKey = $"{tenantId}:config:{key}";
        await _cacheService.RemoveAsync(cacheKey);
    }

    public async Task<IDictionary<string, object>> GetSectionAsync(
        string sectionPrefix,
        Guid? tenantId = null)
    {
        tenantId ??= _tenantProvider.TenantId;

        var configs = await _unitOfWork.TenantConfigurations
            .FindAsync(c => c.TenantId == tenantId && c.ConfigKey.StartsWith(sectionPrefix));

        var result = new Dictionary<string, object>();
        foreach (var config in configs)
        {
            result[config.ConfigKey] = config.ConfigValue;
        }

        return result;
    }

    public async Task<IEnumerable<ConfigurationAuditLog>> GetAuditLogAsync(
        Guid tenantId,
        DateTime? from = null,
        DateTime? to = null)
    {
        return await _unitOfWork.ConfigurationAuditLogs
            .FindAsync(l => l.TenantId == tenantId &&
                           (from == null || l.ChangedAt >= from) &&
                           (to == null || l.ChangedAt <= to),
                      orderBy: q => q.OrderByDescending(l => l.ChangedAt));
    }
}
```

---

## Feature 5: Performance Optimization

### 5.1 Database Indexing Strategy

**Index Naming Convention**: `IX_{TableName}_{ColumnList}_{Suffix}`
- Suffix: PK (primary key), UC (unique), IX (regular), FX (filtered)

**Essential Indexes** (Create via Fluent API in AppDbContext):

```csharp
// Multi-tenancy indexes
modelBuilder.Entity<Product>()
    .HasIndex(p => new { p.TenantId, p.Id })
    .HasName("IX_Products_TenantId_Id");

modelBuilder.Entity<Order>()
    .HasIndex(o => new { o.TenantId, o.Id })
    .HasName("IX_Orders_TenantId_Id");

// Status-based queries
modelBuilder.Entity<Order>()
    .HasIndex(o => new { o.TenantId, o.OrderStatus })
    .HasName("IX_Orders_TenantId_Status");

modelBuilder.Entity<Product>()
    .HasIndex(p => new { p.TenantId, p.ProductStatus })
    .HasName("IX_Products_TenantId_Status");

// Authentication/lookup
modelBuilder.Entity<User>()
    .HasIndex(u => new { u.TenantId, u.Email })
    .IsUnique()
    .HasName("UX_Users_TenantId_Email");

// Date range queries
modelBuilder.Entity<Order>()
    .HasIndex(o => new { o.TenantId, o.CreatedAt })
    .HasName("IX_Orders_TenantId_CreatedAt");

modelBuilder.Entity<OrderItem>()
    .HasIndex(oi => new { oi.OrderId, oi.CreatedAt })
    .HasName("IX_OrderItems_OrderId_CreatedAt");

// Foreign key performance
modelBuilder.Entity<OrderItem>()
    .HasIndex(oi => oi.ProductId)
    .HasName("IX_OrderItems_ProductId");

// Filtered indexes for active entities
modelBuilder.Entity<Product>()
    .HasIndex(p => new { p.TenantId, p.ProductStatus })
    .HasFilter("[ProductStatus] = 1")  // Published status
    .HasName("IX_Products_Active_TenantId");

// Full-text search support (PostgreSQL)
modelBuilder.Entity<Product>()
    .HasIndex(p => new { p.Name, p.Description })
    .HasName("IX_Products_FullText");
```

### 5.2 Query Optimization Patterns

**Pattern 1: Projection (Select only needed columns)**

```csharp
// ❌ Bad: Fetching entire entity
var products = await _unitOfWork.Products
    .GetAllAsync();

// ✅ Good: Projecting only needed properties
var products = await _unitOfWork.Products
    .Query()
    .Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price.Amount
    })
    .ToListAsync();
```

**Pattern 2: Pagination**

```csharp
public async Task<PagedResponse<ProductResponse>> GetProductsAsync(
    Guid tenantId,
    int pageNumber = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default)
{
    const int MaxPageSize = 100;
    pageSize = Math.Min(pageSize, MaxPageSize);
    pageSize = Math.Max(pageSize, 1);

    var query = _unitOfWork.Products
        .Query()
        .Where(p => p.TenantId == tenantId);

    var totalCount = await query.CountAsync(cancellationToken);

    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new ProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price.Amount
        })
        .ToListAsync(cancellationToken);

    return new PagedResponse<ProductResponse>
    {
        Data = items,
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
```

**Pattern 3: Explicit Joins (avoid lazy loading)**

```csharp
// ❌ Bad: Lazy loading causes N+1 query problem
var orders = await _unitOfWork.Orders
    .FindAsync(o => o.TenantId == tenantId);

foreach (var order in orders)
{
    var items = order.OrderItems;  // ← Additional query per order
}

// ✅ Good: Eager loading with Include
var orders = await _unitOfWork.Orders
    .Query()
    .Where(o => o.TenantId == tenantId)
    .Include(o => o.OrderItems)
    .Select(o => new OrderDetailResponse
    {
        Id = o.Id,
        Items = o.OrderItems.Select(oi => new OrderItemResponse
        {
            ProductId = oi.ProductId,
            Quantity = oi.Quantity
        }).ToList()
    })
    .ToListAsync();
```

**Pattern 4: Full-Text Search (PostgreSQL)**

```csharp
// Using PostgreSQL full-text search
var searchVector = EF.Functions.ToTsVector("english", p.Name + " " + p.Description);
var query = EF.Functions.ToTsQuery("english", searchTerm);

var results = await _unitOfWork.Products
    .Query()
    .Where(p => p.TenantId == tenantId &&
               searchVector.Matches(query))
    .OrderByDescending(p => EF.Functions.TsRank(searchVector, query))
    .Take(50)
    .ToListAsync();
```

**Pattern 5: Slow Query Logging**

```csharp
// In AppDbContext configuration
var serviceProvider = services.BuildServiceProvider()
    .GetRequiredService<ILoggerFactory>();

optionsBuilder
    .UseLoggerFactory(serviceProvider)
    .LogTo(query =>
    {
        if (query.Duration > TimeSpan.FromMilliseconds(500))
        {
            var logger = serviceProvider.CreateLogger("Database.Slow");
            logger.LogWarning($"Slow query ({query.Duration.TotalMilliseconds}ms): {query.Sql}");
        }
    });
```

### 5.3 Redis Cache Strategy

**Cache Key Scheme**: `{TenantId}:{EntityType}:{EntityId}`

```csharp
public class CacheKeys
{
    private const string Prefix = "kromic";

    // Single entity keys
    public static string ProductKey(Guid tenantId, Guid productId) =>
        $"{Prefix}:{tenantId}:product:{productId}";

    public static string CustomerKey(Guid tenantId, Guid customerId) =>
        $"{Prefix}:{tenantId}:customer:{customerId}";

    public static string OrderKey(Guid tenantId, Guid orderId) =>
        $"{Prefix}:{tenantId}:order:{orderId}";

    // Collection keys
    public static string ProductsListKey(Guid tenantId, int page = 1) =>
        $"{Prefix}:{tenantId}:products:list:{page}";

    public static string CategoriesListKey(Guid tenantId) =>
        $"{Prefix}:{tenantId}:categories:list";

    // Configuration keys
    public static string TenantConfigKey(Guid tenantId) =>
        $"{Prefix}:{tenantId}:config";

    public static string UserRoleKey(Guid userId) =>
        $"{Prefix}:user:{userId}:roles";

    // Cache tag for bulk invalidation
    public static string ProductTagKey(Guid tenantId) =>
        $"{Prefix}:{tenantId}:products:*";
}

// Cache TTLs
public static class CacheTTL
{
    public static readonly TimeSpan ProductCache = TimeSpan.FromHours(1);
    public static readonly TimeSpan CustomerCache = TimeSpan.FromHours(1);
    public static readonly TimeSpan OrderCache = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan ConfigCache = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan RoleCache = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ListCache = TimeSpan.FromMinutes(5);
}
```

### 5.4 Cache Invalidation Patterns

```csharp
public class CacheInvalidationService : IDomainEventHandler
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    // Subscribe to domain events for automatic cache invalidation
    public async Task HandleProductCreatedAsync(ProductCreatedDomainEvent evt)
    {
        await InvalidateProductCachesAsync(evt.TenantId);
        _logger.LogInformation($"Invalidated product caches for tenant {evt.TenantId}");
    }

    public async Task HandleOrderStatusChangedAsync(OrderStatusChangedDomainEvent evt)
    {
        // Invalidate order cache
        await _cache.RemoveAsync(CacheKeys.OrderKey(evt.TenantId, evt.OrderId));

        // Invalidate related customer cache
        var order = await GetOrderAsync(evt.OrderId);  // Get from DB
        await _cache.RemoveAsync(CacheKeys.CustomerKey(evt.TenantId, order.CustomerId));

        // Invalidate order list
        await InvalidateOrderListCachesAsync(evt.TenantId);
    }

    private async Task InvalidateProductCachesAsync(Guid tenantId)
    {
        // Clear all product-related caches using pattern
        await _cache.RemoveByPatternAsync(CacheKeys.ProductTagKey(tenantId));
        await _cache.RemoveAsync(CacheKeys.CategoriesListKey(tenantId));
    }

    private async Task InvalidateOrderListCachesAsync(Guid tenantId)
    {
        for (int page = 1; page <= 10; page++)
        {
            await _cache.RemoveAsync(CacheKeys.ProductsListKey(tenantId, page));
        }
    }
}
```

### 5.5 Database Connection Pooling

**Configuration in appsettings.json**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=kromicstore;Username=postgres;Password=password;Maximum Pool Size=25;Minimum Pool Size=5;Connection Idle Lifetime=300;"
  },
  "ConnectionPooling": {
    "MinPoolSize": 5,
    "MaxPoolSize": 25,
    "ConnectionIdleTimeout": 300,
    "MaxConnectionAge": 1800,
    "ConnectionTimeout": 30
  }
}
```

**Monitoring Connection Pool Health**:

```csharp
public class ConnectionPoolHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT 1", cancellationToken);

            // Get connection pool statistics if available
            var data = new Dictionary<string, object>
            {
                { "Status", "Healthy" },
                { "Timestamp", DateTime.UtcNow }
            };

            return HealthCheckResult.Healthy("Database connection pool is healthy", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
```

### 5.6 Hangfire Configuration

**Hangfire Optimization Configuration**:

```csharp
// In Program.cs
services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire",
        
        // Worker threads = CPU core count
        WorkerCount = Environment.ProcessorCount,
        
        // Expired job cleanup every hour
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        
        // Keep successful jobs for 1 hour
        SuccessfulJobExpirationInterval = TimeSpan.FromHours(1),
        
        // Failed job retention: 7 days
        FailedJobExpirationInterval = TimeSpan.FromDays(7),
        
        // Queue poll interval (in milliseconds)
        QueuePollInterval = TimeSpan.FromSeconds(15)
    }));

// Configure recurring jobs
RecurringJob.AddOrUpdate<ConfigurationCacheWarmupJob>(
    "config-cache-warmup",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Hourly);

RecurringJob.AddOrUpdate<WebhookRetryJob>(
    "webhook-retry-handler",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.MinuteInterval(5));

// Configure dashboard
app.UseHangfireDashboard("/admin/jobs", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
});
```

**Webhook Delivery Job with Retry**:

```csharp
public class WebhookRetryJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HttpClient _httpClient;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<WebhookRetryJob> _logger;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Find failed deliveries ready for retry
        var failedDeliveries = await _unitOfWork.WebhookDeliveryLogs
            .FindAsync(d => d.IsSuccess == false && d.NextRetryAt <= DateTime.UtcNow,
                      cancellationToken: cancellationToken);

        _logger.LogInformation($"Processing {failedDeliveries.Count()} webhook retries");

        foreach (var delivery in failedDeliveries)
        {
            // Increment retry count
            delivery.RetryCount++;

            if (delivery.RetryCount >= WebhookDeliveryLog.RetryDelaysSeconds.Length)
            {
                // Max retries exceeded
                delivery.IsSuccess = false;
                _logger.LogWarning($"Webhook {delivery.Id} max retries exceeded");
                continue;
            }

            // Queue retry
            _backgroundJobs.Schedule<WebhookDeliveryJob>(
                job => job.RetryAsync(delivery.Id, CancellationToken.None),
                TimeSpan.FromSeconds(5));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

### 5.7 API Response Compression

**Middleware Configuration**:

```csharp
// In Program.cs
services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();

    // Enable for specific types
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/xml",
        "text/plain",
        "text/xml"
    });

    // Minimum size to compress (1KB)
    options.MinimumCompressionSize = 1024;
});

services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

app.UseResponseCompression();
```

---

## Feature 6: MVP Features

### 6.1 Core MVP Entity Relationships

```
Tenant (1) ────┬────── (N) User
               ├────── (N) TenantConfiguration
               ├────── (N) SubscriptionPlan
               ├────── (N) Product
               ├────── (N) Category
               ├────── (N) Customer
               ├────── (N) Order
               ├────── (N) WebhookConfiguration
               └────── (N) WebhookEventLog

Category (1) ─────────── (N) Product

Product (1) ───────┬──── (N) OrderItem
                   └──── (N) Image (Media)

Customer (1) ───────┬──── (N) Order
                    └──── (N) Address

Order (1) ──────────┬──── (N) OrderItem
                    ├──── (1) Payment
                    └──── (1) Customer

OrderItem (N) ────────── (1) Product

Payment (1) ────────── (N) PaymentTransaction

User (1) ───────────── (N) Order (as Processor/Admin)

Subscription (1) ────────── (1) Tenant
```

### 6.2 Core Entities

**Product Aggregate**:

```csharp
// Domain/Entities/Product.cs
public class Product : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Sku { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Money Price { get; set; }
    public int StockQuantity { get; set; }
    public int? ReorderLevel { get; set; }
    public Guid? CategoryId { get; set; }
    public Category Category { get; set; }
    public ProductStatus Status { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Business rules
    public void Publish()
    {
        if (StockQuantity <= 0)
            throw new DomainException("Cannot publish product with zero stock");
        Status = ProductStatus.Published;
    }

    public void Unpublish() => Status = ProductStatus.Draft;

    public void ReduceStock(int quantity)
    {
        if (quantity > StockQuantity)
            throw new DomainException("Insufficient stock");
        StockQuantity -= quantity;
    }

    public void RestoreStock(int quantity) => StockQuantity += quantity;
}

// Domain/Enums/ProductStatus.cs
public enum ProductStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}
```

**Category Aggregate**:

```csharp
// Domain/Entities/Category.cs
public class Category : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public Category ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public int DisplayOrder { get; set; }

    public static Category Create(Guid tenantId, string name, string description, 
        Guid? parentId = null)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            ParentCategoryId = parentId,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

**Order Aggregate** (already exists, enhancements):

```csharp
// Domain/Entities/Order.cs - Enhanced
public class Order : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }
    public string OrderNumber { get; set; }  // Human-readable: ORD-20240115-00001
    public Money Subtotal { get; set; }
    public Money TaxAmount { get; set; }
    public Money ShippingCost { get; set; }
    public Money Total { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public Address ShippingAddress { get; set; }
    public Address BillingAddress { get; set; }
    public Guid? ProcessedBy { get; set; }  // UserId
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public Payment Payment { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public static Order Create(Guid tenantId, Guid customerId, 
        Address shippingAddress, Address billingAddress)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            OrderNumber = GenerateOrderNumber(),
            OrderStatus = OrderStatus.Pending,
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void ConfirmOrder()
    {
        if (OrderStatus != OrderStatus.Pending)
            throw new DomainException("Only pending orders can be confirmed");
        OrderStatus = OrderStatus.Confirmed;
    }

    public void ShipOrder()
    {
        OrderStatus = OrderStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
    }

    public void DeliverOrder()
    {
        OrderStatus = OrderStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (OrderStatus == OrderStatus.Delivered || OrderStatus == OrderStatus.Cancelled)
            throw new DomainException("Cannot cancel delivered or already cancelled order");
        OrderStatus = OrderStatus.Cancelled;
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..5].ToUpper()}";
}

// Domain/Enums/OrderStatus.cs
public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Paid = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6
}
```

**Payment Aggregate** (NEW):

```csharp
// Domain/Entities/Payment.cs
public class Payment : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; }
    public Money Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string ExternalPaymentId { get; set; }  // Razorpay ID
    public string PaymentMethod { get; set; }
    public DateTime? PaidAt { get; set; }
    public ICollection<PaymentTransaction> Transactions { get; set; }

    public static Payment Create(Guid tenantId, Guid orderId, Money amount)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = orderId,
            Amount = amount,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsProcessed(string externalId)
    {
        Status = PaymentStatus.Completed;
        ExternalPaymentId = externalId;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        Status = PaymentStatus.Failed;
    }
}

// Domain/Entities/PaymentTransaction.cs
public class PaymentTransaction : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; }  // Debit, Credit, Refund
    public string Status { get; set; }
    public string ExternalTransactionId { get; set; }
    public string Notes { get; set; }
}

// Domain/Enums/PaymentStatus.cs
public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Refunded = 5
}
```

**Customer Aggregate** (already exists, enhancements):

```csharp
// Domain/Entities/Customer.cs - Enhanced
public class Customer : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public Address BillingAddress { get; set; }
    public Address ShippingAddress { get; set; }
    public decimal LifetimeValue { get; set; }
    public int OrderCount { get; set; }
    public DateTime LastOrderAt { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public bool NewsletterSubscribed { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public string GetFullName() => $"{FirstName} {LastName}";

    public void UpdateLifetimeValue(decimal orderTotal)
    {
        LifetimeValue += orderTotal;
        OrderCount++;
        LastOrderAt = DateTime.UtcNow;
    }
}
```

**Subscription Model** (NEW):

```csharp
// Domain/Entities/Subscription.cs
public class Subscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public SubscriptionPlan PlanType { get; set; }
    public decimal MonthlyPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProducts { get; set; }
    public int MaxApiCallsPerMonth { get; set; }
    public bool WebhooksEnabled { get; set; }
    public bool AnalyticsEnabled { get; set; }
    public DateTime? TrialEndsAt { get; set; }

    public static Subscription CreateTrial(Guid tenantId, int trialDays = 14)
    {
        return new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanType = SubscriptionPlan.Starter,
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow,
            TrialEndsAt = DateTime.UtcNow.AddDays(trialDays),
            MaxUsers = 1,
            MaxProducts = 50,
            MaxApiCallsPerMonth = 1000,
            WebhooksEnabled = true,
            AnalyticsEnabled = false,
            CreatedAt = DateTime.UtcNow
        };
    }
}

// Domain/Enums/SubscriptionPlan.cs
public enum SubscriptionPlan
{
    Starter = 1,
    Professional = 2,
    Enterprise = 3
}

public enum SubscriptionStatus
{
    Trial = 1,
    Active = 2,
    Suspended = 3,
    Cancelled = 4,
    GracePeriod = 5
}

// Subscription plan features mapping
public static class SubscriptionPlanFeatures
{
    public static readonly Dictionary<SubscriptionPlan, (int MaxUsers, int MaxProducts, 
        int MaxApiCalls, decimal MonthlyPrice, bool WebhooksEnabled)> Plans = new()
    {
        { SubscriptionPlan.Starter, (1, 100, 10000, 99m, true) },
        { SubscriptionPlan.Professional, (5, 1000, 100000, 299m, true) },
        { SubscriptionPlan.Enterprise, (100, 50000, 1000000, 9999m, true) }
    };
}
```

### 6.3 API Endpoint Design

**RESTful Convention**:

```
Auth Endpoints:
POST   /api/v1/auth/register          - Register new tenant
POST   /api/v1/auth/login             - Login with credentials
POST   /api/v1/auth/refresh           - Refresh access token
POST   /api/v1/auth/oauth/google      - OAuth login

Product Endpoints:
GET    /api/v1/products               - List products (paginated)
GET    /api/v1/products/{id}          - Get product details
POST   /api/v1/products               - Create product (TenantAdmin+)
PUT    /api/v1/products/{id}          - Update product (TenantAdmin+)
DELETE /api/v1/products/{id}          - Delete product (TenantAdmin+)
POST   /api/v1/products/{id}/publish  - Publish product
POST   /api/v1/products/{id}/unpublish - Unpublish product

Category Endpoints:
GET    /api/v1/categories             - List categories
POST   /api/v1/categories             - Create category
PUT    /api/v1/categories/{id}        - Update category
DELETE /api/v1/categories/{id}        - Delete category

Order Endpoints:
GET    /api/v1/orders                 - List orders (tenant's)
GET    /api/v1/orders/{id}            - Get order details
POST   /api/v1/orders                 - Create order
PUT    /api/v1/orders/{id}            - Update order
POST   /api/v1/orders/{id}/confirm    - Confirm order
POST   /api/v1/orders/{id}/ship       - Mark as shipped
POST   /api/v1/orders/{id}/deliver    - Mark as delivered
POST   /api/v1/orders/{id}/cancel     - Cancel order

Customer Endpoints:
GET    /api/v1/customers              - List customers
GET    /api/v1/customers/{id}         - Get customer details
POST   /api/v1/customers              - Create customer
PUT    /api/v1/customers/{id}         - Update customer
GET    /api/v1/customers/{id}/orders  - Get customer's orders

Payment Endpoints:
POST   /api/v1/payments/create        - Initiate payment
GET    /api/v1/payments/{id}/verify   - Verify payment status
POST   /api/v1/payments/{id}/refund   - Request refund

Webhook Endpoints:
POST   /api/v1/webhooks               - Register webhook
GET    /api/v1/webhooks               - List registered webhooks
DELETE /api/v1/webhooks/{id}          - Unregister webhook
POST   /api/v1/webhooks/replay/{id}   - Replay event

Configuration Endpoints:
GET    /api/v1/admin/config           - Get platform config (SuperUser)
PUT    /api/v1/admin/config           - Update config (SuperUser)
GET    /api/v1/config                 - Get tenant config (TenantAdmin)
PUT    /api/v1/config                 - Update tenant config (TenantAdmin)
GET    /api/v1/admin/audit-logs       - Get config audit trail (SuperUser)
```

---

## Cross-Cutting Concerns

### 7.1 Error Handling & Logging

**Error Response Format**:

```csharp
public class ErrorResponse
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public IDictionary<string, string[]> Details { get; set; }
    public string TraceId { get; set; }
    public DateTime Timestamp { get; set; }
}

// HTTP Status Code Mapping
public static class ErrorCodeHttpStatusMap
{
    public static readonly Dictionary<string, int> StatusMap = new()
    {
        { "VALIDATION_ERROR", 400 },
        { "UNAUTHORIZED", 401 },
        { "FORBIDDEN", 403 },
        { "NOT_FOUND", 404 },
        { "CONFLICT", 409 },
        { "RATE_LIMIT_EXCEEDED", 429 },
        { "SERVICE_UNAVAILABLE", 503 },
        { "DOMAIN_ERROR", 400 },
        { "UNKNOWN_ERROR", 500 }
    };
}
```

**Middleware Implementation**:

```csharp
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.TraceIdentifier;
        context.Items["TraceId"] = traceId;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {TraceId}", traceId);
            await HandleExceptionAsync(context, ex, traceId);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, 
        string traceId)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException ex => new ErrorResponse
            {
                ErrorCode = "VALIDATION_ERROR",
                Message = "Input validation failed",
                Details = ex.Errors.GroupBy(f => f.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray()),
                TraceId = traceId
            },
            DomainException ex => new ErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                TraceId = traceId
            },
            _ => new ErrorResponse
            {
                ErrorCode = "UNKNOWN_ERROR",
                Message = "An unexpected error occurred",
                TraceId = traceId
            }
        };

        context.Response.StatusCode = ErrorCodeHttpStatusMap.StatusMap
            .GetValueOrDefault(response.ErrorCode, 500);

        return context.Response.WriteAsJsonAsync(response);
    }
}
```

### 7.2 Multi-Tenancy Enforcement

**Middleware to Extract & Validate Tenant**:

```csharp
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        // Extract tenant from JWT token or subdomain
        var tenantId = ExtractTenantId(context);

        if (tenantId == Guid.Empty)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "Missing tenant information" 
            });
            return;
        }

        // Set tenant in context
        tenantProvider.SetTenantId(tenantId);

        await _next(context);
    }

    private static Guid ExtractTenantId(HttpContext context)
    {
        // Try JWT token first
        var claim = context.User?.FindFirst("tenant_id");
        if (claim != null && Guid.TryParse(claim.Value, out var tenantId))
            return tenantId;

        // Try subdomain (e.g., tenant1.kromicstore.com)
        var host = context.Request.Host.Host;
        var subdomain = host.Split('.').FirstOrDefault();
        // Map subdomain to tenant ID from cache/database

        return Guid.Empty;
    }
}
```

### 7.3 Rate Limiting Implementation

**Sliding Window Counter per Plan**:

```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;

    public RateLimitingMiddleware(RequestDelegate next, IDistributedCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.TenantId;
        var cacheKey = $"ratelimit:{tenantId}:{DateTime.UtcNow:yyyyMMddHH}";

        // Get subscription plan to determine limit
        var limit = tenantId switch
        {
            _ => 100  // 100 requests per minute for starter
        };

        var requestCountStr = await _cache.GetStringAsync(cacheKey);
        var requestCount = int.Parse(requestCountStr ?? "0");

        if (requestCount >= limit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Add("Retry-After", "60");
            return;
        }

        requestCount++;
        await _cache.SetStringAsync(cacheKey, requestCount.ToString(), 
            TimeSpan.FromMinutes(1));

        context.Response.Headers.Add("X-RateLimit-Limit", limit.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", (limit - requestCount).ToString());

        await _next(context);
    }
}
```

---

## Dependency Injection Setup

### Program.cs Configuration

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Contracts & DTOs
builder.Services.AddScoped<IMapper, Mapper>();

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Infrastructure Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// External Service Proxies (Circuit Breakers)
var paymentCircuitBreaker = new CircuitBreaker();
var oauthCircuitBreaker = new CircuitBreaker();
var mediaCircuitBreaker = new CircuitBreaker();
var notificationCircuitBreaker = new CircuitBreaker();

builder.Services.AddScoped(_ => paymentCircuitBreaker);
builder.Services.AddScoped(_ => oauthCircuitBreaker);
builder.Services.AddScoped(_ => mediaCircuitBreaker);
builder.Services.AddScoped(_ => notificationCircuitBreaker);

builder.Services.AddScoped<PaymentProxy>();
builder.Services.AddScoped<OAuthProxy>();
builder.Services.AddScoped<MediaProxy>();
builder.Services.AddScoped<NotificationProxy>();

// HTTP Clients
builder.Services.AddHttpClient<PaymentProxy>();
builder.Services.AddHttpClient<OAuthProxy>();
builder.Services.AddHttpClient<MediaProxy>();
builder.Services.AddHttpClient<NotificationProxy>();

// Background Jobs
builder.Services.AddHangfire(configuration =>
    configuration.UsePostgreSqlStorage(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<WebhookDeliveryJob>();
builder.Services.AddScoped<WebhookRetryJob>();

// Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Middleware
var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.Run();
```

---

## Correctness Properties

### Property 1: Multi-Tenant Data Isolation

*For any* data query within a tenant context, all returned results shall contain only entities belonging to that tenant (TenantId matches context).

**Validates: Requirements 7.2**

### Property 2: Webhook Event Round Trip

*For any* webhook configuration and published event, the event shall be delivered to the endpoint with identical payload, signature, and timestamp format across retry attempts.

**Validates: Requirements 3.3, 3.4**

### Property 3: Inventory Consistency

*For any* order containing products, the reserved inventory quantity shall equal the sum of all OrderItem quantities, and no inventory shall be oversold across concurrent orders.

**Validates: Requirements 6.6**

### Property 4: Payment Status Atomicity

*For any* payment operation, the payment status and order status shall remain consistent: if payment succeeds, order status shall transition to Confirmed atomically.

**Validates: Requirements 6.7**

### Property 5: Configuration Audit Trail Completeness

*For any* configuration change, a corresponding audit log entry shall exist with changedBy, timestamp, oldValue, and newValue, and the audit log shall be queryable by tenant, date range, and configuration key.

**Validates: Requirements 4.4**

### Property 6: Cache Invalidation Correctness

*For any* cache invalidation operation, subsequent queries with the same key shall retrieve fresh data from the database within 100ms.

**Validates: Requirements 5.4**

### Property 7: Proxy Retry Success

*For any* transient external service failure (timeout, temporary HTTP 5xx), the proxy shall retry according to exponential backoff policy and succeed on subsequent attempt if service recovers.

**Validates: Requirements 2.1, 2.6**

### Property 8: Circuit Breaker Protection

*For any* external service with 5+ consecutive failures, the circuit breaker shall enter open state and reject new calls for 30 seconds, then transition to half-open to test recovery.

**Validates: Requirements 2.1**

---

## Summary

This design document provides comprehensive technical guidance for implementing the KromicStore MVP Enhancement. Key architectural decisions include:

1. **DTO Separation**: Dedicated Contracts project ensures API contracts are decoupled from business logic
2. **Proxy Pattern**: Standardized external service integration with built-in resilience (retry, circuit breaker, timeouts)
3. **Webhook System**: Event-driven architecture with guaranteed delivery, signature verification, and audit trail
4. **Configuration Management**: Hierarchical, auditable, runtime-updatable configuration with role-based access
5. **Performance**: Multi-layered optimization via indexing, query optimization, caching, connection pooling, and compression
6. **MVP Features**: Clean entity design supporting tenant registration, subscriptions, product management, orders, and payments

All patterns follow SOLID principles, are testable, and align with the Clean Architecture foundation already established in the KromicStore solution.

