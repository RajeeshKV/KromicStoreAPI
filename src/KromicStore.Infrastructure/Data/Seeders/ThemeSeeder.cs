namespace KromicStore.Infrastructure.Data.Seeders;

using System.Text.Json;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Seeds built-in themes into the database.
/// </summary>
public class ThemeSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<ThemeSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the ThemeSeeder class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public ThemeSeeder(AppDbContext context, ILogger<ThemeSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds default themes into the database if they don't already exist.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Check if themes already exist
        var existingThemes = await _context.Themes.ToListAsync(cancellationToken);
        
        if (existingThemes.Any())
        {
            _logger.LogInformation("Themes already exist in database. Skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding themes into database...");

        var themes = new List<Theme>
        {
            CreateMinimalTheme(),
            CreateModernTheme(),
            CreateProTheme()
        };

        await _context.Themes.AddRangeAsync(themes, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Themes seeded successfully. Added {Count} themes.", themes.Count);
    }

    /// <summary>
    /// Creates the Minimal theme with clean, modern design.
    /// </summary>
    /// <returns>The Minimal platform theme.</returns>
    private static Theme CreateMinimalTheme()
    {
        var definition = new
        {
            id = "minimal",
            slug = "minimal",
            name = "Minimal",
            description = "Clean, modern storefront with a minimalist design approach",
            version = "1.0.0"
        };

        var definitionJson = JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = true });

        return Theme.CreatePlatformTheme(
            slug: "minimal",
            name: "Minimal",
            description: "Clean, modern storefront with a minimalist design approach",
            version: "1.0.0",
            definitionJson: definitionJson,
            createdByUserId: Guid.Empty);
    }

    /// <summary>
    /// Creates the Modern theme with advanced layouts and features.
    /// </summary>
    /// <returns>The Modern platform theme.</returns>
    private static Theme CreateModernTheme()
    {
        var definition = new
        {
            id = "modern",
            slug = "modern",
            name = "Modern",
            description = "Contemporary storefront with advanced layouts, featured products, and testimonials",
            version = "1.0.0"
        };

        var definitionJson = JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = true });

        return Theme.CreatePlatformTheme(
            slug: "modern",
            name: "Modern",
            description: "Contemporary storefront with advanced layouts, featured products, and testimonials",
            version: "1.0.0",
            definitionJson: definitionJson,
            createdByUserId: Guid.Empty);
    }

    /// <summary>
    /// Creates the Pro theme with enterprise-level features.
    /// </summary>
    /// <returns>The Pro platform theme.</returns>
    private static Theme CreateProTheme()
    {
        var definition = new
        {
            id = "pro",
            slug = "pro",
            name = "Pro",
            description = "Professional enterprise theme with advanced layouts, FAQs, category sections, and comprehensive features",
            version = "1.0.0"
        };

        var definitionJson = JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = true });

        return Theme.CreatePlatformTheme(
            slug: "pro",
            name: "Pro",
            description: "Professional enterprise theme with advanced layouts, FAQs, category sections, and comprehensive features",
            version: "1.0.0",
            definitionJson: definitionJson,
            createdByUserId: Guid.Empty);
    }
}
