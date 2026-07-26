namespace KromicStore.Domain.Entities;

/// <summary>
/// Base class for all domain entities.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Gets the unique identifier for the entity.</summary>
    public Guid Id { get; protected set; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>Gets the last modification timestamp.</summary>
    public DateTime UpdatedAt { get; protected set; }

    /// <summary>Gets the ID of the user who created this entity.</summary>
    public Guid? CreatedBy { get; protected set; }

    /// <summary>Gets the ID of the user who last updated this entity.</summary>
    public Guid? UpdatedBy { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the BaseEntity class.
    /// </summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the UpdatedAt timestamp and UpdatedBy user ID.
    /// </summary>
    /// <param name="userId">The ID of the user performing the update.</param>
    public virtual void UpdateTimestamp(Guid? userId = null)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = userId;
    }
}
