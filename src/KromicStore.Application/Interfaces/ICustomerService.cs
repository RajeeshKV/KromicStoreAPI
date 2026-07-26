namespace KromicStore.Application.Interfaces;

using KromicStore.Contracts.V1.Customers;

/// <summary>
/// Service interface for managing customers.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Gets a paginated list of customers for the current tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of customers.</returns>
    Task<PaginatedResponse<CustomerDto>> GetCustomersAsync(
        Guid tenantId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific customer by ID.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The customer details, or null if not found.</returns>
    Task<CustomerDto?> GetCustomerByIdAsync(
        Guid customerId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The customer creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created customer.</returns>
    /// <exception cref="InvalidOperationException">If email already exists for tenant.</exception>
    Task<CustomerDto> CreateCustomerAsync(
        Guid tenantId,
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The customer update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated customer.</returns>
    /// <exception cref="InvalidOperationException">If customer not found.</exception>
    Task<CustomerDto> UpdateCustomerAsync(
        Guid customerId,
        Guid tenantId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a customer (GDPR-compliant anonymization).
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if customer was deleted, false if not found.</returns>
    Task<bool> DeleteCustomerAsync(
        Guid customerId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the order history for a customer.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of customer orders.</returns>
    Task<PaginatedResponse<CustomerOrderDto>> GetCustomerOrdersAsync(
        Guid customerId,
        Guid tenantId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is unique within the tenant.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="excludeCustomerId">Optional customer ID to exclude (for updates).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if email is unique, false otherwise.</returns>
    Task<bool> IsEmailUniqueAsync(
        string email,
        Guid tenantId,
        Guid? excludeCustomerId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Response for paginated data.
/// </summary>
public class PaginatedResponse<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public required IEnumerable<T> Items { get; set; }

    /// <summary>
    /// The current page number.
    /// </summary>
    public required int PageNumber { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public required int PageSize { get; set; }

    /// <summary>
    /// The total number of items.
    /// </summary>
    public required int TotalCount { get; set; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>
/// DTO for customer orders in a list response.
/// </summary>
public record CustomerOrderDto(
    Guid Id,
    string OrderNumber,
    decimal Total,
    string Status,
    DateTime CreatedAt,
    DateTime? ShippedAt);
