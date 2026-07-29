# KromicStore MVP Enhancement - Tasks

## Task Execution Overview

**Total Tasks**: 60+ tasks organized in 10 sequential waves
**Estimated Duration**: 15-20 weeks (part-time) or 4-6 weeks (full-time)
**Approach**: Wave-based execution with parallel task execution within each wave

---

## WAVE 1: Foundation & Infrastructure (8 tasks, parallel)

### Task 1.1: Create KromicStore.Contracts Project

**Status**: not_started
**Dependencies**: None
**Priority**: Critical
**Effort**: 1.5 hours

#### Description
Create new .NET 8.0 class library project named `KromicStore.Contracts` to house all API DTOs. This project should have minimal dependencies (System libraries only) and serve as the single source of truth for API contracts. Update solution file to include the new project and configure build order.

#### Acceptance Criteria
- [x] New class library project `KromicStore.Contracts.csproj` created with net8.0 target framework
- [x] Project added to `KromicStore.sln` solution file
- [x] Project structure includes folders: `Abstractions`, `V1` (containing Auth, Products, Orders, Customers, Webhooks, Configuration, Common subdirectories)
- [x] No external NuGet dependencies except System libraries
- [x] Project compiles without warnings
- [x] Solution builds successfully with all existing projects

#### Implementation Notes
- Use standard C# class library template
- Configure to match existing projects' SDK style and format
- Ensure OutputPath and IntermediateOutputPath are configured
- Add license header to project file
- Update .gitignore if needed for bin/obj directories

#### Files to Create/Modify
- NEW: `src/KromicStore.Contracts/KromicStore.Contracts.csproj`
- NEW: `src/KromicStore.Contracts/Abstractions/` (folder)
- NEW: `src/KromicStore.Contracts/V1/` (folder structure)
- MODIFY: `KromicStore.sln`
- MODIFY: `Directory.Build.props` (if needed for consistency)

#### Related Requirements
- Requirement 1.1: Create Contracts Project
- Requirement 1.2: DTO Organization & Structure

---

### Task 1.2: Move and Organize DTOs into Contracts Project

**Status**: not_started
**Dependencies**: Task 1.1
**Priority**: Critical
**Effort**: 2 hours

#### Description
Move existing DTOs from `KromicStore.Application/DTOs` folder to `KromicStore.Contracts/V1/` with proper organization by module. Create dedicated folders for each feature area (Auth, Products, Orders, Customers) and organize Request/Response pairs. Maintain all existing DTO functionality and add comprehensive XML documentation.

#### Acceptance Criteria
- [x] All existing DTOs moved from Application to Contracts project
- [x] DTOs organized into V1/Auth, V1/Products, V1/Orders, V1/Customers folders
- [x] File naming follows pattern: {Feature}{Type}Request/Response (e.g., CreateProductRequest, ProductResponse)
- [x] All DTOs include comprehensive XML documentation comments (summary, remarks, parameter descriptions)
- [x] No circular reference dependencies between DTOs
- [x] All DTOs are JSON serializable (marked with [Serializable] or use System.Text.Json attributes)

#### Implementation Notes
- Each Request DTO should include validation attributes (Required, StringLength, Range, etc.)
- Response DTOs should omit request-only properties (passwords, tokens)
- Use namespaces: `KromicStore.Contracts.V1.{Feature}`
- Add file headers with copyright notice
- Ensure backward compatibility - all DTO properties remain unchanged

#### Files to Create/Modify
- NEW: `src/KromicStore.Contracts/V1/Auth/` (with request/response DTOs)
- NEW: `src/KromicStore.Contracts/V1/Products/` (with product/category DTOs)
- NEW: `src/KromicStore.Contracts/V1/Orders/` (with order DTOs)
- NEW: `src/KromicStore.Contracts/V1/Customers/` (with customer DTOs)
- REMOVE: Old DTO files from `src/KromicStore.Application/DTOs/`

#### Related Requirements
- Requirement 1.2: DTO Organization & Structure

---

### Task 1.3: Update Project References Across All Projects

**Status**: not_started
**Dependencies**: Task 1.2
**Priority**: Critical
**Effort**: 1.5 hours

#### Description
Update all project dependencies to reference `KromicStore.Contracts` instead of having local DTO definitions. Add project reference to KromicStore.Contracts in API, Application, and Infrastructure projects. Update all using statements to point to new Contracts namespace.

#### Acceptance Criteria
- [x] `KromicStore.API.csproj` references `KromicStore.Contracts`
- [x] `KromicStore.Application.csproj` references `KromicStore.Contracts`
- [x] `KromicStore.Infrastructure.csproj` references `KromicStore.Contracts`
- [x] All `using KromicStore.Application.DTOs` statements replaced with `using KromicStore.Contracts.V1.{Feature}`
- [x] All controller action signatures updated to use DTOs from Contracts project
- [x] No compiler errors or warnings remaining
- [x] Solution builds successfully
- [x] All existing functionality preserved

#### Implementation Notes
- Use Find & Replace to update namespaces
- Verify project reference structure in .csproj files
- Test that existing API endpoints still work with updated references

#### Files to Modify
- MODIFY: `src/KromicStore.API/KromicStore.API.csproj` (add ProjectReference to Contracts)
- MODIFY: `src/KromicStore.Application/KromicStore.Application.csproj`
- MODIFY: `src/KromicStore.Infrastructure/KromicStore.Infrastructure.csproj`
- MODIFY: All controller files (update using statements)
- MODIFY: All service files (update using statements)

#### Related Requirements
- Requirement 1.1: Create Contracts Project

---

### Task 1.4: Create DTO Abstraction Base Classes

**Status**: not_started
**Dependencies**: Task 1.1
**Priority**: High
**Effort**: 2 hours

#### Description
Create base/abstract classes for common DTO patterns used throughout API responses. Implement `PagedResponse<T>`, `ErrorResponse`, `CollectionResponse<T>`, and `ApiResponse` base class. These abstractions ensure consistency across all endpoints and reduce code duplication.

#### Acceptance Criteria
- [x] `ApiResponse` abstract base class created with Id (Guid), Timestamp (DateTime.UtcNow) properties
- [x] `PagedResponse<T>` generic class with Data, PageNumber, PageSize, TotalCount, computed properties (TotalPages, HasNextPage, HasPreviousPage)
- [x] `ErrorResponse` class with ErrorCode, Message, Details (IDictionary), TraceId properties
- [x] `CollectionResponse<T>` generic class with Data and Count properties
- [x] All base classes include XML documentation
- [x] Base classes support JSON serialization/deserialization
- [x] Unit tests verify computed property calculations

#### Implementation Notes
- Base classes located in `KromicStore.Contracts/Abstractions/`
- Ensure generic constraints where applicable
- Use init-only properties where appropriate for immutability
- Include validation in property setters (e.g., PageSize > 0)

#### Files to Create
- NEW: `src/KromicStore.Contracts/Abstractions/ApiResponse.cs`
- NEW: `src/KromicStore.Contracts/Abstractions/PagedResponse.cs`
- NEW: `src/KromicStore.Contracts/Abstractions/ErrorResponse.cs`
- NEW: `src/KromicStore.Contracts/Abstractions/CollectionResponse.cs`

#### Related Requirements
- Requirement 1.2: DTO Organization & Structure

---

### Task 1.5: Create ServiceProxy Base Class with Retry and Circuit Breaker

**Status**: not_started
**Dependencies**: Task 1.1
**Priority**: Critical
**Effort**: 2 hours

#### Description
Implement abstract `ServiceProxy<TResponse>` base class in Infrastructure project that provides standardized retry logic with exponential backoff, circuit breaker integration, timeout handling, and comprehensive logging. This class serves as foundation for all external service proxies (Razorpay, Google, Cloudinary, Brevo).

#### Acceptance Criteria
- [x] Generic `ServiceProxy<TResponse>` abstract class created in Infrastructure project
- [x] `ExecuteAsync` method implements retry logic with exponential backoff (100ms, 1s, 10s, 30s delays)
- [x] Timeout handling with configurable default (30 seconds)
- [x] Circuit breaker pattern integrated (configurable failure threshold, default 5)
- [x] All operations logged with attempt count, status, and timing
- [x] `ProxyResult<T>` wrapper class for handling success/failure/circuit-breaker-open states
- [x] `ProxyException` custom exception class for proxy-specific errors
- [x] Proper handling of CancellationToken throughout async operations

#### Implementation Notes
- Located in `src/KromicStore.Infrastructure/Proxies/ServiceProxy.cs`
- Use ILogger<ServiceProxy<TResponse>> for logging
- Implement retry delays as protected field (configurable per proxy subclass)
- Circuit breaker should transition through states: Closed → Open → Half-Open
- Include retry count in error messages and logs

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Proxies/ServiceProxy.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/ProxyResult.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/ProxyException.cs`

#### Related Requirements
- Requirement 2.1: Abstract Proxy Base Class

---

### Task 1.6: Create ICircuitBreaker Implementation

**Status**: not_started
**Dependencies**: Task 1.5
**Priority**: Critical
**Effort**: 1.5 hours

#### Description
Implement `ICircuitBreaker` interface and `CircuitBreaker` implementation class. Circuit breaker prevents repeated calls to failing services during recovery period. Manages state transitions (Closed → Open → Half-Open) based on failure count and time elapsed.

#### Acceptance Criteria
- [x] `ICircuitBreaker` interface defined with `IsOpen` property and `RecordSuccess()`, `RecordFailure()` methods
- [x] `CircuitBreaker` implementation manages failure count and state transitions
- [x] Circuit opens after 5 consecutive failures
- [x] Circuit remains open for 30 seconds, then transitions to half-open to test recovery
- [x] Any success resets failure count and closes circuit
- [x] Thread-safe (use locks if needed, or atomic operations)
- [x] Configurable failure threshold and timeout via constructor or static properties
- [x] Unit tests verify all state transitions

#### Implementation Notes
- Use enum for circuit breaker states: Closed, Open, HalfOpen
- Track last failure timestamp for timeout calculation
- Consider using ReaderWriterLockSlim for thread safety without blocking reads

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Proxies/ICircuitBreaker.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/CircuitBreaker.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/CircuitBreakerState.cs` (enum)

#### Related Requirements
- Requirement 2.1: Abstract Proxy Base Class

---

### Task 1.7: Set Up Middleware Infrastructure

**Status**: not_started
**Dependencies**: None (parallel to other tasks)
**Priority**: High
**Effort**: 2 hours

#### Description
Create core middleware components for tenant resolution, error handling, correlation ID tracking, and request logging. These middleware components will be applied globally in Program.cs to handle cross-cutting concerns.

#### Acceptance Criteria
- [x] `TenantResolutionMiddleware` extracts TenantId from JWT token or authentication context
- [x] `ErrorHandlingMiddleware` catches exceptions and returns standardized ErrorResponse
- [x] `CorrelationIdMiddleware` generates/propagates correlation ID for distributed tracing
- [x] `RateLimitingMiddleware` enforces rate limits based on subscription plan
- [x] All middleware log relevant information
- [x] Middleware properly handles async operations and error propagation
- [x] Middleware can be configured with options (bypass paths, etc.)

#### Implementation Notes
- Middleware located in `src/KromicStore.API/Middleware/`
- Correlation ID should be propagated to all downstream services
- Error middleware should mask sensitive information in responses
- Rate limiting should use distributed cache for accuracy across instances
- Ensure proper ordering: Correlation → Tenant → Error → RateLimit → Application

#### Files to Create
- NEW: `src/KromicStore.API/Middleware/CorrelationIdMiddleware.cs`
- NEW: `src/KromicStore.API/Middleware/TenantResolutionMiddleware.cs`
- NEW: `src/KromicStore.API/Middleware/ErrorHandlingMiddleware.cs`
- NEW: `src/KromicStore.API/Middleware/RateLimitingMiddleware.cs`

#### Related Requirements
- Requirement 7.1: Error Handling & Logging
- Requirement 7.2: Data Isolation & Multi-Tenancy
- Requirement 7.3: API Rate Limiting

---

### Task 1.8: Configure Program.cs with All Service Registrations

**Status**: not_started
**Dependencies**: Tasks 1.1-1.7 (parallel, but all infrastructure components created first)
**Priority**: Critical
**Effort**: 1.5 hours

#### Description
Configure Program.cs to register all services, middleware, and infrastructure components. Set up dependency injection container, configure middleware pipeline, add health checks, and enable logging/observability.

#### Acceptance Criteria
- [x] All application services registered (IAuthService, IWebhookService, IConfigurationService)
- [x] All infrastructure services registered (IUnitOfWork, ICacheService, IEncryptionService)
- [x] ServiceProxy and CircuitBreaker instances registered for each external service
- [x] HttpClient configured for external service proxies
- [x] Middleware added to pipeline in correct order
- [x] Serilog/structured logging configured
- [x] Health checks configured (database, cache, external services)
- [x] Swagger/OpenAPI configured for API documentation
- [x] Application starts without errors
- [x] Health check endpoints accessible

#### Implementation Notes
- Use separate extension methods for service registration (AddApplicationServices, AddInfrastructureServices, etc.)
- Configure Serilog with structured logging and correlation ID
- Set up OpenAPI with bearer authentication scheme
- Health checks should verify connectivity to critical dependencies
- Use appsettings.json for service configuration

#### Files to Modify
- MODIFY: `src/KromicStore.API/Program.cs` (major update)
- NEW: `src/KromicStore.API/Extensions/ServiceRegistrationExtensions.cs` (if using extension pattern)

#### Related Requirements
- All Requirements (cross-cutting)


---

## WAVE 2: External Service Proxies (5 tasks, parallel after Wave 1)

