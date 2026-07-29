namespace KromicStore.Infrastructure.Services.StorefrontServices;

using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for cloning theme definitions into storefronts.
/// Parses theme JSON and creates StorefrontPage, StorefrontSection, and StorefrontComponent entities.
/// </summary>
public class ThemeCloneService
{
    private readonly ILogger<ThemeCloneService> _logger;

    /// <summary>
    /// Initializes a new instance of the ThemeCloneService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ThemeCloneService(ILogger<ThemeCloneService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Clones theme pages, sections, and components into a storefront.
    /// </summary>
    /// <param name="theme">The theme to clone from.</param>
    /// <param name="storefront">The storefront to clone into.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The storefront with cloned content (reference for chaining).</returns>
    /// <exception cref="ArgumentNullException">Thrown when theme or storefront is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when theme JSON is invalid or cannot be parsed.</exception>
    public Task<Storefront> CloneThemeToStorefrontAsync(
        Theme theme,
        Storefront storefront,
        CancellationToken cancellationToken = default)
    {
        if (theme == null)
            throw new ArgumentNullException(nameof(theme));
        if (storefront == null)
            throw new ArgumentNullException(nameof(storefront));

        _logger.LogInformation("Starting theme clone for storefront {StorefrontId} from theme {ThemeId}", 
            storefront.Id, theme.Id);

        try
        {
            // Parse theme definition JSON
            using var jsonDoc = JsonDocument.Parse(theme.DefinitionJson);
            var root = jsonDoc.RootElement;

            // Extract default pages from theme definition
            if (!root.TryGetProperty("defaultPages", out var pagesElement))
            {
                _logger.LogWarning("Theme {ThemeId} has no defaultPages property", theme.Id);
                return Task.FromResult(storefront);
            }

            if (pagesElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Theme {ThemeId} defaultPages is not an array", theme.Id);
                return Task.FromResult(storefront);
            }

            int pageOrder = 0;

            // Iterate through each page in the theme
            foreach (var pageElement in pagesElement.EnumerateArray())
            {
                var page = ClonePage(pageElement, storefront, pageOrder, cancellationToken);
                if (page != null)
                {
                    storefront.AddPage(page);
                    pageOrder++;

                    _logger.LogInformation("Cloned page {PageName} (order {Order}) to storefront {StorefrontId}", 
                        page.Name, page.DisplayOrder, storefront.Id);
                }
            }

            return Task.FromResult(storefront);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in theme {ThemeId} definition", theme.Id);
            throw new InvalidOperationException($"Theme {theme.Id} has invalid JSON definition.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning theme {ThemeId} to storefront {StorefrontId}", 
                theme.Id, storefront.Id);
            throw;
        }
    }

    /// <summary>
    /// Clones a page from theme JSON element.
    /// </summary>
    private StorefrontPage? ClonePage(
        JsonElement pageElement,
        Storefront storefront,
        int displayOrder,
        CancellationToken cancellationToken)
    {
        try
        {
            var pageName = pageElement.GetProperty("name").GetString() ?? "Untitled Page";
            var pageSlug = pageElement.GetProperty("slug").GetString() ?? pageName.ToLowerInvariant().Replace(" ", "-");
            var pageDescription = pageElement.TryGetProperty("description", out var desc) 
                ? desc.GetString() 
                : null;

            var page = StorefrontPage.Create(
                storefront.TenantId,
                storefront.Id,
                pageName,
                pageSlug,
                displayOrder,
                pageDescription);

            // Clone sections into page
            if (pageElement.TryGetProperty("sections", out var sectionsElement) &&
                sectionsElement.ValueKind == JsonValueKind.Array)
            {
                int sectionOrder = 0;

                foreach (var sectionElement in sectionsElement.EnumerateArray())
                {
                    var section = CloneSection(sectionElement, page, sectionOrder, cancellationToken);
                    if (section != null)
                    {
                        page.AddSection(section);
                        sectionOrder++;
                    }
                }
            }

            return page;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning page from theme");
            return null;
        }
    }

