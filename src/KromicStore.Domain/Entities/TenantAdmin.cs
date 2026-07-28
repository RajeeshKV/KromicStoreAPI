// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a tenant administrator (store owner/manager).
/// TenantAdmins have global email uniqueness - one email can only be a TenantAdmin once across the platform.
/// </summary>
public class TenantAdmin : BaseEntity
{
    /// <summary>Gets the tenant ID this admin belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the first name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Gets the last name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Gets the email address (globally unique across all TenantAdmins).</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets the phone number.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Gets the password hash.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the admin account is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the date when the admin was last logged in.</summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>Gets the token version for token invalidation.</summary>
    public int TokenVersion { get; private set; } = 1;

    /// <summary>Gets the external authentication provider (e.g., Google).</summary>
    public string? ExternalAuthProvider { get; private set; }

    /// <summary>Gets the external authentication provider ID.</summary>
    public string? ExternalAuthProviderId { get; private set; }

    /// <summary>
    /// Creates a new instance of TenantAdmin.
    /// </summary>
    public static TenantAdmin Create(Guid tenantId, string firstName, string lastName, string email, string password)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));

        return new TenantAdmin
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            PasswordHash = password, // TODO: Hash this password
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records the admin's last login.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the token version to invalidate all existing tokens.
    /// </summary>
    public void IncrementTokenVersion()
    {
        TokenVersion++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the admin account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the admin account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets external authentication details.
    /// </summary>
    public void SetExternalAuth(string provider, string providerId)
    {
        ExternalAuthProvider = provider;
        ExternalAuthProviderId = providerId;
        UpdatedAt = DateTime.UtcNow;
    }
}
