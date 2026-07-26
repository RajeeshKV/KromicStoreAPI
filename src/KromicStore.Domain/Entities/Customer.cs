namespace KromicStore.Domain.Entities;

using ValueObjects;

/// <summary>
/// Represents a customer in the system.
/// </summary>
public class Customer : BaseEntity
{
    /// <summary>Gets the tenant ID this customer belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the user ID if the customer has a registered account.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Gets the first name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Gets the last name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Gets the email address.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets the phone number.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Gets the billing address.</summary>
    public Address? BillingAddress { get; private set; }

    /// <summary>Gets the shipping address.</summary>
    public Address? ShippingAddress { get; private set; }

    /// <summary>Gets the customer's lifetime value.</summary>
    public Money LifetimeValue { get; private set; }

    /// <summary>Gets the number of orders placed by the customer.</summary>
    public int TotalOrdersCount { get; private set; }

    /// <summary>Gets a value indicating whether the customer is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the date when the customer was registered.</summary>
    public DateTime RegisteredAt { get; private set; }

    /// <summary>Gets the date when the customer's email was verified.</summary>
    public DateTime? VerifiedAt { get; private set; }

    /// <summary>Gets a value indicating whether the customer is subscribed to newsletter.</summary>
    public bool NewsletterSubscribed { get; private set; }

    /// <summary>Gets the last order date.</summary>
    public DateTime? LastOrderAt { get; private set; }

    /// <summary>
    /// Creates a new instance of Customer.
    /// </summary>
    public static Customer Create(
        Guid tenantId,
        string firstName,
        string lastName,
        string email,
        string? phoneNumber = null,
        Guid? userId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return new Customer
        {
            TenantId = tenantId,
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            IsActive = true,
            RegisteredAt = DateTime.UtcNow,
            VerifiedAt = null,
            NewsletterSubscribed = false,
            LastOrderAt = null,
            LifetimeValue = new Money(0),
            TotalOrdersCount = 0
        };
    }

    /// <summary>
    /// Updates customer information.
    /// </summary>
    public void Update(string firstName, string lastName, string? phoneNumber = null)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    /// <summary>
    /// Sets the billing address.
    /// </summary>
    public void SetBillingAddress(Address address)
    {
        BillingAddress = address ?? throw new ArgumentNullException(nameof(address));
    }

    /// <summary>
    /// Sets the shipping address.
    /// </summary>
    public void SetShippingAddress(Address address)
    {
        ShippingAddress = address ?? throw new ArgumentNullException(nameof(address));
    }

    /// <summary>
    /// Records a purchase for the customer.
    /// </summary>
    public void RecordPurchase(Money amount)
    {
        TotalOrdersCount++;
        LifetimeValue = new Money(LifetimeValue.Amount + amount.Amount);
    }

    /// <summary>
    /// Deactivates the customer account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activates the customer account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Gets the customer's full name.
    /// </summary>
    public string GetFullName()
    {
        return $"{FirstName} {LastName}".Trim();
    }

    /// <summary>
    /// Marks the customer email as verified.
    /// </summary>
    public void MarkEmailAsVerified()
    {
        VerifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Subscribes the customer to newsletter.
    /// </summary>
    public void SubscribeToNewsletter()
    {
        NewsletterSubscribed = true;
    }

    /// <summary>
    /// Unsubscribes the customer from newsletter.
    /// </summary>
    public void UnsubscribeFromNewsletter()
    {
        NewsletterSubscribed = false;
    }

    /// <summary>
    /// Records a new order for this customer (updates lifetime value and last order date).
    /// </summary>
    public void RecordNewOrder(Money orderTotal)
    {
        TotalOrdersCount++;
        LifetimeValue = new Money(LifetimeValue.Amount + orderTotal.Amount);
        LastOrderAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Anonymizes the customer data for GDPR compliance.
    /// </summary>
    public void Anonymize(Guid customerId)
    {
        FirstName = "DELETED";
        LastName = $"CUSTOMER_{customerId:N}";
        Email = $"deleted_{customerId:N}@deleted.local";
        PhoneNumber = null;
        IsActive = false;
    }
}