### Task 2.1: Implement PaymentProxy (Razorpay)

**Status**: not_started
**Dependencies**: Task 1.5, Task 1.6
**Priority**: Critical
**Effort**: 2.5 hours

#### Description
Implement PaymentProxy class extending ServiceProxy<T> for Razorpay payment gateway integration. Support payment creation with idempotency keys, verification, refund, and status query operations. Include comprehensive error handling and audit trail logging.

#### Acceptance Criteria
- [x] `PaymentProxy` class created extending `ServiceProxy<PaymentResponse>`
- [x] `CreatePaymentAsync` method validates request, creates order in Razorpay with idempotency support
- [x] `VerifyPaymentAsync` method retrieves payment status with retry logic
- [x] `RefundAsync` method processes refund with amount and reason
- [x] All operations use retry/circuit breaker from base class
- [x] Razorpay errors mapped to custom exceptions with meaningful messages
- [x] API key/secret loaded from secure configuration (appsettings)
- [x] All operations logged with amount, payment ID, and result
- [x] Unit tests verify retry behavior and error handling

#### Implementation Notes
- Use HttpClient for Razorpay API calls
- Validate payment amount and currency before calling API
- Generate random idempotency key if not provided
- Encrypt sensitive data (API keys) in configuration
- Support both production and test Razorpay environments

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Proxies/PaymentProxy.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/Models/CreatePaymentRequest.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/Models/PaymentResponse.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/Models/VerifyPaymentRequest.cs`

#### Related Requirements
- Requirement 2.2: Razorpay Payment Gateway Proxy
- Requirement 2.6: Proxy Error Handling & Recovery

---

### Task 2.2: Implement OAuthProxy (Google)

**Status**: not_started
**Dependencies**: Task 1.5, Task 1.6
**Priority**: High
**Effort**: 2 hours

#### Description
Implement OAuthProxy class for Google OAuth 2.0 integration. Support authorization code exchange for access token, user profile retrieval, token refresh, and token expiration detection with clear error messages distinguishing token issues from other failures.

#### Acceptance Criteria
- [x] `OAuthProxy` class created extending `ServiceProxy<OAuthTokenResponse>`
- [x] `ExchangeCodeForTokenAsync` method exchanges authorization code for access/refresh tokens
- [x] `GetUserProfileAsync` method retrieves user info using access token
- [x] `RefreshTokenAsync` method obtains new access token using refresh token
- [x] Token expiration detected and reported separately from other errors
- [x] OAuth tokens stored with rotation capability (encrypted in database)
- [x] Retry logic handles transient Google API failures
- [~] Circuit breaker prevents repeated calls during outages
- [x] All operations logged with user email and operation result

#### Implementation Notes
- Use HttpClient for Google OAuth endpoints
- Handle token expiry with 5-minute buffer for proactive refresh
- Support PKCE flow for enhanced security (if needed)
- Map Google error codes to application-specific exceptions
- Validate redirect URI against configured whitelist

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Proxies/OAuthProxy.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/Models/OAuthTokenResponse.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/Models/GoogleUserProfile.cs`

#### Related Requirements
- Requirement 2.3: Google OAuth Proxy
- Requirement 2.6: Proxy Error Handling & Recovery

---

### Task 2.3: Implement MediaProxy (Cloudinary)

**Status**: not_started
**Dependencies**: Task 1.5, Task 1.6
**Priority**: High
**Effort**: 2.5 hours

#### Description
Implement MediaProxy class for Cloudinary media management. Support file upload with transformations, URL generation for different contexts (thumbnail, display, original), bulk operations with progress tracking, and atomic deletion with local reference updates.

#### Acceptance Criteria
- [x] `MediaProxy` class created extending `ServiceProxy<CloudinaryUploadResponse>`
- [x] `UploadAsync` method uploads files with configurable transformations (resize, format, optimization)
- [x] `GenerateUrlAsync` method produces optimized URLs for different use cases
- [x] `DeleteAsync` method removes files from Cloudinary and updates local references
- [x] `BulkUploadAsync` method handles multiple file uploads with progress callback
- [x] Upload failures roll back file references in database atomically
- [x] Retry logic handles transient upload failures
- [x] File size validation (max 100MB) before upload
- [x] All operations logged with file name, size, and public ID

#### Implementation Notes
- Use MultipartFormDataContent for file uploads
- Apply eager transformations for common sizes (thumbnail, display)
- Public ID should include tenant folder path for organization
- Support various image formats and auto-format selection
- Cache generated URLs briefly to avoid repeated transformations

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Proxies/MediaProxy.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/Models/CloudinaryUploadResponse.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/Models/CloudinaryDeleteResponse.cs`

#### Related Requirements
- Requirement 2.4: Cloudinary Media Service Proxy
- Requirement 2.6: Proxy Error Handling & Recovery

---

### Task 2.4: Implement NotificationProxy (Brevo)

**Status**: not_started
**Dependencies**: Task 1.5, Task 1.6
**Priority**: High
**Effort**: 2 hours

#### Description
Implement NotificationProxy class for Brevo email and SMS notification service. Support transactional emails using templates, SMS sending, delivery status tracking (sent, delivered, bounced, opened, clicked), and respect for unsubscribe preferences.

#### Acceptance Criteria
- [x] `NotificationProxy` class created extending `ServiceProxy<BrevoSendResponse>`
- [x] `SendEmailAsync` method sends transactional emails using Brevo templates
- [x] `SendSmsAsync` method sends SMS messages
- [x] `TrackDeliveryStatusAsync` method queries email delivery status
- [x] Email validation before sending (format, DNS)
- [x] Bounce handling and unsubscribe list management
- [x] Retry logic with exponential backoff for failed sends
- [ ] Circuit breaker prevents repeated calls during outages
- [x] Template parameters validated against template schema
- [x] All sends logged with recipient, template ID, and delivery status

#### Implementation Notes
- Use template IDs instead of HTML bodies
- Validate email addresses against RFC standards
- Support custom headers and metadata for tracking
- Handle bounce and complaint webhooks asynchronously
- Implement delivery status callback for async tracking

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Proxies/NotificationProxy.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/Models/SendEmailRequest.cs`
- NEW: `src/KromicStore.Infrastructure/Proxies/Models/BrevoSendResponse.cs`

#### Related Requirements
- Requirement 2.5: Brevo Email Notification Proxy
- Requirement 2.6: Proxy Error Handling & Recovery

---

### Task 2.5: Create Proxy Configuration and HttpClient Setup

**Status**: not_started
**Dependencies**: Tasks 2.1-2.4
**Priority**: High
**Effort**: 1.5 hours

#### Description
Configure HttpClient factories for all proxies with proper timeout, retry, and circuit breaker settings. Set up configuration classes to manage API endpoints, credentials, and timeouts. Create dependency injection setup for all proxy instances.

#### Acceptance Criteria
- [x] `ServiceProxyConfiguration` class holds timeout, retry count, circuit breaker threshold settings
- [x] HttpClient factories registered for each proxy with appropriate handlers
- [x] API endpoints, credentials loaded from appsettings.json with validation
- [x] Sensitive credentials encrypted in configuration storage
- [x] Circuit breaker instances registered as singletons (one per service)
- [x] HttpClient configured with default headers where needed (User-Agent, etc.)
- [x] Connection timeout set to 30 seconds by default
- [x] Request timeout set to 30 seconds by default
- [x] Proxy instances registered in DI container

#### Implementation Notes
- Use IHttpClientFactory for proper resource management
- Create configuration section in appsettings.json: `ExternalServices`
- Load credentials from environment variables or secrets manager in production
- Include message handlers for logging, retry policies, circuit breaker
- Document required configuration keys and environment variables

#### Files to Create/Modify
- NEW: `src/KromicStore.Infrastructure/Configuration/ServiceProxyConfiguration.cs`
- NEW: `src/KromicStore.API/appsettings.json` (update with ExternalServices section)
- MODIFY: `src/KromicStore.API/Program.cs` (add proxy registration)

#### Related Requirements
- All Proxy Requirements (2.1-2.6)


---

## WAVE 3: Webhook System (6 tasks, parallel after Wave 2)

### Task 3.1: Create Webhook Domain Entities

**Status**: not_started
**Dependencies**: Task 1.3
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create database entities for webhook management: WebhookConfiguration, WebhookEventLog, and WebhookDeliveryLog. These entities store webhook registrations, event history for audit/replay, and delivery attempts with retry tracking.

#### Acceptance Criteria
- [x] `WebhookConfiguration` entity with TenantId, EndpointUrl, EventTypes[], Secret, IsActive, AuthenticationHeader properties
- [x] `WebhookEventLog` entity with TenantId, EventId, EventType, Payload, OccurredAt, IdempotencyKey
- [x] `WebhookDeliveryLog` entity with WebhookConfigId, EventLogId, HttpStatusCode, Response, RetryCount, NextRetryAt
- [x] All entities inherit from BaseEntity (Id, CreatedAt, UpdatedAt)
- [x] WebhookConfiguration.Secret generated securely (64 bytes, Base64 encoded)
- [x] WebhookDeliveryLog includes static field for retry delays: [1s, 10s, 100s, 1000s, 10000s]
- [x] CalculateNextRetry() method on WebhookDeliveryLog returns DateTime for next attempt
- [x] Foreign key relationships properly configured
- [~] Entities added to AppDbContext DbSets

#### Implementation Notes
- EventTypes stored as array/collection of enum values
- Secret never returned in API responses
- Payload stored as JSON string for flexibility
- IdempotencyKey prevents duplicate event processing
- RetryCount incremented on each failed delivery attempt

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Data/Entities/WebhookConfiguration.cs`
- NEW: `src/KromicStore.Infrastructure/Data/Entities/WebhookEventLog.cs`
- NEW: `src/KromicStore.Infrastructure/Data/Entities/WebhookDeliveryLog.cs`
- MODIFY: `src/KromicStore.Infrastructure/Data/AppDbContext.cs` (add DbSets)

#### Related Requirements
- Requirement 3.1: Webhook Configuration & Management
- Requirement 3.3: Webhook Delivery with Retry
- Requirement 3.5: Webhook Event Log

---

### Task 3.2: Create WebhookEventType Enum and WebhookEvent Model

**Status**: not_started
**Dependencies**: None
**Priority**: High
**Effort**: 1 hour

#### Description
Define WebhookEventType enum containing all supported webhook events (OrderCreated, OrderStatusChanged, PaymentProcessed, etc.). Create WebhookEvent model that represents event payload sent to external systems with EventId, Timestamp, TenantId, IdempotencyKey, and Payload.

#### Acceptance Criteria
- [x] `WebhookEventType` enum defined with values: OrderCreated, OrderStatusChanged, OrderCancelled, PaymentProcessed, PaymentFailed, TenantCreated, SubscriptionChanged, SubscriptionCancelled, ProductPublished, ProductUnpublished, CustomerCreated
- [x] `WebhookEvent` model class with properties: EventId, EventType, Timestamp, TenantId, IdempotencyKey, Payload, ApiVersion
- [x] Enum supports string conversion for serialization
- [x] WebhookEvent is JSON serializable
- [x] Each event type has associated enum value
- [x] ApiVersion set to 1 for versioning support
- [x] XML documentation explains each event type purpose

#### Implementation Notes
- Enum located in `Domain/Enums/WebhookEventType.cs`
- WebhookEvent model located in `Application/Models/WebhookEvent.cs`
- Payload is object type for flexibility (any event-specific data)
- IdempotencyKey used for deduplication by webhook consumers
- Consider future versioning strategy for event payloads

#### Files to Create
- NEW: `src/KromicStore.Domain/Enums/WebhookEventType.cs`
- NEW: `src/KromicStore.Application/Models/WebhookEvent.cs`

#### Related Requirements
- Requirement 3.2: Event Type Definition

---

### Task 3.3: Implement IWebhookService and WebhookService

**Status**: not_started
**Dependencies**: Tasks 3.1, 3.2
**Priority**: High
**Effort**: 2.5 hours

#### Description
Implement IWebhookService interface and WebhookService class for webhook management. Support registering webhooks, publishing events, replaying events, and listing webhooks. Service validates endpoints are reachable before registration and queues delivery jobs.

#### Acceptance Criteria
- [x] `IWebhookService` interface defined with methods: RegisterWebhookAsync, PublishEventAsync, RetryDeliveryAsync, ListWebhooksAsync, UnregisterWebhookAsync
- [x] `RegisterWebhookAsync` validates endpoint is reachable (HEAD request with 5s timeout) before saving
- [x] `PublishEventAsync` creates event log entry and queues delivery job for each matching webhook
- [x] Event matching based on webhook's EventTypes subscription
- [x] Delivery jobs queued in Hangfire background job service
- [x] `RetryDeliveryAsync` replays event with original payload and new IdempotencyKey
- [x] Service uses IUnitOfWork for data persistence
- [x] Service uses IBackgroundJobClient for job queueing
- [x] Comprehensive logging of all operations
- [x] Unit tests verify event routing and job queueing

#### Implementation Notes
- Located in `Infrastructure/Services/WebhookService.cs`
- Endpoint validation uses HEAD request (falls back to GET)
- Event payload serialized to JSON before storage
- Multiple webhooks per event type supported
- Tenants can only access their own webhooks

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Services/Webhooks/IWebhookService.cs`
- NEW: `src/KromicStore.Infrastructure/Services/Webhooks/WebhookService.cs`
- MODIFY: `src/KromicStore.Application/Interfaces/IUnitOfWork.cs` (add webhook repositories)

#### Related Requirements
- Requirement 3.1: Webhook Configuration & Management
- Requirement 3.5: Webhook Event Log

---

