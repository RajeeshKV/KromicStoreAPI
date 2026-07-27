namespace KromicStore.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using KromicStore.Domain.Entities;

/// <summary>
/// Entity Framework Core DbContext for KromicStore.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the Tenants table.
    /// </summary>
    public DbSet<Tenant> Tenants { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Users table.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the refresh token sessions table.
    /// </summary>
    public DbSet<AuthRefreshToken> AuthRefreshTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Products table.
    /// </summary>
    public DbSet<Product> Products { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Customers table.
    /// </summary>
    public DbSet<Customer> Customers { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Orders table.
    /// </summary>
    public DbSet<Order> Orders { get; set; } = null!;

    /// <summary>
    /// Gets or sets the OrderItems table.
    /// </summary>
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the WebhookConfigurations table.
    /// </summary>
    public DbSet<WebhookConfiguration> WebhookConfigurations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the WebhookEventLogs table.
    /// </summary>
    public DbSet<WebhookEventLog> WebhookEventLogs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the WebhookDeliveryLogs table.
    /// </summary>
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the TenantConfigurations table.
    /// </summary>
    public DbSet<TenantConfiguration> TenantConfigurations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ConfigurationAuditLogs table.
    /// </summary>
    public DbSet<ConfigurationAuditLog> ConfigurationAuditLogs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the comprehensive AuditLogs table.
    /// </summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the TeamInvitations table.
    /// </summary>
    public DbSet<TeamInvitation> TeamInvitations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the FeatureFlags table.
    /// </summary>
    public DbSet<FeatureFlag> FeatureFlags { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Notifications table.
    /// </summary>
    public DbSet<Notification> Notifications { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ApiKeys table.
    /// </summary>
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CustomerAddresses table.
    /// </summary>
    public DbSet<CustomerAddress> CustomerAddresses { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Wishlists table.
    /// </summary>
    public DbSet<Wishlist> Wishlists { get; set; } = null!;

    /// <summary>
    /// Gets or sets the CustomerGroups table.
    /// </summary>
    public DbSet<CustomerGroup> CustomerGroups { get; set; } = null!;

    /// <summary>
    /// Gets or sets the TenantUsage table.
    /// </summary>
    public DbSet<TenantUsage> TenantUsage { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Categories table.
    /// </summary>
    public DbSet<Category> Categories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Payments table.
    /// </summary>
    public DbSet<Payment> Payments { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PaymentTransactions table.
    /// </summary>
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Subscriptions table.
    /// </summary>
    public DbSet<Subscription> Subscriptions { get; set; } = null!;

    /// <summary>Gets the tenant payment methods DbSet.</summary>
    public DbSet<TenantPaymentMethod> TenantPaymentMethods { get; set; } = null!;

    /// <summary>Gets the Razorpay subscription events DbSet (audit log).</summary>
    public DbSet<RazorpaySubscriptionEvent> RazorpaySubscriptionEvents { get; set; } = null!;

    /// <summary>Gets the order payments DbSet.</summary>
    public DbSet<OrderPayment> OrderPayments { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Themes table (platform-wide themes, not tenant-scoped).
    /// </summary>
    public DbSet<ThemeEntity> Themes { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SuperUsers table (platform admins, separate from tenant users).
    /// </summary>
    public DbSet<SuperUser> SuperUsers { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SuperUserConfigs table (platform-wide configuration).
    /// </summary>
    public DbSet<SuperUserConfig> SuperUserConfigs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the TenantDomains table (supports multiple domains per tenant).
    /// </summary>
    public DbSet<TenantDomain> TenantDomains { get; set; } = null!;

    /// <summary>
    /// Gets or sets the TenantThemes table (tenant-specific theme configurations).
    /// </summary>
    public DbSet<TenantTheme> TenantThemes { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Storefronts table.
    /// </summary>
    public DbSet<Storefront> Storefronts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the StorefrontPages table.
    /// </summary>
    public DbSet<StorefrontPage> StorefrontPages { get; set; } = null!;

    /// <summary>
    /// Gets or sets the StorefrontSections table.
    /// </summary>
    public DbSet<StorefrontSection> StorefrontSections { get; set; } = null!;

    /// <summary>
    /// Gets or sets the StorefrontComponents table.
    /// </summary>
    public DbSet<StorefrontComponent> StorefrontComponents { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the AppDbContext class.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures the model on model creation.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Tenant entity
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsArchived).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.LifecycleReason).HasMaxLength(1000);
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.HasIndex(e => e.ContactEmail);
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
        });


        // Configure AuthRefreshToken entity
        modelBuilder.Entity<AuthRefreshToken>(entity =>
        {
            entity.ToTable("AuthRefreshTokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrincipalId).IsRequired();
            entity.Property(e => e.PrincipalType).IsRequired().HasMaxLength(32);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(128);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.PrincipalId, e.PrincipalType, e.RevokedAt, e.ExpiresAt });
        });
        // Configure SuperUser entity
        modelBuilder.Entity<SuperUser>(entity =>
        {
            entity.ToTable("SuperUsers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure SuperUserConfig entity
        modelBuilder.Entity<SuperUserConfig>(entity =>
        {
            entity.ToTable("SuperUserConfigs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfigKey).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ConfigValue).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.ConfigKey).IsUnique();
        });

        // Configure TenantDomain entity
        modelBuilder.Entity<TenantDomain>(entity =>
        {
            entity.ToTable("TenantDomains");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsPrimary).IsRequired();
            entity.Property(e => e.IsVerified).IsRequired();
            entity.Property(e => e.VerificationToken).IsRequired().HasMaxLength(128);
            entity.Property(e => e.SslStatus).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.TenantId, e.Domain }).IsUnique();
            entity.HasIndex(e => e.Domain);
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId);
            entity.Property(e => e.UserId);
            entity.Property(e => e.UserType).HasMaxLength(32);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.OldState);
            entity.Property(e => e.NewState);
            entity.Property(e => e.Metadata);
            entity.Property(e => e.Success).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.OccurredAt).IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.CorrelationId);
        });

        // Configure TeamInvitation entity
        modelBuilder.Entity<TeamInvitation>(entity =>
        {
            entity.ToTable("TeamInvitations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.AcceptedAt);
            entity.Property(e => e.InvitedBy).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => new { e.TenantId, e.Email });
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FeatureFlag entity
        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.ToTable("FeatureFlags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).IsRequired();
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Plan).HasMaxLength(50);
            entity.HasIndex(e => new { e.TenantId, e.Key }).IsUnique();
            entity.HasIndex(e => e.Key);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Plan);
        });

        // Configure Notification entity
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId);
            entity.Property(e => e.RecipientId);
            entity.Property(e => e.RecipientEmail).HasMaxLength(255);
            entity.Property(e => e.RecipientPhone).HasMaxLength(50);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
            entity.Property(e => e.TemplateKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.Body).IsRequired();
            entity.Property(e => e.Data);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.RetryCount).IsRequired();
            entity.Property(e => e.SentAt);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.ScheduledAt);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.RecipientId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.ScheduledAt);
        });

        // Configure ApiKey entity
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("ApiKeys");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.KeyPrefix).IsRequired().HasMaxLength(4);
            entity.Property(e => e.Scopes).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ExpiresAt);
            entity.Property(e => e.LastUsedAt);
            entity.Property(e => e.CreatedBy).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.KeyHash });
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CustomerAddress entity
        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.ToTable("CustomerAddresses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.AddressType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.IsDefault).IsRequired();
            entity.Property(e => e.Label).HasMaxLength(100);
            entity.OwnsOne(e => e.Address, a =>
            {
                a.Property(p => p.Street).IsRequired().HasMaxLength(255);
                a.Property(p => p.City).IsRequired().HasMaxLength(100);
                a.Property(p => p.State).IsRequired().HasMaxLength(100);
                a.Property(p => p.PostalCode).IsRequired().HasMaxLength(20);
                a.Property(p => p.Country).IsRequired().HasMaxLength(100);
            });
            entity.HasIndex(e => new { e.CustomerId, e.AddressType, e.IsDefault });
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Wishlist entity
        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.ToTable("Wishlists");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.AddedAt).IsRequired();
            entity.HasIndex(e => new { e.CustomerId, e.ProductId }).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ProductId);
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CustomerGroup entity
        modelBuilder.Entity<CustomerGroup>(entity =>
        {
            entity.ToTable("CustomerGroups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DiscountPercentage).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TenantUsage entity
        modelBuilder.Entity<TenantUsage>(entity =>
        {
            entity.ToTable("TenantUsage");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.UsageType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.Unit).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PeriodStart).IsRequired();
            entity.Property(e => e.PeriodEnd).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.UsageType, e.PeriodStart, e.PeriodEnd });
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.PeriodStart);
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TenantTheme entity
        modelBuilder.Entity<TenantTheme>(entity =>
        {
            entity.ToTable("TenantThemes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PrimaryColor).IsRequired().HasMaxLength(7);
            entity.Property(e => e.SecondaryColor).IsRequired().HasMaxLength(7);
            entity.Property(e => e.AccentColor).IsRequired().HasMaxLength(7);
            entity.Property(e => e.BackgroundColor).IsRequired().HasMaxLength(7);
            entity.Property(e => e.TextColor).IsRequired().HasMaxLength(7);
            entity.Property(e => e.FontFamily).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BorderRadius).IsRequired();
            entity.Property(e => e.SpacingUnit).IsRequired();
            entity.Property(e => e.ComponentOverrides).IsRequired();
            entity.Property(e => e.LayoutOptions).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(5000);
            entity.Property(e => e.ReorderLevel).IsRequired();
            entity.OwnsOne(e => e.Price);
            entity.OwnsOne(e => e.CostPrice);
            entity.HasIndex(e => new { e.TenantId, e.Sku }).IsUnique();
        });

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.OwnsOne(e => e.BillingAddress);
            entity.OwnsOne(e => e.ShippingAddress);
            entity.OwnsOne(e => e.LifetimeValue);
            entity.HasIndex(e => new { e.TenantId, e.Email });
        });

        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.OwnsOne(e => e.Subtotal);
            entity.OwnsOne(e => e.TaxAmount);
            entity.OwnsOne(e => e.ShippingCost);
            entity.OwnsOne(e => e.Total);
            entity.OwnsOne(e => e.ShippingAddress);
            entity.OwnsOne(e => e.BillingAddress);
            entity.HasMany<OrderItem>().WithOne().HasForeignKey("OrderId");
            entity.HasIndex(e => new { e.TenantId, e.OrderNumber }).IsUnique();
        });

        // Configure OrderItem entity
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.OwnsOne(e => e.UnitPrice);
            entity.OwnsOne(e => e.TotalPrice);
        });

        // Configure WebhookConfiguration entity
        modelBuilder.Entity<WebhookConfiguration>(entity =>
        {
            entity.ToTable("WebhookConfigurations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.EndpointUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Secret).IsRequired();
            entity.Property(e => e.AuthenticationHeader).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EventTypes).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => (KromicStore.Domain.Enums.WebhookEventType)int.Parse(s))
                    .ToList());
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
        });

        // Configure WebhookEventLog entity
        modelBuilder.Entity<WebhookEventLog>(entity =>
        {
            entity.ToTable("WebhookEventLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.EventId).IsRequired();
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.EventType });
            entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }).IsUnique();
        });

        // Configure WebhookDeliveryLog entity
        modelBuilder.Entity<WebhookDeliveryLog>(entity =>
        {
            entity.ToTable("WebhookDeliveryLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WebhookConfigurationId).IsRequired();
            entity.Property(e => e.WebhookEventLogId).IsRequired();
            entity.Property(e => e.Response).HasMaxLength(1000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.HasIndex(e => e.WebhookConfigurationId);
            entity.HasIndex(e => e.WebhookEventLogId);
            entity.HasIndex(e => new { e.WebhookConfigurationId, e.CreatedAt });
            entity.HasIndex(e => new { e.NextRetryAt }).HasFilter("\"NextRetryAt\" IS NOT NULL");
        });

        // Configure TenantConfiguration entity
        modelBuilder.Entity<TenantConfiguration>(entity =>
        {
            entity.ToTable("TenantConfigurations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfigKey).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ConfigValue).IsRequired();
            entity.Property(e => e.Scope).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.ConfigKey });
        });

        // Configure ConfigurationAuditLog entity
        modelBuilder.Entity<ConfigurationAuditLog>(entity =>
        {
            entity.ToTable("ConfigurationAuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.ConfigurationKey).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ChangedAt).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.ChangedAt });
        });

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.NestingLevel).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.ParentCategoryId })
                .HasDatabaseName("IX_Categories_TenantId_ParentCategoryId");
            entity.HasIndex(e => new { e.TenantId, e.DisplayOrder })
                .HasDatabaseName("IX_Categories_TenantId_DisplayOrder");
        });

        // Configure Payment entity
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.OrderId).IsRequired();
            entity.OwnsOne(e => e.Amount);
            entity.Property(e => e.ExternalPaymentId).HasMaxLength(255);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.HasMany<PaymentTransaction>().WithOne().HasForeignKey("PaymentId");
            entity.HasIndex(e => new { e.TenantId, e.OrderId }).IsUnique()
                .HasDatabaseName("IX_Payments_TenantId_OrderId");
            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("IX_Payments_TenantId_Status");
        });

        // Configure PaymentTransaction entity
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentId).IsRequired();
            entity.OwnsOne(e => e.Amount);
            entity.Property(e => e.TransactionType).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ExternalTransactionId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.PaymentId)
                .HasDatabaseName("IX_PaymentTransactions_PaymentId");
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_PaymentTransactions_CreatedAt");
        });

        // Configure Subscription entity
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("Subscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.PlanType).IsRequired();
            entity.OwnsOne(e => e.MonthlyPrice);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.BillingCycleDay).IsRequired();
            entity.Property(e => e.RazorpaySubscriptionId).HasMaxLength(100);
            entity.Property(e => e.RazorpayCustomerId).HasMaxLength(100);
            entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.FailedPaymentCount).IsRequired().HasDefaultValue(0);
            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("IX_Subscriptions_TenantId_Status");
            entity.HasIndex(e => new { e.TenantId, e.PlanType })
                .HasDatabaseName("IX_Subscriptions_TenantId_PlanType");
            entity.HasIndex(e => e.RazorpaySubscriptionId);
            entity.HasIndex(e => new { e.TenantId, e.PaymentStatus });
        });

        // Configure TenantPaymentMethod
        modelBuilder.Entity<TenantPaymentMethod>(entity =>
        {
            entity.ToTable("TenantPaymentMethods");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EncryptedApiKey).IsRequired();
            entity.Property(e => e.EncryptedApiSecret).IsRequired();
            entity.Property(e => e.EncryptedWebhookSecret).IsRequired();
            entity.Property(e => e.IsEnabled).IsRequired();
            entity.Property(e => e.TestModeEnabled).IsRequired();

            // Indexes
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Provider }).IsUnique();

            // Foreign key to Tenant
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RazorpaySubscriptionEvent
        modelBuilder.Entity<RazorpaySubscriptionEvent>(entity =>
        {
            entity.ToTable("RazorpaySubscriptionEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubscriptionId).IsRequired();
            entity.Property(e => e.RazorpaySubscriptionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RazorpayEventId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventData).IsRequired();
            entity.Property(e => e.ProcessedAt).IsRequired();

            // Indexes for audit trail and idempotency
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.RazorpayEventId).IsUnique();
            entity.HasIndex(e => e.EventType);

            // Foreign key
            entity.HasOne<Subscription>()
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure OrderPayment
        modelBuilder.Entity<OrderPayment>(entity =>
        {
            entity.ToTable("OrderPayments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.RazorpayOrderId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RazorpayPaymentId).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

            // Amount as value object
            entity.OwnsOne(e => e.Amount, ownedBuilder =>
            {
                ownedBuilder.Property(m => m.Amount).HasColumnName("Amount").IsRequired();
                ownedBuilder.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
            });

            // Indexes
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.RazorpayOrderId).IsUnique();
            entity.HasIndex(e => e.Status);

            // Foreign keys
            entity.HasOne<Order>()
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ThemeEntity
        modelBuilder.Entity<ThemeEntity>(entity =>
        {
            entity.ToTable("Themes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DefinitionJson).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("UX_Themes_Slug");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Themes_IsActive");
        });

        // Configure Storefront entity
        modelBuilder.Entity<Storefront>(entity =>
        {
            entity.ToTable("Storefronts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ThemeId);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.ContactEmail).HasMaxLength(255);
            entity.Property(e => e.ContactPhone).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.BrandColor).HasMaxLength(7); // Hex color
            entity.Property(e => e.Copyright).HasMaxLength(500);
            entity.OwnsOne(e => e.MandatoryFields);
            entity.HasMany<StorefrontPage>().WithOne().HasForeignKey("StorefrontId");
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_Storefronts_TenantId");
            entity.HasIndex(e => new { e.TenantId, e.Status }).HasDatabaseName("IX_Storefronts_TenantId_Status");
        });

        // Configure StorefrontPage entity
        modelBuilder.Entity<StorefrontPage>(entity =>
        {
            entity.ToTable("StorefrontPages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StorefrontId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
            entity.Property(e => e.LayoutType).HasMaxLength(100);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.HasMany<StorefrontSection>().WithOne().HasForeignKey("PageId");
            entity.HasIndex(e => e.StorefrontId).HasDatabaseName("IX_StorefrontPages_StorefrontId");
            entity.HasIndex(e => new { e.StorefrontId, e.Slug }).IsUnique().HasDatabaseName("UX_StorefrontPages_StorefrontId_Slug");
        });

        // Configure StorefrontSection entity
        modelBuilder.Entity<StorefrontSection>(entity =>
        {
            entity.ToTable("StorefrontSections");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PageId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.HasMany<StorefrontComponent>().WithOne().HasForeignKey("SectionId");
            entity.HasIndex(e => e.PageId).HasDatabaseName("IX_StorefrontSections_PageId");
        });

        // Configure StorefrontComponent entity
        modelBuilder.Entity<StorefrontComponent>(entity =>
        {
            entity.ToTable("StorefrontComponents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SectionId).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.IsVisible).IsRequired();
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.CssClass).HasMaxLength(500);
            entity.Property(e => e.TrackingId).HasMaxLength(255);
            entity.OwnsOne(e => e.Config, owned =>
            {
                owned.Property(c => c.Type).IsRequired();
                owned.Property(c => c.ConfigJson).IsRequired();
            });
            entity.HasIndex(e => e.SectionId).HasDatabaseName("IX_StorefrontComponents_SectionId");
        });

        // Add comprehensive performance indexes
        ConfigurePerformanceIndexes(modelBuilder);
    }

    /// <summary>
    /// Configures performance indexes for optimal query execution.
    /// </summary>
    private static void ConfigurePerformanceIndexes(ModelBuilder modelBuilder)
    {
        // Tenant Indexes
        modelBuilder.Entity<Tenant>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Tenants_CreatedAt");

        // User Indexes - Tenant isolation and email lookups
        modelBuilder.Entity<User>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_Users_TenantId_Id");

        // Product Indexes - Core query patterns
        modelBuilder.Entity<Product>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_Products_TenantId_Id");

        modelBuilder.Entity<Product>()
            .HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Products_TenantId_Status");

        modelBuilder.Entity<Product>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Products_CreatedAt");

        modelBuilder.Entity<Product>()
            .HasIndex(e => e.UpdatedAt)
            .HasDatabaseName("IX_Products_UpdatedAt");

        modelBuilder.Entity<Product>()
            .HasIndex(e => new { e.TenantId, e.CategoryId })
            .HasDatabaseName("IX_Products_TenantId_CategoryId");

        // Product partial index: only active/published products
        modelBuilder.Entity<Product>()
            .HasIndex(e => new { e.TenantId, e.Status })
            .HasFilter("\"Status\" IN (0, 1)") // Draft, Active
            .HasDatabaseName("IX_Products_TenantId_Status_Active");

        // Customer Indexes
        modelBuilder.Entity<Customer>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_Customers_TenantId_Id");

        modelBuilder.Entity<Customer>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Customers_CreatedAt");

        modelBuilder.Entity<Customer>()
            .HasIndex(e => e.UpdatedAt)
            .HasDatabaseName("IX_Customers_UpdatedAt");

        // Order Indexes - Status and temporal queries
        modelBuilder.Entity<Order>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_Orders_TenantId_Id");

        modelBuilder.Entity<Order>()
            .HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Orders_TenantId_Status");

        modelBuilder.Entity<Order>()
            .HasIndex(e => new { e.TenantId, e.CustomerId })
            .HasDatabaseName("IX_Orders_TenantId_CustomerId");

        modelBuilder.Entity<Order>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Orders_CreatedAt");

        modelBuilder.Entity<Order>()
            .HasIndex(e => e.UpdatedAt)
            .HasDatabaseName("IX_Orders_UpdatedAt");

        // Order partial index: orders requiring action
        modelBuilder.Entity<Order>()
            .HasIndex(e => new { e.TenantId, e.Status })
            .HasFilter("\"Status\" IN (0, 2, 3)") // Pending, Processing, Shipped
            .HasDatabaseName("IX_Orders_TenantId_Status_Active");

        // OrderItem Indexes
        modelBuilder.Entity<OrderItem>()
            .HasIndex(e => e.Id)
            .HasDatabaseName("IX_OrderItems_Id");

        // WebhookConfiguration Indexes
        modelBuilder.Entity<WebhookConfiguration>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_WebhookConfigurations_TenantId_Id");

        modelBuilder.Entity<WebhookConfiguration>()
            .HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_WebhookConfigurations_TenantId_IsActive");

        // WebhookEventLog Indexes
        modelBuilder.Entity<WebhookEventLog>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_WebhookEventLogs_TenantId_Id");

        modelBuilder.Entity<WebhookEventLog>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_WebhookEventLogs_CreatedAt");

        // WebhookDeliveryLog Indexes - Retry and temporal queries
        modelBuilder.Entity<WebhookDeliveryLog>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_WebhookDeliveryLogs_CreatedAt");

        modelBuilder.Entity<WebhookDeliveryLog>()
            .HasIndex(e => new { e.WebhookConfigurationId, e.CreatedAt })
            .HasDatabaseName("IX_WebhookDeliveryLogs_WebhookConfigurationId_CreatedAt");

        modelBuilder.Entity<WebhookDeliveryLog>()
            .HasIndex(e => new { e.NextRetryAt })
            .HasFilter("\"NextRetryAt\" IS NOT NULL")
            .HasDatabaseName("IX_WebhookDeliveryLogs_NextRetryAt_Pending");

        // TenantConfiguration Indexes
        modelBuilder.Entity<TenantConfiguration>()
            .HasIndex(e => new { e.TenantId, e.ConfigKey })
            .HasDatabaseName("IX_TenantConfigurations_TenantId_ConfigKey");

        modelBuilder.Entity<TenantConfiguration>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_TenantConfigurations_CreatedAt");

        // ConfigurationAuditLog Indexes
        modelBuilder.Entity<ConfigurationAuditLog>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_ConfigurationAuditLogs_TenantId_Id");

        modelBuilder.Entity<ConfigurationAuditLog>()
            .HasIndex(e => new { e.TenantId, e.ChangedAt })
            .HasDatabaseName("IX_ConfigurationAuditLogs_TenantId_ChangedAt");

        modelBuilder.Entity<ConfigurationAuditLog>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_ConfigurationAuditLogs_CreatedAt");

        // Category Indexes
        modelBuilder.Entity<Category>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_Categories_TenantId_Id");

        modelBuilder.Entity<Category>()
            .HasIndex(e => new { e.TenantId, e.ParentCategoryId })
            .HasDatabaseName("IX_Categories_TenantId_ParentCategoryId");

        modelBuilder.Entity<Category>()
            .HasIndex(e => new { e.TenantId, e.DisplayOrder })
            .HasDatabaseName("IX_Categories_TenantId_DisplayOrder");

        // Payment Indexes
        modelBuilder.Entity<Payment>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_Payments_TenantId_Id");

        modelBuilder.Entity<Payment>()
            .HasIndex(e => new { e.TenantId, e.OrderId })
            .HasDatabaseName("IX_Payments_TenantId_OrderId");

        modelBuilder.Entity<Payment>()
            .HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Payments_TenantId_Status");

        modelBuilder.Entity<Payment>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Payments_CreatedAt");

        // PaymentTransaction Indexes
        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(e => e.PaymentId)
            .HasDatabaseName("IX_PaymentTransactions_PaymentId");

        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_PaymentTransactions_CreatedAt");

        // Subscription Indexes
        modelBuilder.Entity<Subscription>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_Subscriptions_TenantId_Id");

        modelBuilder.Entity<Subscription>()
            .HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Subscriptions_TenantId_Status");

        modelBuilder.Entity<Subscription>()
            .HasIndex(e => new { e.TenantId, e.PlanType })
            .HasDatabaseName("IX_Subscriptions_TenantId_PlanType");

        modelBuilder.Entity<Subscription>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Subscriptions_CreatedAt");

        modelBuilder.Entity<Subscription>()
            .HasIndex(e => e.RazorpaySubscriptionId)
            .HasDatabaseName("IX_Subscriptions_RazorpaySubscriptionId");

        modelBuilder.Entity<Subscription>()
            .HasIndex(e => new { e.TenantId, e.PaymentStatus })
            .HasDatabaseName("IX_Subscriptions_TenantId_PaymentStatus");

        // TenantPaymentMethod Indexes
        modelBuilder.Entity<TenantPaymentMethod>()
            .HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_TenantPaymentMethods_TenantId");

        modelBuilder.Entity<TenantPaymentMethod>()
            .HasIndex(e => new { e.TenantId, e.Provider })
            .IsUnique()
            .HasDatabaseName("UX_TenantPaymentMethods_TenantId_Provider");

        modelBuilder.Entity<TenantPaymentMethod>()
            .HasIndex(e => e.IsEnabled)
            .HasDatabaseName("IX_TenantPaymentMethods_IsEnabled");

        // RazorpaySubscriptionEvent Indexes
        modelBuilder.Entity<RazorpaySubscriptionEvent>()
            .HasIndex(e => e.SubscriptionId)
            .HasDatabaseName("IX_RazorpaySubscriptionEvents_SubscriptionId");

        modelBuilder.Entity<RazorpaySubscriptionEvent>()
            .HasIndex(e => e.RazorpayEventId)
            .IsUnique()
            .HasDatabaseName("UX_RazorpaySubscriptionEvents_RazorpayEventId");

        modelBuilder.Entity<RazorpaySubscriptionEvent>()
            .HasIndex(e => e.EventType)
            .HasDatabaseName("IX_RazorpaySubscriptionEvents_EventType");

        modelBuilder.Entity<RazorpaySubscriptionEvent>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_RazorpaySubscriptionEvents_CreatedAt");

        // OrderPayment Indexes
        modelBuilder.Entity<OrderPayment>()
            .HasIndex(e => e.OrderId)
            .HasDatabaseName("IX_OrderPayments_OrderId");

        modelBuilder.Entity<OrderPayment>()
            .HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_OrderPayments_TenantId");

        modelBuilder.Entity<OrderPayment>()
            .HasIndex(e => e.RazorpayOrderId)
            .IsUnique()
            .HasDatabaseName("UX_OrderPayments_RazorpayOrderId");

        modelBuilder.Entity<OrderPayment>()
            .HasIndex(e => e.Status)
            .HasDatabaseName("IX_OrderPayments_Status");

        modelBuilder.Entity<OrderPayment>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_OrderPayments_CreatedAt");

        modelBuilder.Entity<OrderPayment>()
            .HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_OrderPayments_TenantId_Status");

        // Theme Indexes (Platform-wide, not tenant-scoped)
        modelBuilder.Entity<ThemeEntity>()
            .HasIndex(e => e.Slug)
            .IsUnique()
            .HasDatabaseName("UX_Themes_Slug");

        modelBuilder.Entity<ThemeEntity>()
            .HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Themes_IsActive");

        modelBuilder.Entity<ThemeEntity>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Themes_CreatedAt");

        // Storefront Indexes
        modelBuilder.Entity<Storefront>()
            .HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_Storefronts_TenantId_Id");

        modelBuilder.Entity<Storefront>()
            .HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Storefronts_TenantId_Status");

        modelBuilder.Entity<Storefront>()
            .HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Storefronts_CreatedAt");

        // StorefrontPage Indexes
        modelBuilder.Entity<StorefrontPage>()
            .HasIndex(e => e.StorefrontId)
            .HasDatabaseName("IX_StorefrontPages_StorefrontId");

        modelBuilder.Entity<StorefrontPage>()
            .HasIndex(e => new { e.StorefrontId, e.Slug })
            .IsUnique()
            .HasDatabaseName("UX_StorefrontPages_StorefrontId_Slug");

        // StorefrontSection Indexes
        modelBuilder.Entity<StorefrontSection>()
            .HasIndex(e => e.PageId)
            .HasDatabaseName("IX_StorefrontSections_PageId");

        // StorefrontComponent Indexes
        modelBuilder.Entity<StorefrontComponent>()
            .HasIndex(e => e.SectionId)
            .HasDatabaseName("IX_StorefrontComponents_SectionId");
    }
}
