namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a platform super user with full system access.
/// Separate from regular tenant users.
/// </summary>
public class SuperUser : BaseEntity
{
    /// <summary>Gets the email address (unique identifier).</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets the password hash.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Gets the first name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Gets the last name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the super user account is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the date when the super user was last logged in.</summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>Gets the token version for token invalidation.</summary>
    public int TokenVersion { get; private set; } = 1;

    /// <summary>
    /// Creates a new instance of SuperUser.
    /// </summary>
    public static SuperUser Create(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        return new SuperUser
        {
            Email = email.ToLowerInvariant(),
            FirstName = "SuperUser",
            LastName = "Admin",
            PasswordHash = passwordHash,
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
    /// Records the super user's last login.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the token version to invalidate all existing tokens.
    /// </summary>
    public void IncrementTokenVersion()
    {
        TokenVersion++;
    }

    /// <summary>
    /// Deactivates the super user account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activates the super user account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}
