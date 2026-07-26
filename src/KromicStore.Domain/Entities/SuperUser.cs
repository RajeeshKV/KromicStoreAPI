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

    /// <summary>
    /// Creates a new instance of SuperUser.
    /// </summary>
    public static SuperUser Create(string email, string firstName, string lastName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        return new SuperUser
        {
            Email = email.ToLowerInvariant(),
            FirstName = firstName,
            LastName = lastName,
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
