namespace KromicStore.Infrastructure.Services.StorefrontServices;

using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

/// <summary>
/// Main orchestration service for storefront creation.
/// Handles creation from themes, from scratch, and validation of mandatory fields.
/// </summary>
public class StorefrontCreationService : IStorefrontCreationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ThemeCloneService _themeCloneService;
    private readonly DefaultDataPopulator _defaultDataPopulator;
    private readonly ILogger<StorefrontCreationService> _logger;

    /// <summary>
    /// Initializes a new instance of the StorefrontCreationService class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="themeCloneService">The theme cloning service.</param>
    /// <param name="defaultDataPopulator">The default data populator service.</param>
    /// <param name="logger">The logger instance.</param>
    public StorefrontCreationService(
        IUnitOfWork unitOfWork,
        ThemeCloneService themeCloneService,
        DefaultDataPopulator defaultDataPopulator,
        ILogger<StorefrontCreationService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _themeCloneService = themeCloneService ?? throw new ArgumentNullException(nameof(themeCloneService));
        _defaultDataPopulator = defaultDataPopulator ?? throw new ArgumentNullException(nameof(defaultDataPopulator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a storefront from a theme template.
    /// Orchestrates theme loading, cloning, default data population, and saving.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="themeId">The theme ID to use as template.</param>
    /// <param name="storeName">The name for the new storefront.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the created storefront.</returns>
    /// <exception cref="ArgumentException">Thrown when tenant or store name is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when theme is not found or theme JSON is invalid.</exception>
    public async Task<Guid> CreateFromThemeAsync(
        Guid tenantId,
        Guid themeId,
        string storeName,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (themeId == Guid.Empty)
            throw new ArgumentException("Theme ID is required.", nameof(themeId));
        if (string.IsNullOrWhiteSpace(storeName))
            throw new ArgumentException("Store name is required.", nameof(storeName));

        _logger.LogInformation(
            "Starting storefront creation from theme {ThemeId} for tenant {TenantId} with name {StoreName}",
            themeId, tenantId, storeName);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Step 1: Load theme by ID
            var theme = await _unitOfWork.Themes.GetByIdAsync(themeId, cancellationToken);
            if (theme == null)
            {
                _logger.LogError("Theme {ThemeId} not found", themeId);
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw new InvalidOperationException($"Theme {themeId} not found.");
            }

            if (!theme.IsActive)
            {
                _logger.LogError("Theme {ThemeId} is not active", themeId);
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw new InvalidOperationException($"Theme {themeId} is not active.");
            }

            _logger.LogInformation("Theme {ThemeId} loaded successfully", themeId);

            // Step 2: Create storefront aggregate from theme
            var storefront = Storefront.CreateFromTheme(tenantId, storeName, themeId);
            _logger.LogInformation("Storefront aggregate created with ID {StorefrontId}", storefront.Id);

            // Step 3: Clone theme pages/sections/components
            await _themeCloneService.CloneThemeToStorefrontAsync(theme, storefront, cancellationToken);
            _logger.LogInformation("Theme pages cloned to storefront {StorefrontId}", storefront.Id);

            // Step 4: Populate default data
            await _defaultDataPopulator.PopulateDefaultDataAsync(storefront, tenantId, cancellationToken);
            _logger.LogInformation("Default data populated for storefront {StorefrontId}", storefront.Id);

            // Step 5: Save storefront transactionally
            await _unitOfWork.Storefronts.AddAsync(storefront, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Storefront {StorefrontId} saved to database", storefront.Id);

            // Step 6: Commit transaction
            await _unitOfWork.CommitAsync(cancellationToken);
            _logger.LogInformation("Transaction committed for storefront {StorefrontId}", storefront.Id);

            return storefront.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating storefront from theme {ThemeId} for tenant {TenantId}", 
                themeId, tenantId);
            
            try
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                _logger.LogInformation("Transaction rolled back");
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Error rolling back transaction");
            }

            throw;
        }
    }

    /// <summary>
    /// Creates an empty storefront from scratch (without theme).
    /// Creates storefront with default home page.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="storeName">The name for the new storefront.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the created storefront.</returns>
    /// <exception cref="ArgumentException">Thrown when tenant or store name is invalid.</exception>
    public async Task<Guid> CreateFromScratchAsync(
        Guid tenantId,
        string storeName,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(storeName))
            throw new ArgumentException("Store name is required.", nameof(storeName));

        _logger.LogInformation(
            "Starting storefront creation from scratch for tenant {TenantId} with name {StoreName}",
            tenantId, storeName);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Step 1: Create empty storefront
            var storefront = Storefront.CreateFromScratch(tenantId, storeName);
            _logger.LogInformation("Storefront aggregate created with ID {StorefrontId}", storefront.Id);

            // Step 2: Create default home page
            var homePage = StorefrontPage.Create(
                tenantId,
                storefront.Id,
                "Home",
                "home",
                displayOrder: 0,
                description: "Home page");

            storefront.AddPage(homePage);
            _logger.LogInformation("Default home page created for storefront {StorefrontId}", storefront.Id);

            // Step 3: Populate default data
            await _defaultDataPopulator.PopulateDefaultDataAsync(storefront, tenantId, cancellationToken);
            _logger.LogInformation("Default data populated for storefront {StorefrontId}", storefront.Id);

            // Step 4: Save storefront transactionally
            await _unitOfWork.Storefronts.AddAsync(storefront, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Storefront {StorefrontId} saved to database", storefront.Id);

            // Step 5: Commit transaction
            await _unitOfWork.CommitAsync(cancellationToken);
            _logger.LogInformation("Transaction committed for storefront {StorefrontId}", storefront.Id);

            return storefront.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating storefront from scratch for tenant {TenantId}", tenantId);
            
            try
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                _logger.LogInformation("Transaction rolled back");
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Error rolling back transaction");
            }

            throw;
        }
    }

    /// <summary>
    /// Validates that all mandatory fields for a storefront are provided (not placeholders).
    /// </summary>
    /// <param name="storefrontId">The storefront ID to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A ValidationResult containing validation status and any missing field names.</returns>
    /// <exception cref="ArgumentException">Thrown when storefront ID is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when storefront is not found.</exception>
    public async Task<ValidationResult> ValidateMandatoryFieldsAsync(
        Guid storefrontId,
        CancellationToken cancellationToken = default)
    {
        if (storefrontId == Guid.Empty)
            throw new ArgumentException("Storefront ID is required.", nameof(storefrontId));

        _logger.LogInformation("Validating mandatory fields for storefront {StorefrontId}", storefrontId);

        try
        {
            // Retrieve storefront
            // Note: Without tenant context, we retrieve without filtering by tenant
            // In production, this should be accessed through a tenant-scoped repository
            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(storefrontId, cancellationToken);
            if (storefront == null)
            {
                _logger.LogError("Storefront {StorefrontId} not found", storefrontId);
                throw new InvalidOperationException($"Storefront {storefrontId} not found.");
            }

            // Check mandatory fields
            if (storefront.MandatoryFields.AreAllFieldsProvided())
            {
                _logger.LogInformation("Storefront {StorefrontId} has all mandatory fields provided", storefrontId);
                return ValidationResult.CreateSuccess();
            }

            // Collect missing field names
            var missingFields = new List<string>();

            if (storefront.MandatoryFields.IsStoreNamePlaceholder)
                missingFields.Add("Store Name");
            if (storefront.MandatoryFields.IsLogoPlaceholder)
                missingFields.Add("Logo");
            if (storefront.MandatoryFields.IsEmailPlaceholder)
                missingFields.Add("Contact Email");
            if (storefront.MandatoryFields.IsPhonePlaceholder)
                missingFields.Add("Contact Phone");
            if (storefront.MandatoryFields.IsAddressPlaceholder)
                missingFields.Add("Address");
            if (storefront.MandatoryFields.IsCurrencyPlaceholder)
                missingFields.Add("Currency");
            if (storefront.MandatoryFields.IsCountryPlaceholder)
                missingFields.Add("Country");
            if (storefront.MandatoryFields.IsBrandColorPlaceholder)
                missingFields.Add("Brand Color");
            if (storefront.MandatoryFields.IsCopyrightPlaceholder)
                missingFields.Add("Copyright");

            _logger.LogWarning(
                "Storefront {StorefrontId} is missing mandatory fields: {MissingFields}",
                storefrontId,
                string.Join(", ", missingFields));

            return ValidationResult.CreateFailure(missingFields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating mandatory fields for storefront {StorefrontId}", storefrontId);
            throw;
        }
    }
}
