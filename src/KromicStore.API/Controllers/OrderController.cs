namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Orders;

/// <summary>
/// Controller for managing orders.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
[Produces("application/json")]
[Authorize(Policy = Permissions.OrdersRead)]
public class OrderController : BaseController
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    /// <summary>
    /// Initializes a new instance of the OrderController class.
    /// </summary>
    public OrderController(
        ITenantProvider tenantProvider,
        IOrderService orderService,
        ILogger<OrderController> logger)
        : base(tenantProvider)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a paginated list of orders for the current tenant.
    /// </summary>
    /// <param name="pageNumber">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <param name="status">Optional filter by order status (pending, confirmed, paid, shipped, delivered, cancelled).</param>
    /// <param name="customerId">Optional filter by customer ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated list of orders.</returns>
    /// <response code="200">List of orders successfully retrieved.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(OrderListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1)
                return BadRequest(new { error = "Page number must be at least 1." });

            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "Page size must be between 1 and 100." });

            _logger.LogInformation(
                "Getting orders for tenant {TenantId}, page {PageNumber}, size {PageSize}, status filter: {Status}, customer filter: {CustomerId}",
                CurrentTenantId, pageNumber, pageSize, status, customerId);

            var result = await _orderService.GetOrdersAsync(
                CurrentTenantId,
                pageNumber,
                pageSize,
                status,
                customerId,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving orders." });
        }
    }

    /// <summary>
    /// Gets details for a specific order including items and payment status.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Order details with items.</returns>
    /// <response code="200">Order details successfully retrieved.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrderById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Order ID must be a valid GUID." });

            _logger.LogInformation(
                "Getting order {OrderId} for tenant {TenantId}",
                id, CurrentTenantId);

            var order = await _orderService.GetOrderByIdAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (order == null)
            {
                _logger.LogWarning(
                    "Order {OrderId} not found for tenant {TenantId}",
                    id, CurrentTenantId);
                return NotFound(new { error = "Order not found." });
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the order." });
        }
    }

    /// <summary>
    /// Creates a new order with inventory validation and reservation.
    /// </summary>
    /// <param name="request">The order creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created order.</returns>
    /// <response code="201">Order successfully created.</response>
    /// <response code="400">Invalid order data or insufficient stock.</response>
    /// <response code="401">User is not authenticated.</response>
    [Authorize(Policy = Permissions.OrdersWrite)]
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            _logger.LogInformation(
                "Creating order for customer {CustomerId} in tenant {TenantId}",
                request.CustomerId, CurrentTenantId);

            var result = await _orderService.CreateOrderAsync(
                CurrentTenantId,
                request,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Failed to create order: {Error}",
                    result.Error);
                return BadRequest(new { error = result.Error });
            }

            return CreatedAtAction(nameof(GetOrderById), new { id = result.Data!.Id }, result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while creating the order." });
        }
    }

    /// <summary>
    /// Updates an existing order (address and items if still pending).
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="request">The order update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated order.</returns>
    /// <response code="200">Order successfully updated.</response>
    /// <response code="400">Invalid order data or order not in pending status.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Order not found.</response>
    [Authorize(Policy = Permissions.OrdersWrite)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrder(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Order ID must be a valid GUID." });

            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            _logger.LogInformation(
                "Updating order {OrderId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _orderService.UpdateOrderAsync(
                id,
                CurrentTenantId,
                request,
                cancellationToken);

            if (!result.Success)
            {
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the order." });
        }
    }

    /// <summary>
    /// Confirms a pending order.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The confirmed order.</returns>
    /// <response code="200">Order successfully confirmed.</response>
    /// <response code="400">Invalid order status for confirmation.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Order not found.</response>
    [Authorize(Policy = Permissions.OrdersWrite)]
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmOrder(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Order ID must be a valid GUID." });

            _logger.LogInformation(
                "Confirming order {OrderId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _orderService.ConfirmOrderAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (!result.Success)
            {
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order {OrderId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while confirming the order." });
        }
    }

    /// <summary>
    /// Marks an order as shipped.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="request">The ship request with tracking number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The shipped order.</returns>
    /// <response code="200">Order successfully marked as shipped.</response>
    /// <response code="400">Invalid order status for shipping.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Order not found.</response>
    [Authorize(Policy = Permissions.OrdersWrite)]
    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShipOrder(
        [FromRoute] Guid id,
        [FromBody] ShipOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Order ID must be a valid GUID." });

            if (request == null || string.IsNullOrWhiteSpace(request.TrackingNumber))
                return BadRequest(new { error = "Tracking number is required." });

            _logger.LogInformation(
                "Shipping order {OrderId} for tenant {TenantId}, tracking: {TrackingNumber}",
                id, CurrentTenantId, request.TrackingNumber);

            var result = await _orderService.ShipOrderAsync(
                id,
                CurrentTenantId,
                request.TrackingNumber,
                cancellationToken);

            if (!result.Success)
            {
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping order {OrderId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while shipping the order." });
        }
    }

    /// <summary>
    /// Marks an order as delivered.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The delivered order.</returns>
    /// <response code="200">Order successfully marked as delivered.</response>
    /// <response code="400">Invalid order status for delivery.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Order not found.</response>
    [Authorize(Policy = Permissions.OrdersWrite)]
    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeliverOrder(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Order ID must be a valid GUID." });

            _logger.LogInformation(
                "Delivering order {OrderId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _orderService.DeliverOrderAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (!result.Success)
            {
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering order {OrderId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while delivering the order." });
        }
    }

    /// <summary>
    /// Cancels an order and releases inventory.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cancelled order.</returns>
    /// <response code="200">Order successfully cancelled.</response>
    /// <response code="400">Invalid order status for cancellation.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Order not found.</response>
    [Authorize(Policy = Permissions.OrdersWrite)]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Order ID must be a valid GUID." });

            _logger.LogInformation(
                "Cancelling order {OrderId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _orderService.CancelOrderAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (!result.Success)
            {
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while cancelling the order." });
        }
    }
}
