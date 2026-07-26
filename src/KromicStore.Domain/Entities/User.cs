namespace KromicStore.Domain.Entities;

using Enums;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User : BaseEntity
{
    /// <summary>Gets the tenant ID this user belongs to. Null for platform admins.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Gets the first name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Gets the last name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Gets the email address.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets the phone number.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Gets the password hash.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Gets the user role.</summary>
    public UserRole Role { get; private set; }

    /// <summary>Gets a value indicating whether the user account is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the date when the user was last logged in.</summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>Gets the external authentication provider (e.g., Google, GitHub).</summary>
    public string? ExternalAuthProvider { get; private set; }

    /// <summary>Gets the external authentication provider ID.</summary>
    public string? ExternalAuthProviderId { get; private set; }

    /// <summary>
    /// Creates a new instance of User.
    /// </summary>
    public static User Create(Guid? tenantId, string firstName, string lastName, string email, UserRole role)
    {
        // Tenant ID is required for all roles except PlatformAdmin
        if (role != UserRole.PlatformAdmin && (tenantId == null || tenantId == Guid.Empty))
            throw new ArgumentException("Tenant ID is required for non-platform admin users.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return new User
        {
            TenantId = tenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Role = role,
            IsActive = true
        };
    }

    /// <summary>
    /// Sets the password hash.
    /// </summary>
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Records the user's last login.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activates the user account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Sets external authentication details.
    /// </summary>
    public void SetExternalAuth(string provider, string providerId)
    {
        ExternalAuthProvider = provider;
        ExternalAuthProviderId = providerId;
    }
}
