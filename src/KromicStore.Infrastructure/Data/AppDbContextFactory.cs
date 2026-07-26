namespace KromicStore.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time DbContext factory for Entity Framework Core migrations.
/// This factory creates a DbContext instance without requiring:
/// - A running database
/// - Environment variables
/// - Dependency injection container
/// 
/// This allows migrations to be generated with: dotnet ef migrations add <MigrationName>
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Creates a DbContext instance for design-time operations.
    /// </summary>
    /// <param name="args">Command line arguments (not used but required by interface).</param>
    /// <returns>A configured AppDbContext instance.</returns>
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Use a dummy PostgreSQL connection string for design-time
        // This doesn't need to actually connect - it's only used for schema generation
        // The connection string format is valid but points to a non-existent database
        var connectionString = "postgresql://user:password@localhost:5432/dummy";

        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
