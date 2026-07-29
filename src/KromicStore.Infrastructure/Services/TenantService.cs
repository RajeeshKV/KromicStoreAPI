using KromicStore.Application.DTOs.Tenant;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Infrastructure.BackgroundJobs;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace KromicStore.Infrastructure.Services
{
    /// <summary>
    /// Service for managing tenant operations including registration, retrieval, updates, and account status management.
    /// Handles the complete tenant lifecycle from registration through suspension/reactivation.
    /// </summary>
    public class TenantService : ITenantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TenantService> _logger;
        private readonly IAuthService _authService;
        private readonly IEncryptionService _encryptionService;
        private readonly TenantConfigurationSeeder _configurationSeeder;
        private readonly IBackgroundJobClient _backgroundJobClient;

        /// <summary>
        /// Initializes a new instance of the TenantService class.
        /// </summary>
        /// <param name="unitOfWork">Unit of work for data persistence operations</param>
        /// <param name="logger">Logger for diagnostic information</param>
        /// <param name="authService">Authentication service for JWT token generation</param>
        /// <param name="encryptionService">Encryption service for API secret encryption</param>
        /// <param name="configurationSeeder">Configuration seeder for initializing default tenant settings</param>
        /// <param name="backgroundJobClient">Hangfire client for queuing background jobs</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
        public TenantService(
            IUnitOfWork unitOfWork,
            ILogger<TenantService> logger,
            IAuthService authService,
            IEncryptionService encryptionService,
            TenantConfigurationSeeder configurationSeeder,
            IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _configurationSeeder = configurationSeeder ?? throw new ArgumentNullException(nameof(configurationSeeder));
            _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        }

        /// <summary>
        /// Registers a new tenant with initial admin user and default configuration.
        /// Orchestrates the complete registration workflow including:
        /// - Tenant entity creation
        /// - TenantOwner user creation with hashed password
        /// - Trial subscription initialization
        /// - Default configuration setup via TenantConfigurationSeeder
        /// - API credential generation
        /// </summary>
        /// <param name="request">Tenant registration details including company name, contact info, and admin credentials</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Registration response containing tenant ID, API credentials, and access token</returns>
        /// <exception cref="InvalidOperationException">Thrown when email already exists or registration fails</exception>
        public async Task<TenantRegistrationResponse> RegisterAsync(RegisterTenantRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Starting tenant registration for company: {CompanyName}, email: {Email}", request.CompanyName, request.Email);

            try
            {
                // Begin transaction for atomicity
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // 1. Validate email uniqueness across all tenants
                var existingUser = (await _unitOfWork.Users.FindAsync(u => u.Email == request.Email, cancellationToken)).FirstOrDefault();
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} already in use", request.Email);
                    throw new InvalidOperationException("Email address is already registered.");
                }

                // 1.5. Validate subdomain uniqueness
                var existingTenant = (await _unitOfWork.Tenants.FindAsync(t => t.Subdomain.ToLower() == request.Subdomain.ToLower(), cancellationToken)).FirstOrDefault();
                if (existingTenant != null)
                {
                    _logger.LogWarning("Registration failed: Subdomain {Subdomain} already in use", request.Subdomain);
                    throw new InvalidOperationException("Subdomain is already taken.");
                }

                // 2. Create Tenant entity using factory method
                var tenantId = Guid.NewGuid();
                var tenantSlug = GenerateTenantId();
                var tenant = Tenant.Create(tenantSlug, request.CompanyName, request.Subdomain, string.Empty, request.Email);
                // Override the ID if needed (EF Core will handle it)
                await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Tenant created: TenantId={TenantId}, Name={Name}, Subdomain={Subdomain}", tenant.Id, request.CompanyName, request.Subdomain);

                // 3. Create TenantOwner User
                var passwordHash = HashPassword(request.Password);
                var user = User.Create(tenant.Id, request.FirstName, request.LastName, request.Email, UserRole.TenantOwner);
                user.SetPasswordHash(passwordHash);

                await _unitOfWork.Users.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("TenantOwner user created: UserId={UserId}, Email={Email}", user.Id, request.Email);

                // 4. Create Default Subscription (Trial - 14 days)
                var trialSubscription = Subscription.CreateTrial(tenant.Id, trialDays: 14, SubscriptionPlan.Starter);
                await _unitOfWork.Subscriptions.AddAsync(trialSubscription, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Trial subscription created for tenant: TenantId={TenantId}", tenant.Id);

                // 5. Generate API Credentials
                var apiKey = GenerateApiKey(tenant.Id);
                var apiSecret = GenerateApiSecret();

                // 6. Initialize Default Configuration via Seeder
                // Country code can be extracted from request if needed, otherwise defaults are used
                await _configurationSeeder.SeedDefaultConfigurationAsync(
                    tenantId: tenant.Id,
                    country: request.Country, // Pass country for currency/timezone defaults
                    cancellationToken: cancellationToken);
                _logger.LogInformation("Default configuration initialized for tenant: TenantId={TenantId}", tenant.Id);

                // 7. Generate JWT Access Token (24 hours)
                var accessToken = _authService.GenerateAccessToken(user.Id, tenant.Id, request.Email, new[] { UserRole.TenantOwner.ToString() });

                // Commit transaction
                await _unitOfWork.CommitAsync(cancellationToken);

                var response = new TenantRegistrationResponse
                {
                    TenantId = tenant.Id,
                    CompanyName = request.CompanyName,
                    Email = request.Email,
                    AccessToken = accessToken,
                    ApiKey = apiKey,
                    ApiSecret = apiSecret,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Tenant registration completed successfully: TenantId={TenantId}", tenant.Id);

                // Queue welcome email as background job (non-blocking)
                // Hangfire will retry with exponential backoff if email sending fails
                try
                {
                    var tenantAdminName = $"{request.FirstName} {request.LastName}".Trim();
                    var trialEndDate = trialSubscription.TrialEndsAt ?? DateTime.UtcNow.AddDays(14);
                    
                    _backgroundJobClient.Enqueue<SendWelcomeEmailJob>(job => job.ExecuteAsync(
                        tenant.Id,
                        request.CompanyName,
                        request.Email,
                        tenantAdminName,
                        trialEndDate,
                        CancellationToken.None));

                    _logger.LogInformation("Welcome email job queued for TenantId={TenantId}", tenant.Id);
                }
                catch (Exception ex)
                {
                    // Log error but don't fail registration - welcome email is not critical
                    _logger.LogWarning(ex, "Failed to queue welcome email for TenantId={TenantId}, will retry later", tenant.Id);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant registration failed for email: {Email}", request.Email);
                try
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Rollback failed during registration error handling");
                }
                throw;
            }
        }

        /// <summary>
        /// Retrieves tenant details by ID.
        /// Returns public tenant information without sensitive data.
        /// </summary>
        /// <param name="tenantId">Unique identifier of the tenant</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Tenant details if found; null if tenant does not exist</returns>
        public async Task<TenantResponse?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            // Look up by TenantId string field
            var tenants = await _unitOfWork.Tenants.FindAsync(
                t => t.TenantId == tenantId.ToString(), 
                cancellationToken);
            
            var tenant = tenants.FirstOrDefault();
            if (tenant == null)
                return null;

            return new TenantResponse
            {
                Id = tenant.Id,
                CompanyName = tenant.Name,
                Email = tenant.ContactEmail,
                Country = string.Empty, // Not stored in current Tenant model
                Status = tenant.IsActive ? "Active" : "Suspended",
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            };
        }

        /// <summary>
        /// Updates tenant information such as company name, country, and subdomain.
        /// Only TenantOwner can perform this operation.
        /// Lookup is done by TenantId (string) from JWT, not by GUID primary key.
        /// </summary>
        /// <param name="tenantId">The tenant GUID from URL (used for authorization check only)</param>
        /// <param name="request">Updated tenant information (all fields optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Updated tenant details</returns>
        /// <exception cref="InvalidOperationException">Thrown when tenant not found or update fails</exception>
        public async Task<TenantResponse> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // The tenantId parameter is a UUID from the URL path
            // We need to look up the tenant by TenantId string field (from JWT/context)
            var tenants = await _unitOfWork.Tenants.FindAsync(
                t => t.TenantId == tenantId.ToString(), 
                cancellationToken);
            
            var tenant = tenants.FirstOrDefault();
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found by TenantId: {TenantId}", tenantId);
                throw new InvalidOperationException($"Tenant with ID {tenantId} not found.");
            }

            // Update subdomain if provided
            if (!string.IsNullOrEmpty(request.Subdomain))
            {
                // Check subdomain uniqueness (case-insensitive)
                var existingTenant = (await _unitOfWork.Tenants.FindAsync(
                    t => t.Subdomain.ToLower() == request.Subdomain.ToLower() && t.TenantId != tenantId.ToString(), 
                    cancellationToken)).FirstOrDefault();
                
                if (existingTenant != null)
                {
                    throw new InvalidOperationException($"Subdomain '{request.Subdomain}' is already taken.");
                }

                tenant.UpdateSubdomain(request.Subdomain);
                _logger.LogInformation("Tenant subdomain updated: TenantId={TenantId}, Subdomain={Subdomain}", tenantId, request.Subdomain);
            }

            // Update company name if provided
            if (!string.IsNullOrEmpty(request.CompanyName))
            {
                tenant.Update(request.CompanyName, tenant.Description, tenant.ContactEmail, tenant.ContactPhone);
                _logger.LogInformation("Tenant name updated: TenantId={TenantId}, Name={Name}", tenantId, request.CompanyName);
            }

            tenant.UpdateTimestamp();
            _unitOfWork.Tenants.Update(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tenant updated: TenantId={TenantId}", tenantId);

            return new TenantResponse
            {
                Id = tenant.Id,
                CompanyName = tenant.Name,
                Email = tenant.ContactEmail,
                Country = string.Empty,
                Status = tenant.IsActive ? "Active" : "Suspended",
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            };
        }

        /// <summary>
        /// Suspends a tenant account, preventing all operations until reactivated.
        /// Only SuperUser can suspend tenants.
        /// </summary>
        /// <param name="tenantId">Unique identifier of the tenant to suspend</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if suspension was successful; false if tenant not found or already suspended</returns>
        /// <exception cref="InvalidOperationException">Thrown when suspension fails or tenant is in invalid state</exception>
        public async Task<bool> SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
                return false;

            if (!tenant.IsActive)
                return false;

            tenant.Deactivate();
            tenant.UpdateTimestamp();
            _unitOfWork.Tenants.Update(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tenant suspended: TenantId={TenantId}", tenantId);
            return true;
        }

        /// <summary>
        /// Reactivates a suspended tenant account, restoring full access.
        /// Only SuperUser can reactivate tenants.
        /// </summary>
        /// <param name="tenantId">Unique identifier of the tenant to reactivate</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if reactivation was successful; false if tenant not found or not suspended</returns>
        /// <exception cref="InvalidOperationException">Thrown when reactivation fails or tenant is in invalid state</exception>
        public async Task<bool> ReactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
                return false;

            if (tenant.IsActive)
                return false;

            tenant.Activate();
            tenant.UpdateTimestamp();
            _unitOfWork.Tenants.Update(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tenant reactivated: TenantId={TenantId}", tenantId);
            return true;
        }

        /// <summary>
        /// Generates a unique tenant ID slug.
        /// </summary>
        private string GenerateTenantId()
        {
            // Generate a URL-friendly tenant ID (e.g., "tenant_a1b2c3d4")
            var guid = Guid.NewGuid().ToString().Substring(0, 8);
            return $"tenant_{guid.ToLower()}";
        }

        /// <summary>
        /// Hashes a password using PBKDF2.
        /// </summary>
        private string HashPassword(string password)
        {
            const int keySize = 64; // 512 bits
            const int iterations = 10000;

            using (var algorithm = new Rfc2898DeriveBytes(password, 16, iterations, HashAlgorithmName.SHA256))
            {
                var key = Convert.ToBase64String(algorithm.GetBytes(keySize));
                var salt = Convert.ToBase64String(algorithm.Salt);
                return $"{iterations}.{salt}.{key}";
            }
        }

        /// <summary>
        /// Generates a public API key in the format: {TenantId}_{RandomString}
        /// </summary>
        private string GenerateApiKey(Guid tenantId)
        {
            var randomPart = GenerateRandomString(16);
            return $"{tenantId:N}_{randomPart}".ToLower();
        }

        /// <summary>
        /// Generates a secure random API secret.
        /// </summary>
        private string GenerateApiSecret()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Generates a random string of specified length.
        /// </summary>
        private string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}
