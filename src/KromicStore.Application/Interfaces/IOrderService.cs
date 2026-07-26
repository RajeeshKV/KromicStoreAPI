namespace KromicStore.Application.Interfaces;

using KromicStore.Contracts.V1.Orders;

/// <summary>
/// Interface for order management services.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Gets a paginated list of orders for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="status">Optional status filter (pending, confirmed, paid, shipped, delivered, cancelled).</param>
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated order list.</returns>
    Task<OrderListResponse> GetOrdersAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        string? status = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific order by ID with all details.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The order details or null if not found.</returns>
    Task<OrderDto?> GetOrderByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new order with inventory validation and reservation.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The order creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the created order or error message.</returns>
    Task<ServiceResult<OrderDto>> CreateOrderAsync(
        Guid tenantId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing order (address and items if still pending).
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The order update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the updated order or error message.</returns>
    Task<ServiceResult<OrderDto>> UpdateOrderAsync(
        Guid id,
        Guid tenantId,
        UpdateOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a pending order and updates inventory.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the confirmed order or error message.</returns>
    Task<ServiceResult<OrderDto>> ConfirmOrderAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an order as shipped.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the shipped order or error message.</returns>
    Task<ServiceResult<OrderDto>> ShipOrderAsync(
        Guid id,
        Guid tenantId,
        string trackingNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an order as delivered.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the delivered order or error message.</returns>
    Task<ServiceResult<OrderDto>> DeliverOrderAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an order and releases inventory.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the cancelled order or error message.</returns>
    Task<ServiceResult<OrderDto>> CancelOrderAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
