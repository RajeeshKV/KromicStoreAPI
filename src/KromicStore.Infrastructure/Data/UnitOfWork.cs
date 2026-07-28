namespace KromicStore.Infrastructure.Data;

using Application.Interfaces;
using Domain.Entities;
using Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Unit of Work implementation for managing transactions and repositories.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ILoggerFactory _loggerFactory;
    private IRepository<Tenant>? _tenantRepository;
    private IRepository<User>? _userRepository;
    private IRepository<TenantAdmin>? _tenantAdminRepository;
    private IRepository<Product>? _productRepository;
    private IRepository<Category>? _categoryRepository;
    private IRepository<Customer>? _customerRepository;
    private IRepository<Courier>? _courierRepository;
    private IRepository<RazorpayConfiguration>? _razorpayConfigurationRepository;
    private IRepository<Order>? _orderRepository;
    private IRepository<TenantConfiguration>? _tenantConfigurationRepository;
    private IRepository<ConfigurationAuditLog>? _configurationAuditLogRepository;
    private IRepository<TenantPaymentMethod>? _tenantPaymentMethodRepository;
    private IRepository<Subscription>? _subscriptionRepository;
    private IThemeRepository? _themeRepository;
    private IStorefrontRepository? _storefrontRepository;
    private IRepository<TenantDomain>? _tenantDomainRepository;

    /// <summary>
    /// Initializes a new instance of the UnitOfWork class.
    /// </summary>
    public UnitOfWork(AppDbContext context, ILoggerFactory loggerFactory)
    {
        _context = context;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IRepository<Tenant> Tenants => _tenantRepository ??= new Repository<Tenant>(_context, _loggerFactory.CreateLogger<Repository<Tenant>>());

    /// <inheritdoc />
    public IRepository<User> Users => _userRepository ??= new Repository<User>(_context, _loggerFactory.CreateLogger<Repository<User>>());

    /// <inheritdoc />
    public IRepository<TenantAdmin> TenantAdmins => _tenantAdminRepository ??= new Repository<TenantAdmin>(_context, _loggerFactory.CreateLogger<Repository<TenantAdmin>>());

    /// <inheritdoc />
    public IRepository<Product> Products => _productRepository ??= new Repository<Product>(_context, _loggerFactory.CreateLogger<Repository<Product>>());

    /// <inheritdoc />
    public IRepository<Category> Categories => _categoryRepository ??= new Repository<Category>(_context, _loggerFactory.CreateLogger<Repository<Category>>());

    /// <inheritdoc />
    public IRepository<Customer> Customers => _customerRepository ??= new Repository<Customer>(_context, _loggerFactory.CreateLogger<Repository<Customer>>());

    /// <inheritdoc />
    public IRepository<Order> Orders => _orderRepository ??= new Repository<Order>(_context, _loggerFactory.CreateLogger<Repository<Order>>());

    /// <inheritdoc />
    public IRepository<TenantConfiguration> TenantConfigurations => _tenantConfigurationRepository ??= new Repository<TenantConfiguration>(_context, _loggerFactory.CreateLogger<Repository<TenantConfiguration>>());

    /// <inheritdoc />
    public IRepository<Courier> Couriers => _courierRepository ??= new Repository<Courier>(_context, _loggerFactory.CreateLogger<Repository<Courier>>());

    /// <inheritdoc />
    public IRepository<RazorpayConfiguration> RazorpayConfigurations => _razorpayConfigurationRepository ??= new Repository<RazorpayConfiguration>(_context, _loggerFactory.CreateLogger<Repository<RazorpayConfiguration>>());

    /// <inheritdoc />
    public IRepository<ConfigurationAuditLog> ConfigurationAuditLogs => _configurationAuditLogRepository ??= new Repository<ConfigurationAuditLog>(_context, _loggerFactory.CreateLogger<Repository<ConfigurationAuditLog>>());

    /// <inheritdoc />
    public IRepository<TenantPaymentMethod> TenantPaymentMethods => _tenantPaymentMethodRepository ??= new Repository<TenantPaymentMethod>(_context, _loggerFactory.CreateLogger<Repository<TenantPaymentMethod>>());

    /// <inheritdoc />
    public IRepository<Subscription> Subscriptions => _subscriptionRepository ??= new Repository<Subscription>(_context, _loggerFactory.CreateLogger<Repository<Subscription>>());

    /// <inheritdoc />
    public IThemeRepository Themes => _themeRepository ??= new ThemeRepository(_context, _loggerFactory.CreateLogger<ThemeRepository>());

    /// <inheritdoc />
    public IStorefrontRepository Storefronts => _storefrontRepository ??= new StorefrontRepository(_context, _loggerFactory.CreateLogger<StorefrontRepository>());

    /// <inheritdoc />
    public IRepository<TenantDomain> TenantDomains => _tenantDomainRepository ??= new Repository<TenantDomain>(_context, _loggerFactory.CreateLogger<Repository<TenantDomain>>());

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.RollbackTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
