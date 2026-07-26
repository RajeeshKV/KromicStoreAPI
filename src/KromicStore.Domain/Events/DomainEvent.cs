namespace KromicStore.Domain.Events;

/// <summary>
/// Base class for domain events.
/// Domain events represent something that has happened in the business domain.
/// They are used to notify other parts of the system about state changes.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Gets the event ID (unique identifier).
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the tenant ID this event belongs to.
    /// </summary>
    public Guid TenantId { get; protected set; }

    /// <summary>
    /// Gets the entity ID that triggered this event.
    /// </summary>
    public Guid EntityId { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether this event has been published.
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Initializes a new instance of the DomainEvent class.
    /// </summary>
    protected DomainEvent()
    {
    }

    /// <summary>
    /// Gets the event type name.
    /// </summary>
    public virtual string GetEventType()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Gets a description of the event for logging/debugging.
    /// </summary>
    public virtual string GetDescription()
    {
        return $"{GetEventType()} - EntityId: {EntityId}, TenantId: {TenantId}, OccurredAt: {OccurredAt}";
    }
}