### Task 3.4: Implement WebhookDeliveryJob (Hangfire)

**Status**: not_started
**Dependencies**: Task 3.3
**Priority**: High
**Effort**: 2.5 hours

#### Description
Implement WebhookDeliveryJob background job class for Hangfire. Job sends webhook payloads to configured endpoints with HMAC-SHA256 signature verification headers, retry logic, and comprehensive logging. Implements retry delays: 1s, 10s, 100s, 1000s.

#### Acceptance Criteria
- [x] `WebhookDeliveryJob` class implements background job execution
- [x] Sends webhook payload via HTTP POST to configured endpoint
- [x] Generates HMAC-SHA256 signature using webhook secret and payload
- [x] Includes signature in `X-KromicStore-Signature` header
- [x] Includes timestamp in `X-KromicStore-Timestamp` header (ISO 8601 format)
- [x] Includes event type in `X-KromicStore-Event` header
- [x] Supports optional custom authorization header from webhook config
- [x] HTTP timeout set to 30 seconds
- [x] Successful responses (2xx status) marked as delivered
- [x] Failed/timeout responses queued for retry with exponential backoff
- [x] Max retry attempts: 5 (1s, 10s, 100s, 1000s, 10000s delays)
- [x] Delivery logs created with status, response, and retry count
- [x] Failed deliveries after max retries marked as failed
- [x] All operations logged with endpoint, payload size, and result

#### Implementation Notes
- Located in `Infrastructure/BackgroundJobs/WebhookDeliveryJob.cs`
- Signature format: `sha256=<hex-encoded-hmac>`
- Support both 2xx status codes and specific success status (configurable)
- Log response body for failed deliveries (truncate to 1000 chars)
- Hang fire should be configured to retry automatically on exception

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/BackgroundJobs/WebhookDeliveryJob.cs`
- MODIFY: `src/KromicStore.API/Program.cs` (register background job)

#### Related Requirements
- Requirement 3.3: Webhook Delivery with Retry
- Requirement 3.4: Webhook Signature Verification

---

### Task 3.5: Create Webhook Controller Endpoints

**Status**: not_started
**Dependencies**: Tasks 3.3, 3.4
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create WebhookController with REST endpoints for webhook management. Support CRUD operations for webhook configurations, event replay, and delivery log retrieval. Endpoints accessible only to TenantAdmin role.

#### Acceptance Criteria
- [x] `WebhookController` extends `BaseController` with appropriate authorization
- [x] `POST /api/v1/webhooks` registers new webhook (TenantAdmin)
- [x] `GET /api/v1/webhooks` lists tenant's webhooks with pagination
- [x] `GET /api/v1/webhooks/{id}` retrieves webhook details
- [x] `PUT /api/v1/webhooks/{id}` updates webhook configuration
- [x] `DELETE /api/v1/webhooks/{id}` unregisters webhook
- [x] `POST /api/v1/webhooks/{id}/test` sends test event to endpoint
- [x] `POST /api/v1/webhooks/events/{eventId}/replay` replays event delivery
- [x] `GET /api/v1/webhooks/{id}/deliveries` lists delivery logs for webhook
- [~] All endpoints return WebhookConfigurationResponse or WebhookDeliveryLogResponse DTOs
- [x] Proper error handling with meaningful error codes
- [x] Request/response validation using FluentValidation

#### Implementation Notes
- Endpoints filtered by TenantId from context
- Webhook registration returns Secret (only once) for consumer to store
- Test event uses synthetic payload to verify endpoint is working
- Replay endpoint re-queues original event with new IdempotencyKey
- Delivery log query supports filtering by date range and status

#### Files to Create
- NEW: `src/KromicStore.API/Controllers/WebhookController.cs`
- NEW: `src/KromicStore.Contracts/V1/Webhooks/WebhookConfigurationRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Webhooks/WebhookConfigurationResponse.cs`
- NEW: `src/KromicStore.Contracts/V1/Webhooks/WebhookDeliveryLogResponse.cs`

#### Related Requirements
- Requirement 3.1: Webhook Configuration & Management

---

### Task 3.6: Implement Signature Verification and Consumer Guide

**Status**: not_started
**Dependencies**: Task 3.4
**Priority**: Medium
**Effort**: 1.5 hours

#### Description
Implement helper class for webhook consumers to verify signatures and timestamps. Create comprehensive documentation explaining webhook payload format, signature algorithm (HMAC-SHA256), and implementation examples in multiple languages.

#### Acceptance Criteria
- [~] `WebhookSignatureValidator` class with static `VerifySignature()` method
- [~] Signature verification validates HMAC-SHA256 hash matches X-KromicStore-Signature
- [~] Timestamp validation checks X-KromicStore-Timestamp is within 5-minute window
- [~] Prevents replay attacks by enforcing timestamp window
- [~] Unit tests verify signature validation with valid/invalid signatures
- [~] Documentation explains webhook payload structure
- [~] Documentation provides algorithm description and examples
- [~] Documentation includes sample C# implementation
- [~] Documentation includes cURL example for testing

#### Implementation Notes
- Located in `Application/WebhookSignatureValidator.cs`
- Signature format in header: `sha256=<hex-encoded-value>`
- Timestamp format: ISO 8601 (DateTime.UtcNow.ToString("O"))
- Consumer should replay exact request body (don't parse/re-serialize)
- Documentation in markdown format

#### Files to Create
- NEW: `src/KromicStore.Application/Webhooks/WebhookSignatureValidator.cs`
- NEW: `docs/Webhook-Consumer-Guide.md`

#### Related Requirements
- Requirement 3.4: Webhook Signature Verification


---

## WAVE 4: Configuration Management (5 tasks, parallel after Wave 1)

### Task 4.1: Create TenantConfiguration and ConfigurationAuditLog Entities

**Status**: not_started
**Dependencies**: Task 1.3
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create database entities for configuration management. TenantConfiguration stores tenant-specific settings (key-value pairs), while ConfigurationAuditLog maintains audit trail of all configuration changes with who, what, when, and previous values.

#### Acceptance Criteria
- [x] `TenantConfiguration` entity with TenantId, ConfigKey, ConfigValue, Scope, IsEncrypted, ExpiresAt
- [~] `ConfigurationAuditLog` entity with TenantId, ConfigurationKey, OldValue, NewValue, ChangedBy (UserId), ChangedAt, Reason
- [~] Both entities inherit from BaseEntity
- [~] ConfigValue stored as JSON string (serialized from object)
- [~] IsEncrypted flag indicates if value should be encrypted at rest
- [~] ExpiresAt allows temporary configuration overrides
- [~] Scope enum: Platform (SuperUser only), Tenant (TenantAdmin only)
- [~] Factory methods: TenantConfiguration.Create(), ConfigurationAuditLog.Create()
- [ ] Entities added to AppDbContext DbSets
- [~] Indexes created on (TenantId, ConfigKey) and (TenantId, ChangedAt)

#### Implementation Notes
- ConfigKey follows dot notation: "notifications:email_enabled", "webhooks:max_retry_count"
- ConfigValue stored as JSON for flexibility (supports strings, numbers, booleans, objects)
- Encrypted values decrypted on read using IEncryptionService
- Audit log supports querying change history by date range
- ConfigurationAuditLog retention: minimum 365 days

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Data/Entities/TenantConfiguration.cs`
- NEW: `src/KromicStore.Infrastructure/Data/Entities/ConfigurationAuditLog.cs`
- NEW: `src/KromicStore.Domain/Enums/ConfigScope.cs`
- MODIFY: `src/KromicStore.Infrastructure/Data/AppDbContext.cs` (add DbSets and indexes)

#### Related Requirements
- Requirement 4.1: Extended Configuration Schema
- Requirement 4.4: Configuration Audit Trail

---

### Task 4.2: Implement IConfigurationService and ConfigurationService

**Status**: not_started
**Dependencies**: Task 4.1
**Priority**: High
**Effort**: 2.5 hours

#### Description
Implement IConfigurationService interface and ConfigurationService class for runtime configuration management. Support get/set operations with caching, section queries, cache invalidation, and audit log retrieval.

#### Acceptance Criteria
- [~] `IConfigurationService` interface with methods: GetAsync<T>, SetAsync<T>, GetSectionAsync, InvalidateCacheAsync, GetAuditLogAsync
- [~] `GetAsync<T>` returns configuration value (cached, then database, then appsettings fallback)
- [~] Cache TTL: 30 minutes for most configurations
- [~] `SetAsync<T>` persists configuration, creates audit log, invalidates cache
- [~] `GetSectionAsync` returns all configs matching prefix (e.g., "notifications:*")
- [~] `InvalidateCacheAsync` removes cache entry and propagates invalidation
- [~] `GetAuditLogAsync` queries audit log with filtering by date range and key
- [~] Encrypted values decrypted transparently on read
- [~] Configuration validation (required fields, format checks)
- [~] Unit tests verify caching and fallback behavior