    /// <summary>
    /// Clones a section from theme JSON element.
    /// </summary>
    private StorefrontSection? CloneSection(
        JsonElement sectionElement,
        StorefrontPage page,
        int displayOrder,
        CancellationToken cancellationToken)
    {
        try
        {
            var sectionName = sectionElement.GetProperty("name").GetString() ?? "Untitled Section";
            var sectionDescription = sectionElement.TryGetProperty("description", out var desc)
                ? desc.GetString()
                : null;

            var section = StorefrontSection.Create(
                page.TenantId,
                page.Id,
                sectionName,
                displayOrder,
                sectionDescription);

            // Extract and set styling if available
            if (sectionElement.TryGetProperty("backgroundColor", out var bgColor))
                section.Update(sectionName, sectionDescription, backgroundColor: bgColor.GetString());

            if (sectionElement.TryGetProperty("backgroundImageUrl", out var bgImage))
            {
                var currentName = section.Name;
                section.Update(currentName, sectionDescription, backgroundImageUrl: bgImage.GetString());
            }

            // Clone components into section
            if (sectionElement.TryGetProperty("components", out var componentsElement) &&
                componentsElement.ValueKind == JsonValueKind.Array)
            {
                int componentOrder = 0;

                foreach (var componentElement in componentsElement.EnumerateArray())
                {
                    var component = CloneComponent(componentElement, section, componentOrder, cancellationToken);
                    if (component != null)
                    {
                        section.AddComponent(component);
                        componentOrder++;
                    }
                }
            }

            return section;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning section from theme");
            return null;
        }
    }

    /// <summary>
    /// Clones a component from theme JSON element.
    /// </summary>
    private StorefrontComponent? CloneComponent(
        JsonElement componentElement,
        StorefrontSection section,
        int displayOrder,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract component type
            var componentTypeStr = componentElement.GetProperty("type").GetString() ?? "TextBlock";
            if (!Enum.TryParse<ComponentType>(componentTypeStr, true, out var componentType))
            {
                _logger.LogWarning("Unknown component type {ComponentType}", componentTypeStr);
                componentType = ComponentType.TextBlock; // Default to TextBlock
            }

            // Extract component configuration data
            object configData = ExtractComponentConfig(componentElement, componentType);

            // Create ComponentConfig value object
            var config = new ComponentConfig(componentType, configData);

            // Create StorefrontComponent
            var component = StorefrontComponent.Create(
                section.TenantId,
                section.Id,
                componentType,
                config,
                displayOrder);

            // Set optional CSS class if present
            if (componentElement.TryGetProperty("cssClass", out var cssClass))
                component.SetCssClass(cssClass.GetString());

            // Set optional tracking ID if present
            if (componentElement.TryGetProperty("trackingId", out var trackingId))
                component.SetTrackingId(trackingId.GetString());

            return component;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning component from theme");
            return null;
        }
    }

    /// <summary>
    /// Extracts component configuration data based on component type.
    /// </summary>
    private object ExtractComponentConfig(JsonElement componentElement, ComponentType componentType)
    {
        // If config object exists in JSON, use it; otherwise create default based on type
        if (componentElement.TryGetProperty("config", out var configElement))
        {
            return configElement.GetRawText();
        }

        // Return default configuration for component type
        return componentType switch
        {
            ComponentType.Hero => new
            {
                title = "Welcome",
                subtitle = "Your storefront awaits",
                ctaText = "Shop Now",
                ctaUrl = "/products",
                backgroundImageUrl = "https://via.placeholder.com/1920x400"
            },
            ComponentType.ProductGrid => new
            {
                productsPerPage = 12,
                columns = 4,
                showFilters = true
            },
            ComponentType.Banner => new
            {
                message = "Special Promotion",
                backgroundColor = "#ff0000"
            },
            ComponentType.ButtonBlock => new
            {
                text = "Click Here",
                url = "#",
                alignment = "center"
            },
            ComponentType.TextBlock => new
            {
                content = "Add your text here",
                alignment = "left",
                fontSize = "16px"
            },
            ComponentType.ImageBlock => new
            {
                imageUrl = "https://via.placeholder.com/600x400",
                altText = "Image",
                width = "100%"
            },
            ComponentType.VideoBlock => new
            {
                videoUrl = "https://placeholder-video.com/video.mp4",
                autoplay = false,
                controls = true
            },
            ComponentType.TestimonialsCarousel => new
            {
                testimonials = new object[] { },
                autorotate = true,
                interval = 5000
            },
            ComponentType.Newsletter => new
            {
                title = "Subscribe to our newsletter",
                placeholder = "Enter your email",
                buttonText = "Subscribe"
            },
            _ => new { /* default */ }
        };
    }
}
