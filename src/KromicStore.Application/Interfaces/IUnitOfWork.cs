namespace KromicStore.Application.Interfaces;

using Domain.Entities;

/// <summary>
/// Unit of Work pattern for transaction management.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the tenant repository.
    /// </summary>
    IRepository<Tenant> Tenants { get; }

    /// <summary>
    /// Gets the user repository.
    /// </summary>
    IRepository<User> Users { get; }

    /// <summary>
    /// Gets the product repository.
    /// </summary>
    IRepository<Product> Products { get; }

    /// <summary>
    /// Gets the category repository.
    /// </summary>
    IRepository<Category> Categories { get; }

    /// <summary>
    /// Gets the customer repository.
    /// </summary>
    IRepository<Customer> Customers { get; }

    /// <summary>
    /// Gets the order repository.
    /// </summary>
    IRepository<Order> Orders { get; }

    /// <summary>
    /// Gets the tenant configuration repository.
    /// </summary>
    IRepository<TenantConfiguration> TenantConfigurations { get; }

    /// <summary>
    /// Gets the configuration audit log repository.
    /// </summary>
    IRepository<ConfigurationAuditLog> ConfigurationAuditLogs { get; }

    /// <summary>
    /// Gets the subscription repository.
    /// </summary>
    IRepository<Subscription> Subscriptions { get; }

    /// <summary>
    /// Gets the theme repository.
    /// </summary>
    IThemeRepository Themes { get; }

    /// <summary>
    /// Gets the storefront repository.
    /// </summary>
    IStorefrontRepository Storefronts { get; }

    /// <summary>
    /// Commits all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a transaction.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