#### Implementation Notes
- Located in `Infrastructure/Services/ConfigurationService.cs`
- Cache keys: `{TenantId}:config:{key}`
- DefaultValue returned if configuration not found anywhere
- Encryption handled via injected IEncryptionService
- Supports per-tenant and platform-wide configurations
- Change notifications can be published via domain events

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Services/ConfigurationService.cs`
- MODIFY: `src/KromicStore.Application/Interfaces/IConfigurationService.cs` (if exists)

#### Related Requirements
- Requirement 4.1: Extended Configuration Schema
- Requirement 4.5: Runtime Configuration Updates

---

### Task 4.3: Create SuperUser Configuration Dashboard Controller

**Status**: not_started
**Dependencies**: Task 4.2
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create AdminConfigController with endpoints for SuperUser to manage platform-wide configuration. Support reading all configuration sections, updating settings, and viewing configuration audit trail.

#### Acceptance Criteria
- [~] `AdminConfigController` extends `BaseController` with SuperUser authorization
- [~] `GET /api/v1/admin/config` returns all platform configuration sections
- [~] `GET /api/v1/admin/config/{key}` retrieves specific configuration value
- [~] `PUT /api/v1/admin/config/{key}` updates configuration with validation
- [~] `GET /api/v1/admin/config/audit-logs` returns audit trail with filtering
- [~] Configuration changes apply immediately where supported (feature flags, cache settings)
- [~] Configuration validation (type checking, range validation, format validation)
- [~] Changes logged to audit trail with SuperUser ID and optional reason
- [~] Notifications sent to SuperUsers when critical configs change
- [~] Proper error handling with validation error details

#### Implementation Notes
- Configuration sections: ExternalServices, FeatureFlags, Webhooks, Performance, Security, Notifications
- Critical configurations requiring restart listed in response
- Audit trail exported as CSV for compliance
- SuperUser role verification in authorization policy
- Test configuration endpoint without applying changes (dry-run mode)

#### Files to Create
- NEW: `src/KromicStore.API/Controllers/AdminConfigController.cs`
- NEW: `src/KromicStore.Contracts/V1/Configuration/SystemConfigurationResponse.cs`
- NEW: `src/KromicStore.Contracts/V1/Configuration/ConfigurationUpdateRequest.cs`

#### Related Requirements
- Requirement 4.2: SuperUser Admin Dashboard

---

### Task 4.4: Create TenantAdmin Configuration Dashboard Controller

**Status**: not_started
**Dependencies**: Task 4.2
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create ConfigController with endpoints for TenantAdmin to manage tenant-specific configuration. Support reading tenant settings, updating settings within constraints, and viewing change history.

#### Acceptance Criteria
- [~] `ConfigController` extends `BaseController` with TenantAdmin authorization
- [~] `GET /api/v1/config` returns tenant-specific configurations
- [~] `GET /api/v1/config/{key}` retrieves specific configuration value
- [~] `PUT /api/v1/config/{key}` updates configuration with SuperUser policy constraints
- [~] `GET /api/v1/config/audit-logs` returns tenant's configuration change history
- [~] TenantAdmin cannot access platform-wide configurations
- [~] Configuration changes validated against SuperUser policies
- [~] Changes logged to audit trail with TenantAdmin ID
- [~] Reset to defaults available for each setting
- [ ] Proper error handling with validation error details

#### Implementation Notes
- Configuration sections: Notifications, Webhooks, Feature Preferences
- Tenant configurations stored separately from platform defaults
- Reset operation creates audit log entry referencing original change
- Audit log queryable by date range, key, and operation type
- Changes take effect immediately for read-only operations

#### Files to Create
- NEW: `src/KromicStore.API/Controllers/ConfigController.cs`
- NEW: `src/KromicStore.Contracts/V1/Configuration/TenantConfigurationResponse.cs`
- NEW: `src/KromicStore.Contracts/V1/Configuration/TenantConfigurationRequest.cs`

#### Related Requirements
- Requirement 4.3: TenantAdmin Dashboard

---

### Task 4.5: Implement Configuration Audit Logging and Retrieval

**Status**: not_started
**Dependencies**: Task 4.2
**Priority**: Medium
**Effort**: 1.5 hours

#### Description
Implement comprehensive audit logging for configuration changes with queryable history, change comparison, and export functionality. Support filtering by user, date range, configuration key, and change type.

#### Acceptance Criteria
- [~] Audit log entry created for every configuration change
- [~] Audit log includes: ConfigKey, OldValue, NewValue, ChangedBy, ChangedAt, Reason
- [~] Query method: GetAuditLogAsync(tenantId, from?, to?, configKey?, userId?)
- [~] Results include computed fields: ChangedByName, OldValueSummary, NewValueSummary
- [~] Sensitive values (passwords, tokens) masked in audit log display
- [~] Export method returns audit log as CSV with headers
- [~] Pagination support for large audit logs
- [~] Sorting by ChangedAt (ascending/descending)
- [~] 365-day retention policy enforced (old records archived or deleted)
- [~] Audit log queryable via API endpoint

#### Implementation Notes
- Old/New values stored as JSON strings (formatted for readability)
- Sensitive value detection based on ConfigKey pattern (contains "password", "token", "secret", "key")
- Export includes all fields except ChangedBy password
- Archived records stored separately or marked with retention date
- Consider read-only audit log storage for compliance

#### Files to Create/Modify
- MODIFY: `src/KromicStore.Infrastructure/Services/ConfigurationService.cs` (add audit log methods)
- NEW: `src/KromicStore.Contracts/V1/Configuration/ConfigurationAuditLogResponse.cs`

#### Related Requirements
- Requirement 4.4: Configuration Audit Trail


---

## WAVE 5: Performance Optimization (5 tasks, sequential)

### Task 5.1: Create Database Indexes (AppDbContext Fluent API)

**Status**: not_started
**Dependencies**: Task 1.3
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create comprehensive database indexes via Fluent API in AppDbContext to optimize query performance. Indexes target common query patterns: tenant filtering, status-based filtering, email lookups, and date range queries.

#### Acceptance Criteria
- [~] Composite index on (TenantId, Id) for all tenant-scoped tables
- [x] Composite index on (TenantId, Status) for Product, Order, Payment tables
- [~] Unique index on (TenantId, Email) for User, Customer tables
- [~] Index on CreatedAt and UpdatedAt for time-based queries
- [~] Foreign key indexes on all relationships (ProductId, CustomerId, etc.)
- [~] Partial indexes on active entities where applicable (ProductStatus = Published)
- [~] Full-text search index on Product (Name, Description) for PostgreSQL
- [~] Index naming follows convention: IX_{TableName}_{Columns}_{Type}
- [~] No redundant indexes
- [~] Database can be recreated with migrations

#### Implementation Notes
- Located in `AppDbContext.OnModelCreating()` method
- Use HasIndex() fluent API for creation
- For PostgreSQL, use GinIndex or GistIndex for full-text search
- Consider partial indexes (filtered) to reduce index size
- Document index purpose and query pattern

#### Files to Modify
- MODIFY: `src/KromicStore.Infrastructure/Data/AppDbContext.cs` (add index configurations)
- MODIFY: `src/KromicStore.Infrastructure/Data/Migrations/` (create migration)

#### Related Requirements
- Requirement 5.1: Database Indexing Strategy

---

### Task 5.2: Implement Query Optimization Patterns in Repositories

**Status**: not_started
**Dependencies**: Task 5.1
**Priority**: High
**Effort**: 2 hours

#### Description
Implement query optimization patterns in repository classes: projection (Select) to fetch only needed columns, pagination with enforced limits, explicit joins instead of lazy loading, and full-text search for text queries.

#### Acceptance Criteria
- [~] Repository queries use Select() projections to reduce data transfer
- [~] Pagination implemented with Skip/Take and enforced max page size (100 items)
- [~] All queries include include TenantId filter
- [~] Related entities loaded via Include() instead of lazy loading
- [~] Queries over 500ms logged with execution time and SQL
- [~] Full-text search implemented for product/customer searches
- [~] IQueryable patterns used to allow composition in services
- [~] Query optimization methods documented with usage examples
- [~] Performance tests verify query times for common operations

#### Implementation Notes
- Add extension methods: AsProject<T>, WithPagination(), WithTiming()
- Use EF.Functions for database-specific operations (full-text search on PostgreSQL)
- Log slow queries via EF Core logging
- Consider query result caching for common searches
- Document N+1 query antipatterns and solutions

#### Files to Modify
- MODIFY: `src/KromicStore.Infrastructure/Data/Repositories/Repository.cs` (add methods)
- CREATE: Extension methods for query optimization if needed

#### Related Requirements
- Requirement 5.2: Query Optimization

---

### Task 5.3: Implement Redis Caching Strategy and CacheService Enhancements

**Status**: not_started
**Dependencies**: Task 1.3
**Priority**: High
**Effort**: 2.5 hours

#### Description
Enhance CacheService to implement Redis caching with consistent key schemes, TTL management, and cache tag support for bulk invalidation. Define caching strategy for different entity types with appropriate TTLs.

#### Acceptance Criteria
- [~] `CacheKeys` static class defines cache key schemes with tenant isolation
- [~] Cache key format: `{Prefix}:{TenantId}:{EntityType}:{EntityId}` for singles
- [~] Cache key format: `{Prefix}:{TenantId}:{EntityType}:list` for collections
- [~] Cache TTL strategy defined: Products (1h), Customers (1h), Orders (5m), Config (30m), Roles (15m)
- [~] CacheService supports SetAsync<T>, GetAsync<T>, RemoveAsync, RemoveByPatternAsync
- [~] Pattern-based cache removal for bulk invalidation
- [~] Cache eviction policies configured (LRU, TTL)
- [~] Distributed cache tags for related entities (clear product + category caches together)
- [~] Cache hit/miss statistics available for monitoring
- [~] Unit tests verify cache behavior and TTL expiration

#### Implementation Notes
- Located in `Infrastructure/Services/CacheService.cs`
- Use IDistributedCache (StackExchange.Redis)
- Prefix all keys with tenant ID to ensure isolation
- Pattern matching for related entity groups (e.g., all product variants)
- Implement cache warming for critical data on startup
- Add health check for Redis availability

#### Files to Create/Modify
- NEW: `src/KromicStore.Infrastructure/Services/Caching/CacheKeys.cs`
- NEW: `src/KromicStore.Infrastructure/Services/Caching/CacheTTL.cs`
- MODIFY: `src/KromicStore.Infrastructure/Services/CacheService.cs` (enhancements)
- MODIFY: `src/KromicStore.API/Program.cs` (configure Redis)

#### Related Requirements
- Requirement 5.3: Redis Caching Strategy

---

### Task 5.4: Create Cache Invalidation Patterns via Domain Events

**Status**: not_started
**Dependencies**: Task 5.3
**Priority**: High
**Effort**: 2 hours

#### Description
Implement automatic cache invalidation triggered by domain events. When entities change (Product updated, Order status changed, etc.), corresponding cache entries are invalidated to prevent stale data.

#### Acceptance Criteria
- [~] Domain events published when entities change (Product, Order, Customer, etc.)
- [~] Event handlers subscribe to invalidate related caches
- [~] Product changes invalidate product cache + category cache
- [~] Order changes invalidate order cache + customer order list cache
- [~] Configuration changes invalidate configuration cache across all instances
- [~] Cache invalidation asynchronous (using background jobs or events)
- [~] Failed cache invalidation logged but doesn't fail transaction
- [~] Related cache tags cleared together (products + categories)
- [~] Bulk operations efficiently invalidate multiple cache entries
- [~] Test scenarios verify cache freshness after operations

#### Implementation Notes
- Use domain events pattern (DomainEvent base class, event handlers)
- Event handlers implement ICacheInvalidationHandler
- Consider using MediatR for event publishing if already used
- Some invalidations can be deferred (near real-time vs. immediate)
- Log cache invalidation for debugging

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Services/Caching/CacheInvalidationService.cs`
- NEW: `src/KromicStore.Domain/Events/` (folder for domain events if not exists)
- MODIFY: Domain entity classes (publish events on changes)

#### Related Requirements
- Requirement 5.4: Cache Invalidation

---

### Task 5.5: Configure Connection Pooling and Hangfire Optimization

**Status**: not_started
**Dependencies**: Task 5.1
**Priority**: High
**Effort**: 1.5 hours

#### Description
Configure PostgreSQL connection pooling with appropriate min/max sizes and idle timeout. Optimize Hangfire configuration for background job processing with worker threads, retry policies, and queue management.

#### Acceptance Criteria
- [~] Connection pool MinPoolSize: 5, MaxPoolSize: 25 (configurable via appsettings)
- [~] Connection idle timeout: 5 minutes
- [~] Connection max age: 30 minutes
- [~] Connection timeout: 30 seconds
- [~] Hangfire worker threads: equal to CPU core count
- [~] Hangfire job retry: exponential backoff (1min, 10min, 1hour)
- [~] Successful jobs removed after 1 hour
- [~] Failed jobs retained for 7 days
- [~] Webhook delivery jobs in separate queue
- [~] Hangfire dashboard accessible with authentication
- [~] Connection pool and Hangfire health checks configured
- [~] Metrics endpoint for monitoring pool status

#### Implementation Notes
- Connection string includes pooling parameters: Maximum Pool Size, Minimum Pool Size
- Hangfire storage: PostgreSQL backend
- Worker count: Environment.ProcessorCount
- Separated job queues: default, webhooks, scheduled
- Dashboard requires SuperUser role to access
- Monitor metrics: active connections, available connections, queued requests

#### Files to Modify
- MODIFY: `src/KromicStore.API/appsettings.json` (connection string, Hangfire config)
- MODIFY: `src/KromicStore.API/Program.cs` (Hangfire setup)
- NEW: `src/KromicStore.Infrastructure/HealthChecks/ConnectionPoolHealthCheck.cs`

#### Related Requirements
- Requirement 5.5: Database Connection Pooling
- Requirement 5.6: Hangfire Optimization


---

## WAVE 6: Enhanced Domain Entities (4 tasks, parallel)

### Task 6.1: Create/Enhance Product and Category Entities with Business Logic

**Status**: not_started
**Dependencies**: None
**Priority**: High
**Effort**: 2 hours

#### Description
Create/enhance Product and Category aggregate entities with business rules enforcement, domain methods, and value objects. Product should enforce stock constraints, status transitions, and pricing rules. Category should support hierarchical organization.

#### Acceptance Criteria
- [x] `Product` entity with properties: Sku (unique per tenant), Name, Description, Price (Money value object), StockQuantity, ReorderLevel, CategoryId, Status
- [~] `Category` entity with properties: Name, Description, ParentCategoryId, DisplayOrder, supporting up to 3 levels of nesting
- [~] `ProductStatus` enum: Draft, Published, Archived
- [~] Product domain methods: Publish(), Unpublish(), ReduceStock(quantity), RestoreStock(quantity)
- [~] Product validation: stock >= 0, price > 0, SKU unique within tenant
- [~] Product prevents publishing with zero stock
- [~] Category factory method: Create()
- [~] Category prevents circular hierarchy (parent cannot be descendant)
- [~] Both entities use BaseEntity (Id, TenantId, CreatedAt, UpdatedAt)
- [~] Entities fully mapped in AppDbContext

#### Implementation Notes
- Money is value object with Amount (decimal) and Currency
- Stock reduction throws exception if insufficient inventory
- Status changes only via domain methods (not direct assignment)
- Category deletion unassigns products gracefully
- Indexes on (TenantId, Status) and (TenantId, ParentCategoryId)

#### Files to Create/Modify
- MODIFY: `src/KromicStore.Domain/Entities/Product.cs` (enhance if exists)
- MODIFY: `src/KromicStore.Domain/Entities/Category.cs` (create if not)
- NEW: `src/KromicStore.Domain/ValueObjects/Money.cs`
- NEW: `src/KromicStore.Domain/Enums/ProductStatus.cs`
- MODIFY: `src/KromicStore.Infrastructure/Data/AppDbContext.cs` (mappings)

#### Related Requirements
- Requirement 6.3: Product Catalog Management
- Requirement 6.4: Category Management

---

### Task 6.2: Create/Enhance Order and OrderItem Entities

**Status**: not_started
**Dependencies**: None
**Priority**: High
**Effort**: 2 hours

#### Description
Create/enhance Order aggregate with complete order lifecycle, OrderItem collection, and business logic. Support order status transitions, inventory reservation, price snapshot preservation, and customer linkage.

#### Acceptance Criteria
- [x] `Order` entity with properties: OrderNumber (human-readable), CustomerId, OrderStatus, Total (Money), Subtotal, Tax, ShippingCost, ShippingAddress, BillingAddress, ProcessedBy (UserId), ShippedAt, DeliveredAt
- [~] `OrderStatus` enum: Pending, Confirmed, Paid, Shipped, Delivered, Cancelled
- [~] Order domain methods: ConfirmOrder(), ShipOrder(), DeliverOrder(), Cancel()
- [~] Order prevents invalid status transitions (e.g., cannot deliver unshipped order)
- [~] `OrderItem` entity with properties: OrderId, ProductId, Quantity, UnitPrice (Price at order time), ProductName, ProductSku (snapshot)
- [~] OrderItem prevents invalid quantities (must be > 0)
- [~] Product price snapshot stored with order to preserve historical pricing
- [~] Total calculation includes subtotal + tax + shipping
- [~] OrderNumber format: ORD-{yyyyMMdd}-{RandomSuffix}
- [~] Both entities support BaseEntity
- [~] Navigation properties properly configured

#### Implementation Notes
- OrderNumber generated via factory method on Order.Create()
- Product price/name snapshot captured when OrderItem created
- Status transitions validated before assignment
- Indexes on (TenantId, OrderStatus), (CustomerId, CreatedAt)
- Order total validated before confirmation

#### Files to Create/Modify
- MODIFY: `src/KromicStore.Domain/Entities/Order.cs` (enhance)
- MODIFY: `src/KromicStore.Domain/Entities/OrderItem.cs` (create/enhance)
- NEW: `src/KromicStore.Domain/Enums/OrderStatus.cs`
- MODIFY: `src/KromicStore.Infrastructure/Data/AppDbContext.cs` (mappings, indexes)

#### Related Requirements
- Requirement 6.6: Basic Order Workflow

---

### Task 6.3: Create Payment, PaymentTransaction, and Subscription Entities

