namespace KromicStore.Domain.ValueObjects;

using System.Text.Json;
using Enums;

/// <summary>
/// Represents configuration for a storefront component.
/// Stores the component type and JSON-serialized configuration data.
/// </summary>
public record ComponentConfig
{
    /// <summary>
    /// Gets the type of component.
    /// </summary>
    public ComponentType Type { get; init; }

    /// <summary>
    /// Gets the JSON-serialized configuration data.
    /// </summary>
    public string ConfigJson { get; init; } = string.Empty;

    /// <summary>
    /// Creates a new instance of ComponentConfig.
    /// </summary>
    /// <param name="type">The component type.</param>
    /// <param name="configData">The configuration data object to be serialized.</param>
    /// <exception cref="ArgumentException">Thrown when type is invalid or configData is null.</exception>
    public ComponentConfig(ComponentType type, object? configData)
    {
        Type = type;
        ConfigJson = configData != null ? JsonSerializer.Serialize(configData) : "{}";
    }

    /// <summary>
    /// Deserializes the configuration JSON to specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized configuration object, or default if JSON is empty.</returns>
    public T? GetConfig<T>() where T : class
    {
        if (string.IsNullOrWhiteSpace(ConfigJson) || ConfigJson == "{}")
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(ConfigJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a ComponentConfig for Hero component type.
    /// </summary>
    public static ComponentConfig CreateHero(string title, string? subtitle = null, string? ctaText = null, string? ctaUrl = null, string? backgroundImageUrl = null)
    {
        var config = new
        {
            title,
            subtitle,
            ctaText,
            ctaUrl,
            backgroundImageUrl
        };
        return new ComponentConfig(ComponentType.Hero, config);
    }

    /// <summary>
    /// Creates a ComponentConfig for Banner component type.
    /// </summary>
    public static ComponentConfig CreateBanner(string title, string? message = null, string? bannerColor = null)
    {
        var config = new
        {
            title,
            message,
            bannerColor
        };
        return new ComponentConfig(ComponentType.Banner, config);
    }

    /// <summary>
    /// Creates a ComponentConfig for ProductGrid component type.
    /// </summary>
    public static ComponentConfig CreateProductGrid(int columns = 4, int itemsPerPage = 12, string? categoryId = null)
    {
        var config = new
        {
            columns,
            itemsPerPage,
            categoryId
        };
        return new ComponentConfig(ComponentType.ProductGrid, config);
    }

    /// <summary>
    /// Creates a ComponentConfig for CategoryGrid component type.
    /// </summary>
    public static ComponentConfig CreateCategoryGrid(int columns = 4)
    {
        var config = new { columns };
        return new ComponentConfig(ComponentType.CategoryGrid, config);
    }

    /// <summary>
    /// Creates a ComponentConfig for Newsletter component type.
    /// </summary>
    public static ComponentConfig CreateNewsletter(string title, string? subtitle = null, string? buttonText = null)
    {
        var config = new
        {
            title,
            subtitle,
            buttonText
        };
        return new ComponentConfig(ComponentType.Newsletter, config);
    }

    /// <summary>
    /// Creates a ComponentConfig for TextBlock component type.
    /// </summary>
    public static ComponentConfig CreateTextBlock(string content, string? alignment = null)
    {
        var config = new
        {
            content,
            alignment
        };
        return new ComponentConfig(ComponentType.TextBlock, config);
    }

    /// <summary>
    /// Creates a ComponentConfig for ImageBlock component type.
    /// </summary>
    public static ComponentConfig CreateImageBlock(string imageUrl, string? caption = null, string? altText = null)
    {
        var config = new
        {
            imageUrl,
            caption,
            altText
        };
        return new ComponentConfig(ComponentType.ImageBlock, config);
    }

    /// <summary>
    /// Creates a ComponentConfig for ButtonBlock component type.
    /// </summary>
    public static ComponentConfig CreateButtonBlock(string text, string url, string? style = null, string? size = null)
    {
        var config = new
        {
            text,
            url,
            style,
            size
        };
        return new ComponentConfig(ComponentType.ButtonBlock, config);
    }
}