**Status**: not_started
**Dependencies**: None
**Priority**: High
**Effort**: 2 hours

#### Description
Create Payment aggregate for order payments, PaymentTransaction for transaction tracking, and Subscription entity for tenant subscription plans. Support payment status tracking, refunds, and subscription plan features.

#### Acceptance Criteria
- [~] `Payment` entity with properties: OrderId, Amount (Money), Status, ExternalPaymentId (Razorpay), PaymentMethod, PaidAt
- [~] `PaymentStatus` enum: Pending, Processing, Completed, Failed, Refunded
- [~] Payment domain methods: MarkAsProcessed(externalId), MarkAsFailed(reason)
- [~] `PaymentTransaction` entity with properties: PaymentId, Amount, TransactionType (Debit, Credit, Refund), Status, ExternalTransactionId, Notes
- [~] `Subscription` entity with properties: TenantId, PlanType, MonthlyPrice, StartDate, EndDate, Status, MaxUsers, MaxProducts, MaxApiCallsPerMonth, WebhooksEnabled, AnalyticsEnabled, TrialEndsAt
- [~] `SubscriptionPlan` enum: Starter, Professional, Enterprise
- [~] `SubscriptionStatus` enum: Trial, Active, Suspended, Cancelled, GracePeriod
- [~] Subscription factory: CreateTrial(tenantId, trialDays)
- [~] Feature mapping per plan (MaxUsers, MaxProducts, MaxApiCalls, Price)
- [~] All entities inherit from BaseEntity
- [~] Proper navigation properties and indexes

#### Implementation Notes
- Payment linked to Order (1:1), can have multiple transactions
- External payment ID stored for reconciliation with Razorpay
- Transactions immutable (no updates, only creates)
- Subscription trial defaults to 14 days
- Feature limits enforced at application level
- Indexes on (TenantId, Status), (OrderId)

#### Files to Create/Modify
- NEW: `src/KromicStore.Domain/Entities/Payment.cs`
- NEW: `src/KromicStore.Domain/Entities/PaymentTransaction.cs`
- NEW: `src/KromicStore.Domain/Entities/Subscription.cs`
- NEW: `src/KromicStore.Domain/Enums/PaymentStatus.cs`
- NEW: `src/KromicStore.Domain/Enums/SubscriptionPlan.cs`
- NEW: `src/KromicStore.Domain/Enums/SubscriptionStatus.cs`
- NEW: `src/KromicStore.Domain/ValueObjects/SubscriptionPlanFeatures.cs`
- MODIFY: `src/KromicStore.Infrastructure/Data/AppDbContext.cs` (mappings, indexes)

#### Related Requirements
- Requirement 6.2: Subscription Management
- Requirement 6.7: Payment Integration (Razorpay)

---

### Task 6.4: Create/Enhance Customer Entity

**Status**: not_started
**Dependencies**: None
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create/enhance Customer aggregate with profile information, order history tracking, lifetime value calculation, and GDPR-compliant data deletion support.

#### Acceptance Criteria
- [~] `Customer` entity with properties: TenantId, Email (unique per tenant), FirstName, LastName, PhoneNumber, BillingAddress, ShippingAddress, LifetimeValue (decimal), OrderCount (int), LastOrderAt, NewsletterSubscribed, VerifiedAt
- [~] `Address` value object with properties: Street, City, State, PostalCode, Country, IsDefault
- [~] Customer domain method: UpdateLifetimeValue(orderTotal)
- [~] Customer domain method: GetFullName()
- [~] Customer supports multiple addresses (billing, shipping, with default selection)
- [~] Email verification timestamp tracked (VerifiedAt)
- [~] Lifetime value and order count automatically updated on new orders
- [~] GDPR-compliant deletion: anonymize data, retain for audit trail with retention date
- [~] Entity supports BaseEntity (Id, TenantId, CreatedAt, UpdatedAt)
- [~] Indexes on (TenantId, Email), (TenantId, CreatedAt)

#### Implementation Notes
- Email unique within tenant only
- Address as value object (can be updated but not tracked separately)
- LifetimeValue in base currency (currency determined by tenant config)
- Deletion doesn't remove record (compliance), just marks as deleted with anonymization
- Newsletter subscription managed by tenant preferences
- CustomerCreated domain event published on creation

#### Files to Create/Modify
- MODIFY: `src/KromicStore.Domain/Entities/Customer.cs` (enhance)
- NEW: `src/KromicStore.Domain/ValueObjects/Address.cs`
- MODIFY: `src/KromicStore.Infrastructure/Data/AppDbContext.cs` (mappings, value object config)

#### Related Requirements
- Requirement 6.5: Customer Management


---

## WAVE 7: API Controllers & Services (8 tasks, parallel after Wave 6)

### Task 7.1: Create ProductController with CRUD + Publish/Unpublish

**Status**: not_started
**Dependencies**: Task 6.1
**Priority**: High
**Effort**: 2 hours

#### Description
Create ProductController with REST endpoints for product CRUD operations and publish/unpublish functionality. Include pagination, filtering by category, and image upload support.

#### Acceptance Criteria
- [~] `ProductController` extends `BaseController`
- [~] `GET /api/v1/products` lists products with pagination, optional category filter
- [~] `GET /api/v1/products/{id}` retrieves product details
- [~] `POST /api/v1/products` creates product (TenantAdmin+)
- [~] `PUT /api/v1/products/{id}` updates product (TenantAdmin+)
- [~] `DELETE /api/v1/products/{id}` soft-deletes product (TenantAdmin+)
- [~] `POST /api/v1/products/{id}/publish` publishes product, validates stock
- [~] `POST /api/v1/products/{id}/unpublish` unpublishes product
- [~] Request/response DTOs use Contracts project
- [~] Pagination with configurable page size (default 20, max 100)
- [~] Filter by status (draft, published, archived)
- [~] Proper error handling and validation

#### Implementation Notes
- Published products visible in list by default
- Stock quantity must be > 0 to publish
- Category filter optional (return all if not specified)
- Endpoints return ProductResponse with cached category info
- SKU must be unique within tenant
- Soft delete (mark archived, not removed)

#### Files to Create
- NEW: `src/KromicStore.API/Controllers/ProductController.cs`
- NEW: `src/KromicStore.Contracts/V1/Products/CreateProductRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Products/UpdateProductRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Products/ProductResponse.cs`
- NEW: `src/KromicStore.Contracts/V1/Products/ProductListResponse.cs`

#### Related Requirements
- Requirement 6.3: Product Catalog Management

---

### Task 7.2: Create CategoryController with CRUD

**Status**: not_started
**Dependencies**: Task 6.1
**Priority**: Medium
**Effort**: 1.5 hours

#### Description
Create CategoryController with REST endpoints for category management. Support hierarchical organization, reordering, and bulk operations.

#### Acceptance Criteria
- [~] `CategoryController` extends `BaseController`
- [~] `GET /api/v1/categories` lists categories with hierarchy
- [~] `GET /api/v1/categories/{id}` retrieves category details with subcategories
- [~] `POST /api/v1/categories` creates category (TenantAdmin+)
- [~] `PUT /api/v1/categories/{id}` updates category
- [~] `DELETE /api/v1/categories/{id}` deletes category, unassigns products
- [~] `POST /api/v1/categories/{id}/reorder` sets display order
- [~] Parent category validation (up to 3 levels)
- [~] Circular reference prevention
- [~] Response includes subcategories and product count
- [~] Proper error handling

#### Implementation Notes
- Categories returned as tree structure (nested subcategories)
- Reorder operation takes DisplayOrder value
- Deletion orphans products (unassign category)
- Parent category validated to exist and belong to same tenant
- DisplayOrder used for UI sorting

#### Files to Create
- NEW: `src/KromicStore.API/Controllers/CategoryController.cs`
- NEW: `src/KromicStore.Contracts/V1/Products/CreateCategoryRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Products/UpdateCategoryRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Products/CategoryResponse.cs`

#### Related Requirements
- Requirement 6.4: Category Management

---

### Task 7.3: Create OrderController with Full Workflow

**Status**: not_started
**Dependencies**: Task 6.2
**Priority**: Critical
**Effort**: 2.5 hours

#### Description
Create OrderController with endpoints for complete order lifecycle: creation, status transitions, cancellation, and detail retrieval. Support filtering by status and customer.

#### Acceptance Criteria
- [~] `OrderController` extends `BaseController`
- [~] `GET /api/v1/orders` lists orders with pagination, status/customer filter
- [~] `GET /api/v1/orders/{id}` retrieves order with items and payment status
- [~] `POST /api/v1/orders` creates order, validates inventory, reserves stock
- [~] `PUT /api/v1/orders/{id}` updates order (address, items) if pending
- [~] `POST /api/v1/orders/{id}/confirm` confirms order, updates inventory
- [~] `POST /api/v1/orders/{id}/ship` marks as shipped
- [~] `POST /api/v1/orders/{id}/deliver` marks as delivered
- [~] `POST /api/v1/orders/{id}/cancel` cancels order, releases inventory
- [~] Inventory validation: sufficient stock for all items
- [~] Product price snapshot preserved with order
- [~] Customer linked to order automatically
- [~] Response includes order items with product details
- [~] Proper error handling (insufficient stock, invalid transitions)

#### Implementation Notes
- Order creation validates customer exists
- Inventory reservation atomic with order creation
- Stock release on cancellation
- Inventory validation before status transitions
- Order items cannot be modified after confirmation
- Customer last order date updated on creation

#### Files to Create
- NEW: `src/KromicStore.API/Controllers/OrderController.cs`
- NEW: `src/KromicStore.Contracts/V1/Orders/CreateOrderRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Orders/OrderItemRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Orders/OrderResponse.cs`
- NEW: `src/KromicStore.Contracts/V1/Orders/OrderDetailResponse.cs`

#### Related Requirements
- Requirement 6.6: Basic Order Workflow

---

### Task 7.4: Create CustomerController with CRUD

**Status**: not_started
**Dependencies**: Task 6.4
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create CustomerController with endpoints for customer management. Support profile CRUD, order history, address management, and GDPR-compliant deletion.

#### Acceptance Criteria
- [~] `CustomerController` extends `BaseController`
- [~] `GET /api/v1/customers` lists customers with pagination
- [~] `GET /api/v1/customers/{id}` retrieves customer profile
- [~] `POST /api/v1/customers` creates customer (customer can self-register or admin create)
- [~] `PUT /api/v1/customers/{id}` updates customer profile
- [~] `DELETE /api/v1/customers/{id}` GDPR deletion (anonymize data)
- [~] `GET /api/v1/customers/{id}/orders` lists customer's orders with pagination
- [~] `POST /api/v1/customers/{id}/addresses` adds/updates address
- [~] Email unique within tenant
- [~] Response includes customer lifetime value and order count
- [ ] Proper error handling

#### Implementation Notes
- Email validation (format and uniqueness)
- GDPR deletion: replace name/email with anonymous values, keep for audit
- Customer can update own profile
- Admin can update any customer profile
- Addresses include billing/shipping flags
- Last order date auto-updated from orders

#### Files to Create
- NEW: `src/KromicStore.API/Controllers/CustomerController.cs`
- NEW: `src/KromicStore.Contracts/V1/Customers/CreateCustomerRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Customers/UpdateCustomerRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Customers/CustomerResponse.cs`
- NEW: `src/KromicStore.Contracts/V1/Customers/AddressRequest.cs`

#### Related Requirements
- Requirement 6.5: Customer Management

---

### Task 7.5: Create PaymentController with Razorpay Integration

**Status**: not_started
**Dependencies**: Task 2.1, Task 6.3
**Priority**: Critical
**Effort**: 2 hours

#### Description
Create PaymentController with endpoints to initiate payments, verify payment status, and handle refunds. Integration with PaymentProxy for Razorpay operations.

#### Acceptance Criteria
- [~] `PaymentController` extends `BaseController`
- [~] `POST /api/v1/payments/create` initiates payment for order via Razorpay
- [~] `GET /api/v1/payments/{id}/status` verifies payment status
- [~] `POST /api/v1/payments/{id}/refund` requests refund
- [~] Payment validation: order exists, amount matches order total
- [~] Razorpay order ID returned for frontend integration
- [~] Idempotency keys prevent duplicate payments
- [~] Payment status transitions tracked (Pending → Processing → Completed/Failed)
- [~] Webhook handler verifies Razorpay signature
- [~] Order status updated to Confirmed on successful payment
- [~] Proper error handling with Razorpay error mapping

#### Implementation Notes
- PaymentProxy injected for Razorpay calls
- Idempotency key generated from order ID
- Payment status webhook received from Razorpay asynchronously
- Refund creates PaymentTransaction entry
- Response includes Razorpay order/payment ID

#### Files to Create
- NEW: `src/KromicStore.API/Controllers/PaymentController.cs`
- NEW: `src/KromicStore.Contracts/V1/Payments/CreatePaymentRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Payments/PaymentStatusResponse.cs`
- NEW: `src/KromicStore.Contracts/V1/Payments/RefundRequest.cs`

#### Related Requirements
- Requirement 6.7: Payment Integration (Razorpay)

---

### Task 7.6: Create SubscriptionController with Plan Management

**Status**: not_started
**Dependencies**: Task 6.3
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create SubscriptionController with endpoints to view current subscription, upgrade/downgrade plans, and manage cancellation. Handle pro-rata billing for mid-cycle changes.

#### Acceptance Criteria
- [~] `SubscriptionController` extends `BaseController` (TenantAdmin+)
- [~] `GET /api/v1/subscriptions/current` retrieves current subscription details
- [~] `GET /api/v1/subscriptions/plans` lists available plans with feature comparison
- [~] `POST /api/v1/subscriptions/upgrade` upgrades to higher plan
- [~] `POST /api/v1/subscriptions/downgrade` downgrades to lower plan
- [~] `POST /api/v1/subscriptions/cancel` requests cancellation (30-day grace period)
- [~] `POST /api/v1/subscriptions/reactivate` reactivates cancelled subscription
- [~] Plan upgrade triggers payment if pro-rata charge required
- [~] Downgrade credit applied to next billing
- [~] Feature limits reflected in response
- [~] Proper error handling (invalid transitions, payment failures)

#### Implementation Notes
- Subscription displayed per-tenant (authorized)
- Pro-rata calculation: daily rate × remaining days
- Grace period: 30 days before full deactivation
- Current plan shows usage vs. limits
- Upgrade immediate, downgrade on next cycle option
- PaymentProxy called for charge/refund

#### Files to Create
- NEW: `src/KromicStore.API/Controllers/SubscriptionController.cs`
- NEW: `src/KromicStore.Contracts/V1/Subscriptions/CurrentSubscriptionResponse.cs`
- NEW: `src/KromicStore.Contracts/V1/Subscriptions/SubscriptionPlanResponse.cs`
- NEW: `src/KromicStore.Contracts/V1/Subscriptions/UpgradeRequest.cs`

#### Related Requirements
- Requirement 6.2: Subscription Management

---

### Task 7.7: Create AuthController with Registration, Login, Refresh, OAuth

**Status**: not_started
**Dependencies**: Task 2.2
**Priority**: Critical
**Effort**: 2.5 hours

#### Description
Create AuthController with endpoints for user registration, login with credentials, token refresh, and OAuth login via Google. Generate JWT tokens with tenant info and roles.

#### Acceptance Criteria
- [~] `AuthController` without base authorization (public endpoints)
- [~] `POST /api/v1/auth/register` creates new tenant and TenantAdmin user
- [~] `POST /api/v1/auth/login` authenticates user with email/password
- [~] `POST /api/v1/auth/refresh` refreshes access token using refresh token
- [~] `POST /api/v1/auth/oauth/google` exchanges Google authorization code for account
- [~] Registration validates email uniqueness, password strength
- [~] Login returns accessToken (1 hour) and refreshToken (30 days)
- [~] JWT token includes TenantId, UserId, Roles, permissions
- [~] Google OAuth creates/links account if first-time login
- [~] Refresh token rotated on each refresh
- [~] Password hashed using bcrypt or equivalent

#### Implementation Notes
- AuthService handles authentication logic
- JWT claims: tenant_id, user_id, email, roles
- Refresh token stored in secure httpOnly cookie
- OAuth integrates with OAuthProxy for Google exchange
- Password validation: min 8 chars, uppercase, number, special char
- Account lockout after 5 failed login attempts

#### Files to Create
- NEW: `src/KromicStore.API/Controllers/AuthController.cs`
- NEW: `src/KromicStore.Contracts/V1/Auth/RegisterRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Auth/LoginRequest.cs`
- NEW: `src/KromicStore.Contracts/V1/Auth/AuthResponse.cs`
- NEW: `src/KromicStore.Contracts/V1/Auth/OAuthRequest.cs`

#### Related Requirements
- Requirement 6.1: Tenant Registration & Onboarding

---

### Task 7.8: Implement Business Logic Services

**Status**: not_started
**Dependencies**: Tasks 7.1-7.7 (parallel, controllers created)
**Priority**: High
**Effort**: 2.5 hours

#### Description
Implement application services containing business logic for domain operations: ProductService, OrderService, CustomerService, PaymentService, SubscriptionService. Services orchestrate repositories, proxies, and domain logic.

#### Acceptance Criteria
- [~] `ProductService` handles create, update, publish, unpublish operations
- [~] `OrderService` handles create, status transitions, inventory management
- [~] `CustomerService` handles create, update, profile retrieval
- [~] `PaymentService` orchestrates payment creation and verification
- [~] `SubscriptionService` handles plan changes, billing, cancellation
- [~] Services use injected repositories, proxies, caching
- [~] Services publish domain events for cache invalidation
- [~] Services include comprehensive error handling
- [~] Services log operations with relevant context
- [~] Unit tests verify business logic (inventory validation, transitions, etc.)

#### Implementation Notes
- Located in `Infrastructure/Services/` or `Application/Services/`
- Each service depends on IUnitOfWork and specific repositories
- Domain events published via event publisher or directly
- Error handling wraps repository/proxy exceptions
- Consider using MediatR for command/query separation if appropriate

#### Files to Create
- NEW: `src/KromicStore.Infrastructure/Services/ProductService.cs`
- NEW: `src/KromicStore.Infrastructure/Services/OrderService.cs`
- NEW: `src/KromicStore.Infrastructure/Services/CustomerService.cs`
- NEW: `src/KromicStore.Infrastructure/Services/PaymentService.cs`
- NEW: `src/KromicStore.Infrastructure/Services/SubscriptionService.cs`

#### Related Requirements
- All Feature 6 requirements


---

## WAVE 8: Tenant Registration & Onboarding (3 tasks, sequential after Wave 7)

### Task 8.1: Implement Tenant Registration Workflow

**Status**: not_started
**Dependencies**: Task 7.7
**Priority**: Critical
**Effort**: 2 hours

#### Description
Implement comprehensive tenant registration workflow triggered via AuthController. Create Tenant entity, initialize TenantAdmin user, generate API credentials, and set up default configuration.

#### Acceptance Criteria
- [x] `Tenant` entity created with properties: CompanyName, Email, Country, Status, CreatedAt
- [~] `User` entity enhanced with: TenantId, Email, HashedPassword, Roles, LastLoginAt
- [~] `TenantService.RegisterAsync()` method orchestrates full workflow
- [~] Registration validates all required fields (company name, email, password)
- [~] Email uniqueness checked across all tenants
- [~] TenantAdmin user created with provided credentials
- [~] Default Subscription (Trial) created for new tenant
- [~] API key pair generated for tenant (public/private)
- [~] Default configuration initialized (notifications disabled, webhooks enabled)
- [~] Transaction-like behavior: all succeed or all rollback
- [~] Verification email sent to confirm ownership
- [~] Proper error handling for duplicate emails, validation failures

#### Implementation Notes
- Tenant status: Active, Suspended, Deactivated
- API key format: "{TenantId}_{RandomString}" (use Guid + random suffix)
- API private key only returned once at registration
- Default config includes: trial period, feature flags
- Email verification link valid for 24 hours
- Registration response includes auth token (valid for 24 hours)

#### Files to Create/Modify
- NEW: `src/KromicStore.Domain/Entities/Tenant.cs`
- MODIFY: `src/KromicStore.Domain/Entities/User.cs` (add properties)
- NEW: `src/KromicStore.Infrastructure/Services/TenantService.cs`
- MODIFY: `src/KromicStore.Infrastructure/Data/AppDbContext.cs` (add Tenant DbSet)

#### Related Requirements
- Requirement 6.1: Tenant Registration & Onboarding

---

### Task 8.2: Create Default Configuration and Initialization

**Status**: not_started
**Dependencies**: Task 8.1
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create default configuration settings applied to new tenants. Seed initial configuration values into TenantConfiguration table and provide configuration reset capability.

#### Acceptance Criteria
- [x] `TenantConfigurationSeeder` creates default configs on registration
- [~] Default configs include: Notifications (enabled/disabled), Webhooks (enabled), Features (all enabled for trial)
- [~] Subscription limits enforced based on plan (MaxUsers, MaxProducts, MaxApiCalls)
- [~] Email templates assigned (Brevo template IDs)
- [~] Payment provider configured (Razorpay settings)
- [~] Currency defaults to account country (USD, EUR, INR, etc.)
- [~] Configuration reset method available (TenantAdmin only)
- [~] Audit log created for initial configs (system user)
- [~] Configuration persists correctly and loads on requests
- [~] Unit tests verify default values

#### Implementation Notes
- Seeder called during TenantService.RegisterAsync()
- Config keys follow pattern: "feature:subfeature:setting"
- Plan-based limits loaded from SubscriptionPlanFeatures
- Brevo template IDs configured in appsettings
- Timezone defaults to tenant account timezone (if selected)

#### Files to Create/Modify
- NEW: `src/KromicStore.Infrastructure/Services/TenantConfigurationSeeder.cs`
- MODIFY: `src/KromicStore.Infrastructure/Services/TenantService.cs` (call seeder)
- MODIFY: `src/KromicStore.API/appsettings.json` (default template IDs, provider configs)

#### Related Requirements
- Requirement 4.1: Extended Configuration Schema
- Requirement 6.1: Tenant Registration & Onboarding

---

### Task 8.3: Send Welcome Email via Notification Proxy

**Status**: not_started
**Dependencies**: Task 2.4, Task 8.1
**Priority**: Medium
**Effort**: 1.5 hours

#### Description
Send welcome email to new tenants via NotificationProxy using Brevo email templates. Include onboarding instructions, API documentation link, and customer support contact.

#### Acceptance Criteria
- [~] Welcome email sent immediately after successful registration
- [~] Email uses Brevo template (template ID configured)
- [~] Email includes: company name, tenant dashboard URL, API docs link
- [~] Email includes first steps guide (create categories, add products)
- [~] Support contact information provided in email
- [~] Email sent asynchronously (background job) to not block registration
- [~] Retry logic handles transient email failures
- [~] Failed email logged but doesn't fail registration
- [~] Email delivery status tracked (sent, delivered, bounced)
- [~] Unsubscribe/preference links included in template

#### Implementation Notes
- Email queued via NotificationProxy.SendEmailAsync()
- Template ID from config: "notifications:welcome_email_template_id"
- Template parameters: CompanyName, DashboardUrl, TrialEndDate
- Retry delays: 1s, 10s, 100s per NotificationProxy config
- Dashboard URL constructed from tenant subdomain/slug
- Support email from config: "notifications:support_email"

#### Files to Create/Modify
- MODIFY: `src/KromicStore.Infrastructure/Services/TenantService.cs` (queue welcome email)
- MODIFY: `src/KromicStore.API/appsettings.json` (Brevo template IDs, support email)
- MODIFY: `docs/Getting-Started.md` (update with signup flow)

#### Related Requirements
- Requirement 6.1: Tenant Registration & Onboarding
- Requirement 6.8: Email Notifications


---

## WAVE 9: Testing & Documentation (4 tasks, parallel)

### Task 9.1: Write Unit Tests for Domain Entities

**Status**: not_started
**Dependencies**: Wave 6 (Entities created)
**Priority**: High
**Effort**: 2 hours

#### Description
Write comprehensive unit tests for domain entities verifying business logic, invariants, and domain methods. Tests should validate entity creation, method behavior, and error conditions.

#### Acceptance Criteria
- [-] Unit tests for Product entity: creation, publish/unpublish, stock management
- [~] Unit tests for Order entity: status transitions, total calculation, validation
- [~] Unit tests for Category entity: hierarchy validation, circular reference prevention
- [~] Unit tests for Payment entity: status transitions, external ID handling
- [~] Unit tests for Subscription entity: plan features, trial expiry
- [~] Unit tests for Customer entity: profile updates, lifetime value calculation
- [~] Tests use xUnit or NUnit framework
- [~] Tests follow AAA pattern (Arrange, Act, Assert)
- [~] Tests verify invalid state prevention (e.g., publish with zero stock)
- [~] Tests use descriptive names explaining scenario
- [~] Code coverage minimum 80% for entity logic
- [~] Tests located in `tests/KromicStore.Tests.Unit/Domain/`

#### Implementation Notes
- Use test fixtures for common setup
- Test both happy path and error cases
- Verify domain method side effects (status changes, totals)
- Test value objects (Money, Address) separately
- Consider parametrized tests for multiple scenarios

#### Files to Create
- NEW: `tests/KromicStore.Tests.Unit/Domain/ProductTests.cs`
- NEW: `tests/KromicStore.Tests.Unit/Domain/OrderTests.cs`
- NEW: `tests/KromicStore.Tests.Unit/Domain/CategoryTests.cs`
- NEW: `tests/KromicStore.Tests.Unit/Domain/PaymentTests.cs`
- NEW: `tests/KromicStore.Tests.Unit/Domain/SubscriptionTests.cs`
- NEW: `tests/KromicStore.Tests.Unit/Domain/CustomerTests.cs`

#### Related Requirements
- All Feature 6 requirements

---

### Task 9.2: Write Integration Tests for Repositories and Services

**Status**: not_started
**Dependencies**: Wave 5, Wave 7 (Services created)
**Priority**: High
**Effort**: 2.5 hours

#### Description
Write integration tests for repositories and services validating database interactions, multi-tenancy enforcement, and service logic. Tests should use real database (test instance) or in-memory database.

#### Acceptance Criteria
- [-] Repository tests verify CRUD operations (Create, Read, Update, Delete)
- [~] Service tests verify orchestration logic (ProductService, OrderService, etc.)
- [~] Multi-tenancy enforcement: queries filtered by TenantId
- [~] Cache service tests verify get, set, invalidation
- [~] Configuration service tests verify read, write, audit logging
- [~] Webhook service tests verify event publishing, job queueing
- [~] Proxy tests mock external APIs, verify retry logic
- [~] Tests use fixture for database setup/teardown
- [~] Tests use TestHost for dependency injection
- [~] Code coverage minimum 70% for service logic
- [~] Tests located in `tests/KromicStore.Tests.Integration/`

#### Implementation Notes
- Use SQLite in-memory or PostgreSQL test instance
- Factory/Builder patterns for test data creation
- Mock external proxies (Razorpay, Google, Cloudinary)
- Verify domain events published correctly
- Test transaction behavior (rollback on error)

#### Files to Create
- NEW: `tests/KromicStore.Tests.Integration/RepositoryTests/ProductRepositoryTests.cs`
- NEW: `tests/KromicStore.Tests.Integration/ServiceTests/ProductServiceTests.cs`
- NEW: `tests/KromicStore.Tests.Integration/ServiceTests/OrderServiceTests.cs`
- NEW: `tests/KromicStore.Tests.Integration/ServiceTests/WebhookServiceTests.cs`
- NEW: `tests/KromicStore.Tests.Integration/ServiceTests/ConfigurationServiceTests.cs`
- NEW: `tests/KromicStore.Tests.Integration/Fixtures/DatabaseFixture.cs`

#### Related Requirements
- All Feature requirements

---

### Task 9.3: Write Endpoint Tests for Controllers

**Status**: not_started
**Dependencies**: Wave 7 (Controllers created)
**Priority**: High
**Effort**: 2 hours

#### Description
Write end-to-end tests for API endpoints validating request/response contracts, authentication/authorization, and error handling. Tests simulate client calls via TestHost.

#### Acceptance Criteria
- [-] Endpoint tests for ProductController (CRUD, publish/unpublish)
- [~] Endpoint tests for OrderController (full workflow)
- [~] Endpoint tests for AuthController (register, login, refresh, OAuth)
- [~] Endpoint tests for PaymentController (create, verify, refund)
- [~] Endpoint tests for WebhookController (register, list, replay)
- [~] Tests verify correct HTTP status codes (200, 400, 401, 403, 404, 409, 422)
- [~] Tests verify response DTO structure and data
- [~] Tests verify authentication (missing token, invalid token)
- [~] Tests verify authorization (user cannot access other tenant's data)
- [~] Tests verify validation error responses (field-level errors)
- [~] Tests use TestHost and in-memory database
- [~] Code coverage minimum 70% for endpoints
- [~] Tests located in `tests/KromicStore.Tests.Integration/Endpoints/`

#### Implementation Notes
- Use WebApplicationFactory<Program> for TestHost
- Create HttpClient with authentication token
- Test with different user roles (SuperUser, TenantAdmin, Customer)
- Verify multi-tenancy: user can't access other tenant data
- Test pagination, filtering, sorting parameters

#### Files to Create
- NEW: `tests/KromicStore.Tests.Integration/Endpoints/AuthControllerTests.cs`
- NEW: `tests/KromicStore.Tests.Integration/Endpoints/ProductControllerTests.cs`
- NEW: `tests/KromicStore.Tests.Integration/Endpoints/OrderControllerTests.cs`
- NEW: `tests/KromicStore.Tests.Integration/Endpoints/PaymentControllerTests.cs`
- NEW: `tests/KromicStore.Tests.Integration/Endpoints/WebhookControllerTests.cs`
- NEW: `tests/KromicStore.Tests.Integration/Fixtures/ApiTestFixture.cs`

#### Related Requirements
- All Feature requirements

---

### Task 9.4: Create API Documentation (Swagger/OpenAPI)

**Status**: not_started
**Dependencies**: Wave 7 (Endpoints created)
**Priority**: Medium
**Effort**: 1.5 hours

#### Description
Generate and enhance Swagger/OpenAPI documentation for all API endpoints. Include request/response examples, authentication scheme, error codes, and usage guides.

#### Acceptance Criteria
- [-] Swagger UI accessible at `/swagger`
- [~] All endpoints documented with summary and description
- [~] Request/response DTOs documented with XML comments
- [~] Authentication scheme (Bearer JWT) documented
- [~] Error responses documented (400, 401, 403, 404, 422, 500)
- [~] Request examples for complex DTOs (CreateOrderRequest, etc.)
- [~] Response examples for important endpoints
- [~] API versioning documented (v1)
- [~] Rate limiting headers documented
- [~] Multi-tenancy explained (how to use TenantId)
- [~] Contact information and license in metadata
- [~] Export as OpenAPI JSON/YAML for client code generation

#### Implementation Notes
- Add Swashbuckle.AspNetCore package if not present
- Configure Swagger in Program.cs
- Add XML documentation to controllers and DTOs
- Use Swashbuckle attributes for customization: [SwaggerOperation], [SwaggerResponse]
- Update documentation on each controller/DTO update
- Provide documentation for webhook consumers

#### Files to Create/Modify
- MODIFY: `src/KromicStore.API/Program.cs` (Swagger setup)
- MODIFY: `src/KromicStore.API/appsettings.json` (Swagger metadata)
- UPDATE: All controller files (add XML documentation)
- UPDATE: All DTO files (add XML documentation)
- NEW: `docs/API-Guide.md` (manual documentation)
- NEW: `docs/Getting-Started.md` (quick start guide)

#### Related Requirements
- All Feature requirements


---

## WAVE 10: Build & Verification (2 tasks, sequential)

### Task 10.1: Build Solution and Fix Compilation Errors

**Status**: not_started
**Dependencies**: All waves (all tasks complete)
**Priority**: Critical
**Effort**: 1.5 hours

#### Description
Build complete solution verifying all projects compile without errors or warnings. Address any compilation issues, missing dependencies, or configuration problems.

#### Acceptance Criteria
- [~] Solution builds successfully without compilation errors
- [~] All projects compile (API, Application, Infrastructure, Domain, Contracts)
- [~] No compiler warnings (enable TreatWarningsAsErrors if applicable)
- [~] All NuGet dependencies resolved correctly
- [~] Project references correct (no circular dependencies)
- [~] Build time reasonable (< 30 seconds for clean build)
- [~] Release build succeeds with optimizations
- [~] Solution file updated with all new projects
- [~] Global usings configured appropriately
- [~] Any code analysis warnings addressed

#### Implementation Notes
- Use `dotnet build` from command line
- Check for missing using statements
- Verify all interfaces implemented
- Test in both Debug and Release configurations
- Ensure platform-specific code handled (if any)

#### Files to Verify
- `KromicStore.sln` (all projects referenced)
- `Directory.Build.props` (shared properties)
- All .csproj files (dependencies, framework version)

#### Related Requirements
- All requirements (cross-cutting)

---

### Task 10.2: Run All Tests and Verify Functionality

**Status**: not_started
**Dependencies**: Task 10.1
**Priority**: Critical
**Effort**: 2 hours

#### Description
Execute all unit, integration, and endpoint tests verifying functionality correctness. Verify test coverage meets minimum thresholds and all tests pass consistently.

#### Acceptance Criteria
- [~] All unit tests pass (xUnit or NUnit)
- [~] All integration tests pass (with test database)
- [~] All endpoint tests pass (with TestHost)
- [~] No flaky tests (tests pass consistently on repeated runs)
- [~] Test code coverage >= 70% for services and endpoints
- [~] Test code coverage >= 80% for domain entities
- [~] Test execution time reasonable (< 5 minutes for full suite)
- [~] Test output clear and readable
- [~] Failed test investigation documented
- [~] Performance tests included for critical paths (optional)

#### Implementation Notes
- Use `dotnet test` from command line
- Configure test framework (xUnit, NUnit)
- Use code coverage tools: OpenCover, Coverlet, or similar
- Run tests in CI/CD pipeline format
- Verify tests against multiple configurations if applicable

#### Test Command Examples
```bash
dotnet build KromicStore.sln
dotnet test KromicStore.sln --no-build --verbosity normal --logger "console;verbosity=normal"
```

#### Files to Verify
- `tests/KromicStore.Tests.Unit/` (all test files)
- `tests/KromicStore.Tests.Integration/` (all test files)
- Test configuration (appsettings.test.json, database connection strings)

#### Related Requirements
- All requirements (verification)

---

## Summary

### Execution Plan

**Total Tasks**: 60+ tasks organized in 10 waves
**Estimated Timeline**:
- Full-time (40 hrs/week): 4-6 weeks
- Part-time (10 hrs/week): 15-20 weeks

### Wave Dependencies

```
Wave 1 (Foundation) ──┬──→ Wave 2 (Proxies)
                      ├──→ Wave 4 (Configuration)
                      └──→ Wave 5 (Performance)

Wave 2 (Proxies) ────→ Wave 3 (Webhooks)
Wave 5 (Performance) ─┤
                      └──→ Wave 6 (Entities)

Wave 6 (Entities) ────→ Wave 7 (Controllers/Services)

Wave 7 (Controllers) ─→ Wave 8 (Onboarding)

Wave 7 (Controllers) ─→ Wave 9 (Testing)

All waves ────────────→ Wave 10 (Build & Verify)
```

### Key Implementation Principles

1. **SOLID Design**: Follow Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion
2. **Clean Architecture**: Separate layers with clear dependencies (Domain → Application → Infrastructure → API)
3. **DDD Principles**: Aggregates, value objects, domain events, ubiquitous language
4. **Testability**: All business logic testable with unit/integration tests
5. **Multi-Tenancy**: All queries filtered by TenantId, data isolation enforced
6. **Error Handling**: Standardized error responses, correlation IDs for tracing
7. **Performance**: Indexes, caching, query optimization, connection pooling
8. **Security**: Input validation, encryption, authentication/authorization, rate limiting

### Configuration Required

1. **appsettings.json**:
   - Database connection string (PostgreSQL)
   - Redis connection string
   - External service API keys (Razorpay, Google, Cloudinary, Brevo)
   - JWT settings (secret, issuer, audience)
   - Email configuration (Brevo template IDs)

2. **Environment Variables** (Production):
   - `ConnectionStrings__DefaultConnection`
   - `ConnectionStrings__Redis`
   - `ExternalServices__Razorpay__ApiKey`
   - `ExternalServices__Google__ClientId`
   - etc.

3. **Database Migration**:
   - Create initial migrations for new entities
   - Seed default configurations
   - Create indexes

### Deliverables

- Fully functional MVP with all features implemented
- Comprehensive test suite (unit + integration + endpoint tests)
- API documentation (Swagger/OpenAPI)
- Consumer guide for webhook integration
- Architecture documentation
- Configuration templates

### Risk Mitigation

- **Data Isolation**: Every query must include TenantId filter (code review required)
- **External Service Failures**: Circuit breakers prevent cascading failures
- **Performance**: Indexes, caching, query optimization implemented incrementally
- **Security**: Input validation, encryption, authentication on every endpoint

### Success Criteria

✅ Solution builds without errors
✅ All tests pass with >70% coverage
✅ API endpoints respond within SLA (< 500ms for common operations)
✅ Multi-tenancy enforced (no data leakage between tenants)
✅ External service calls resilient (retry, circuit breaker, timeout)
✅ All documentation complete and accurate



## WAVE 11: Storefront & Theming System (5 tasks, parallel after Wave 10)

### Task 11.1: Domain Models & Value Objects

**Status**: completed
**Dependencies**: Wave 1
**Priority**: Critical
**Effort**: 3 hours

#### Description
[Already completed in previous execution]

---

## WAVE 12: Deployment & Infrastructure (7 tasks, sequential after Wave 11)

### Task 12.1: Create Multi-Stage Dockerfile

**Status**: not_started
**Dependencies**: Wave 10 (build verified)
**Priority**: Critical
**Effort**: 2 hours

#### Description
Create production-ready Dockerfile using multi-stage build pattern. Build stage compiles the ASP.NET Core application, runtime stage prepares lightweight image suitable for Render deployment with health checks and environment variable support.

#### Acceptance Criteria
- [~] Multi-stage Dockerfile created: `Dockerfile`
- [~] Build stage uses official Microsoft .NET 8.0 SDK image
- [~] Build stage restores packages, builds project in Release configuration
- [~] Runtime stage uses official Microsoft ASP.NET Core 8.0 runtime image (Alpine or Debian)
- [~] Runtime image includes only runtime files (no SDK)
- [~] Startup script copied to image (entrypoint)
- [~] Health check configured: `HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 CMD curl -f http://localhost:${PORT:-8080}/health || exit 1`
- [~] Health check includes both HEAD and GET requests with database connectivity
- [~] Exposed port configurable via environment variable (default 8080)
- [~] Entrypoint invokes startup script for migration and seeding
- [~] Image built and tested locally: `docker build -t kromic-store:latest .`
- [~] Image runs successfully: `docker run --env DATABASE_URL=... kromic-store:latest`
- [~] No production secrets hardcoded in image
- [~] Image size minimal (< 500MB)

#### Implementation Notes
- Use `.dockerignore` to exclude build artifacts, tests, git files
- Build stage install dependencies: RUN dotnet restore
- Runtime stage: FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine (or debian-slim)
- Startup script chmod +x before COPY
- HEALTHCHECK includes curl or wget for connectivity test
- Health endpoint returns 200 with database status

#### Files to Create/Modify
- NEW: `Dockerfile` (root of solution)
- NEW: `.dockerignore` (exclude unnecessary files)
- NEW: `scripts/entrypoint.sh` (startup script, see Task 12.2)
- MODIFY: `src/KromicStore.API/Program.cs` (expose health endpoints, see Task 12.4)

#### Related Requirements
- Requirement 1: Containerization
- Requirement 5: Production Readiness

---

### Task 12.2: Create Startup Script with Migration Runner

**Status**: not_started
**Dependencies**: Task 12.1
**Priority**: Critical
**Effort**: 1.5 hours

#### Description
Create `entrypoint.sh` startup script invoked by Docker container. Script waits for database availability, executes pending EF Core migrations, seeds default data if needed, and starts the ASP.NET Core application.

#### Acceptance Criteria
- [~] Startup script `scripts/entrypoint.sh` created (Bash/Shell)
- [~] Script waits for PostgreSQL availability using pg_isready or connection retry loop
- [~] Retry logic: 30 retries with 2-second sleep between attempts
- [~] Executes database migrations: `dotnet ef database update --project src/KromicStore.Infrastructure --startup-project src/KromicStore.API`
- [~] Logs migration execution status (start, success, failure)
- [~] Handles migration failures with clear error message and exit code 1
- [~] If migrations succeed, starts application: `exec dotnet src/KromicStore.API/bin/Release/net8.0/KromicStore.API.dll`
- [~] Script uses environment variables: `DATABASE_URL`, `ASPNETCORE_ENVIRONMENT`
- [~] Script logging sends to STDOUT/STDERR for container logs
- [~] Script executable: chmod +x scripts/entrypoint.sh
- [~] Docker image invokes script as entrypoint

#### Implementation Notes
- Bash script preferred for Linux container compatibility
- Use set -e for fail-fast behavior
- pg_isready utility available in most PostgreSQL images
- Log format: `[entrypoint] <action> <status>`
- Database URL format: postgresql://user:pass@host:5432/database
- ASPNETCORE_ENVIRONMENT: Development, Staging, Production

#### Files to Create
- NEW: `scripts/entrypoint.sh`
- NEW: `scripts/wait-for-db.sh` (optional helper)

#### Related Requirements
- Requirement 2: Database Migration Automation
- Requirement 3: Startup Script

---

### Task 12.3: Configure Environment Variables and appsettings

**Status**: not_started
**Dependencies**: Wave 1 (Program.cs configured)
**Priority**: Critical
**Effort**: 1.5 hours

#### Description
Configure application to load all settings from environment variables. No production secrets hardcoded. Support multiple environments (Development, Staging, Production) with environment-specific configurations.

#### Acceptance Criteria
- [~] All secrets loaded from environment variables: DATABASE_URL, JWT_SECRET, RAZORPAY_KEY, GOOGLE_CLIENT_ID, CLOUDINARY_API_KEY, BREVO_API_KEY, REDIS_URL
- [~] No secrets in source code or default appsettings.json
- [~] `appsettings.Development.json` for local development with test credentials
- [~] `appsettings.Production.json` template provided (actual values from environment)
- [~] `appsettings.Staging.json` for staging environment
- [~] IConfiguration properly injected in Startup (Program.cs)
- [~] Application fails fast with clear error if required configuration missing
- [~] Environment variable validation on startup: check required keys present
- [~] Logging configuration via environment (LOG_LEVEL, SERILOG_MINIMUM_LEVEL)
- [~] Database connection string validated on startup
- [~] Application version/environment logged on startup

#### Implementation Notes
- Use configuration builder: `builder.Configuration.AddEnvironmentVariables()`
- Support both colon `:` and double underscore `__` separators for nested keys
- Secrets validation in Program.cs with throw if missing
- Error message includes required environment variable names
- Development config can have non-sensitive defaults
- Production config expects all values from environment

#### Files to Create/Modify
- NEW: `src/KromicStore.API/appsettings.Production.json` (template)
- NEW: `src/KromicStore.API/appsettings.Staging.json`
- MODIFY: `src/KromicStore.API/appsettings.Development.json`
- MODIFY: `src/KromicStore.API/Program.cs` (validate required config on startup)
- NEW: `docs/Environment-Setup.md` (document all environment variables)

#### Related Requirements
- Requirement 4: Environment Configuration
- Requirement 6: Production Readiness

---

### Task 12.4: Implement Health Check Endpoints

**Status**: not_started
**Dependencies**: Task 12.1
**Priority**: High
**Effort**: 1.5 hours

#### Description
Implement health check endpoints for Render deployment. Endpoints distinguish between liveness (is process running?) and readiness (is app ready to receive traffic?).

#### Acceptance Criteria
- [~] `GET /health` endpoint returns 200 with liveness status (always returns 200 if app running)
- [~] `GET /health/ready` endpoint returns 200 if ready to receive traffic, 503 if degraded
- [~] `HEAD /health` endpoint returns 200 (for simpler health check protocols)
- [~] Liveness response: `{ "status": "Healthy" }` (minimal, fast)
- [~] Readiness response includes checks: `{ "status": "Healthy", "checks": { "database": "Healthy", "cache": "Healthy", "dependencies": "Healthy" } }`
- [~] Database check: attempts SELECT 1 query with timeout
- [~] Cache check: attempts SET/GET to Redis (if enabled)
- [~] All checks must complete within 10 seconds total
- [~] Failed database check returns 503 Service Unavailable
- [~] Failed cache check returns 503 but logs warning
- [~] Endpoints require no authentication
- [~] Response includes X-Response-Time header with milliseconds
- [~] Logging tracks health check calls (limited to DEBUG level to avoid log spam)

#### Implementation Notes
- Use Microsoft.Extensions.Diagnostics.HealthChecks
- Database check: new DbHealthCheck() via EntityFramework health check
- Cache check: new RedisHealthCheck() if Redis connected
- Configure in Program.cs: `builder.Services.AddHealthChecks().Add...`
- Map endpoints: `app.MapHealthChecks("/health")` and `app.MapHealthChecks("/health/ready")`
- Render uses `/health` endpoint for uptime monitoring

#### Files to Create/Modify
- NEW: `src/KromicStore.API/HealthChecks/DatabaseHealthCheck.cs`
- NEW: `src/KromicStore.API/HealthChecks/RedisHealthCheck.cs`
- MODIFY: `src/KromicStore.API/Program.cs` (register health checks, map endpoints)
- MODIFY: `src/KromicStore.API/appsettings.json` (health check settings)

#### Related Requirements
- Requirement 7: Health Checks

---

### Task 12.5: Create Render Deployment Configuration

**Status**: not_started
**Dependencies**: Tasks 12.1-12.4
**Priority**: High
**Effort**: 1.5 hours

#### Description
Create configuration and documentation for Render deployment. Include render.yaml, environment variable examples, and deployment procedure documentation.

#### Acceptance Criteria
- [~] `render.yaml` file created in repository root
- [~] render.yaml specifies: Docker build image, environment variables (as placeholders), health check path `/health`
- [~] render.yaml configures Dockerfile: `dockerfilePath: ./Dockerfile`
- [~] render.yaml sets health check endpoint: `healthCheckPath: /health`
- [~] render.yaml exposes port: `8080` (matches Dockerfile)
- [~] Environment variables template includes all required keys (database, secrets, etc.)
- [~] Documentation explains: connect GitHub repo, configure environment variables, deploy
- [~] Documentation includes Render-specific settings (region, instance type, memory)
- [~] Example environment file provided: `.env.render.example`
- [~] Post-deployment verification steps documented
- [~] Rollback procedure documented (if deployment fails)

#### Implementation Notes
- Render supports build from Dockerfile
- Environment variables must be set in Render dashboard before deployment
- Database URL format: postgresql://user:pass@host:5432/db
- Health check enabled for uptime monitoring
- Auto-scaling configured (if needed)
- Render uses 1GB RAM default (adjustable)

#### Files to Create
- NEW: `render.yaml` (Render deployment configuration)
- NEW: `.env.render.example` (environment template)
- NEW: `docs/Render-Deployment.md` (deployment guide)

#### Related Requirements
- Requirement 8: Render Deployment

---

### Task 12.6: Configure Structured Logging for Startup Diagnostics

**Status**: not_started
**Dependencies**: Task 12.3
**Priority**: High
**Effort**: 1.5 hours

#### Description
Configure structured logging using Serilog to provide clear startup diagnostics. Log application version, environment, database connection, migration status, and startup completion.

#### Acceptance Criteria
- [~] Serilog configured in Program.cs with console sink for container logs
- [~] Log startup banner: `"KromicStore API started: version={Version}, environment={Environment}"`
- [~] Log database connection: `"Database connection established: {ConnectionString} (masked)"` (mask password)
- [~] Log migration execution: `"Executing database migrations..."` then `"Database migrations completed successfully"` or error
- [~] Log application startup complete: `"Application ready to receive requests on http://0.0.0.0:8080"`
- [~] Log startup failures with full exception stack trace and context
- [~] All startup logs at INFO level (visible by default)
- [~] Correlation IDs propagated to all logs (if enabled)
- [~] Structured fields: `@l (level), @mt (message template), @ts (timestamp), Exception`
- [~] JSON output format for container log aggregation (ELK, CloudWatch, etc.)
- [~] Log levels configurable via environment: `LOG_LEVEL` (Debug, Information, Warning, Error)
- [~] Minimum log level: Information (production), Debug (development)

#### Implementation Notes
- Serilog setup: `Log.Logger = new LoggerConfiguration()...CreateLogger()`
- Console sink output: async, buffered
- JSON formatter for structured logging
- Property enrichment: Version, Environment, MachineName, ProcessId
- Exception destructuring enabled for stack traces
- Sensitive data (passwords, tokens) masked in logs

#### Files to Create/Modify
- MODIFY: `src/KromicStore.API/Program.cs` (configure Serilog)
- MODIFY: `src/KromicStore.API/appsettings.json` (log settings)
- NEW: `docs/Logging-Configuration.md` (explain log levels, settings)

#### Related Requirements
- Requirement 9: Logging

---

### Task 12.7: Create Build Verification and Deployment Testing

**Status**: not_started
**Dependencies**: Tasks 12.1-12.6
**Priority**: High
**Effort**: 2 hours

#### Description
Verify Docker build process, test health endpoints, and document deployment verification steps. Ensure deployment ready with no manual intervention required.

#### Acceptance Criteria
- [~] Docker image builds successfully: `docker build -t kromic-store:latest .`
- [~] Build logs show no errors or warnings
- [~] Image size verified: `docker images | grep kromic-store` (< 500MB)
- [~] Container runs locally with test database
- [~] Health check endpoint responds: `curl http://localhost:8080/health`
- [~] Readiness check endpoint responds: `curl http://localhost:8080/health/ready`
- [~] Database migrations execute on startup (verify with logs)
- [~] Application logs show startup complete message
- [~] No manual server configuration required post-deployment
- [~] Render deployment template tested (dry-run if possible)
- [~] Deployment documentation complete and accurate
- [~] Rollback procedure tested and documented

#### Implementation Notes
- Local testing: use docker-compose with test database
- Verify all environment variables set before container start
- Check startup time (should be < 30 seconds)
- Monitor logs during first 5 minutes post-deployment
- Verify data integrity after migration
- Test with production database URI (to verify connection)

#### Files to Create/Modify
- NEW: `docker-compose.test.yml` (local testing)
- NEW: `docs/Deployment-Checklist.md` (pre/post deployment steps)
- NEW: `docs/Troubleshooting.md` (common issues and solutions)
- MODIFY: `README.md` (update with deployment section)

#### Related Requirements
- Requirement 8: Render Deployment
- Requirement 9: Production Readiness

---

## Deployment Verification Report

### Pre-Deployment Checklist

- [~] All tests pass locally
- [~] Build succeeds in Release configuration
- [~] Docker image builds without errors
- [~] Health endpoints respond correctly
- [~] All environment variables documented
- [~] Database migrations tested locally
- [~] Logging configured for production
- [~] No secrets in source code or default configs
- [~] API documentation complete (Swagger)
- [~] Monitoring and alerting configured (if applicable)

### Post-Deployment Verification

- [~] Application health check returns 200
- [~] API endpoints respond with expected status codes
- [~] Database migrations executed successfully
- [~] Startup logs show application ready
- [~] Health check includes database and cache status
- [~] No errors in application logs after 5 minutes
- [~] Sample API call succeeds (GET /health, GET /api/v1/products)
- [~] Render dashboard shows application running
- [~] Response times within SLA (< 500ms for API calls)

---

## Updated Summary

### Waves Completed

- ✅ Wave 1: Foundation & Infrastructure
- ✅ Wave 2: External Service Proxies
- ✅ Wave 3: Webhook System
- ✅ Wave 4: Configuration Management
- ✅ Wave 5: Performance Optimization
- ✅ Wave 6: Enhanced Domain Entities
- ✅ Wave 7: API Controllers & Services
- ✅ Wave 8: Tenant Registration & Onboarding
- ✅ Wave 9: Testing & Documentation
- ✅ Wave 10: Build & Verification
- ✅ Wave 11: Storefront & Theming System
- ⏳ Wave 12: Deployment & Infrastructure (IN PROGRESS)

### Total Tasks

**Original**: 60+ tasks in 10 waves
**Updated**: 75+ tasks in 12 waves
**New**: Wave 12 adds 7 deployment/infrastructure tasks

### Final Deliverables

Upon completion of Wave 12, the solution will include:

1. ✅ Complete ASP.NET Core MVP application
2. ✅ Comprehensive test suite (unit + integration + endpoint)
3. ✅ Full API documentation (Swagger/OpenAPI)
4. ✅ Docker containerization (production-ready)
5. ✅ Database migration automation
6. ✅ Health check endpoints
7. ✅ Structured logging for diagnostics
8. ✅ Environment variable configuration
9. ✅ Render deployment artifacts
10. ✅ Deployment documentation and checklists

### Production Readiness Verification

**✅ Containerization**: Multi-stage Dockerfile with Alpine runtime
**✅ Migrations**: Automated on startup via entrypoint.sh
**✅ Configuration**: All secrets from environment variables
**✅ Health Checks**: Liveness and readiness endpoints
**✅ Logging**: Structured logging with startup diagnostics
**✅ Deployment**: Single-click deployment to Render with git integration
**✅ Documentation**: Complete deployment and troubleshooting guides
**✅ Verification**: Pre/post-deployment checklists included

---

**Status Update**: The KromicStore MVP Enhancement project is now fully specified for production deployment. Wave 12 completes the specification for containerization and Render deployment.
