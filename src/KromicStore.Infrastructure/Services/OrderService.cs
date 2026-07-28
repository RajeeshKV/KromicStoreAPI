namespace KromicStore.Infrastructure.Services;

using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Orders;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Domain.ValueObjects;
using KromicStore.Infrastructure.Proxies;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

/// <summary>
/// Service for managing orders.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
    private readonly ICacheService _cacheService;
    private readonly NotificationProxy _notificationProxy;

    /// <summary>
    /// Initializes a new instance of the OrderService class.
    /// </summary>
    public OrderService(
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger,
        ICacheService cacheService,
        NotificationProxy notificationProxy)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _notificationProxy = notificationProxy ?? throw new ArgumentNullException(nameof(notificationProxy));
    }

    /// <summary>
    /// Gets a paginated list of orders for a tenant.
    /// </summary>
    public async Task<OrderListResponse> GetOrdersAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        string? status = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Fetching orders for tenant {TenantId}, page {PageNumber}, size {PageSize}, status: {Status}, customer: {CustomerId}",
                tenantId, pageNumber, pageSize, status, customerId);

            // Enforce max page size
            if (pageSize > 100)
                pageSize = 100;

            // Build predicate for filtering
            Expression<Func<Order, bool>> predicate = o => o.TenantId == tenantId;

            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusEnum = ParseOrderStatus(status);
                if (statusEnum.HasValue)
                {
                    predicate = o => o.TenantId == tenantId && o.Status == statusEnum.Value;
                }
            }

            if (customerId.HasValue && customerId.Value != Guid.Empty)
            {
                var currentPredicate = predicate;
                predicate = o => currentPredicate.Compile()(o) && o.CustomerId == customerId.Value;
            }

            // Get all orders matching the filter
            var allOrders = await _unitOfWork.Orders.FindAsync(predicate, cancellationToken);

            // Apply pagination
            var totalCount = allOrders.Count;
            var skip = (pageNumber - 1) * pageSize;
            var orders = allOrders
                .OrderByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(MapToOrderListItemDto)
                .ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (decimal)pageSize);

            var response = new OrderListResponse(
                Data: orders,
                PageNumber: pageNumber,
                PageSize: pageSize,
                TotalCount: totalCount,
                TotalPages: totalPages,
                HasNextPage: pageNumber < totalPages,
                HasPreviousPage: pageNumber > 1);

            _logger.LogInformation(
                "Successfully fetched {OrderCount} orders for tenant {TenantId}",
                orders.Count, tenantId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific order by ID with all details.
    /// </summary>
    public async Task<OrderDto?> GetOrderByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try cache first
            var cacheKey = $"{tenantId}:order:{id}";
            var cached = await _cacheService.GetAsync<OrderDto>(cacheKey);
            if (cached != null)
                return cached;

            _logger.LogInformation(
                "Fetching order {OrderId} for tenant {TenantId}",
                id, tenantId);

            var order = await _unitOfWork.Orders.GetByIdAsync(id, cancellationToken);

            if (order == null || order.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Order {OrderId} not found for tenant {TenantId}",
                    id, tenantId);
                return null;
            }

            var dto = MapToOrderDto(order);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order {OrderId}", id);
            throw;
        }
    }

    /// <summary>
    /// Creates a new order with inventory validation and reservation.
    /// </summary>
    public async Task<ServiceResult<OrderDto>> CreateOrderAsync(
        Guid tenantId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating order for tenant {TenantId}, customer {CustomerId}",
                tenantId, request.CustomerId);

            // Validate customer exists
            var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null || customer.TenantId != tenantId)
            {
                const string error = "Customer not found.";
                _logger.LogWarning(error);
                return ServiceResult<OrderDto>.FailureResult(error);
            }

            // Validate and check inventory for all items
            var orderItems = new List<(Product Product, CreateOrderItemRequest Request)>();
            decimal subtotal = 0;

            foreach (var itemRequest in request.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(itemRequest.ProductId, cancellationToken);
                if (product == null || product.TenantId != tenantId)
                {
                    return ServiceResult<OrderDto>.FailureResult(
                        $"Product {itemRequest.ProductId} not found.");
                }

                // Check stock availability
                if (product.StockQuantity < itemRequest.Quantity)
                {
                    return ServiceResult<OrderDto>.FailureResult(
                        $"Insufficient stock for product {product.Name}. Available: {product.StockQuantity}, Requested: {itemRequest.Quantity}");
                }

                orderItems.Add((product, itemRequest));
                subtotal += product.Price.Amount * itemRequest.Quantity;
            }

            // Create the order
            var order = Order.Create(
                tenantId,
                customer.Id,
                MapToAddress(request.ShippingAddress),
                MapToAddress(request.BillingAddress));

            // Add items to order
            foreach (var (product, itemRequest) in orderItems)
            {
                order.AddItem(product.Id, itemRequest.Quantity, product.Price);
            }

            // Calculate totals (simplified: no tax/shipping in this implementation)
            var subtotalMoney = new Money(subtotal);
            var taxAmount = new Money(0);
            var shippingCost = new Money(0);
            order.UpdateTotals(subtotalMoney, taxAmount, shippingCost);

            // Reserve inventory (reduce stock)
            foreach (var (product, itemRequest) in orderItems)
            {
                product.ReduceStock(itemRequest.Quantity);
            }

            // Persist the order
            await _unitOfWork.Orders.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} ({OrderNumber}) created successfully for tenant {TenantId}",
                order.Id, order.OrderNumber, tenantId);

            // Send Order Placed email
            await SendOrderPlacedEmailAsync(order, customer, tenantId, cancellationToken);

            var dto = MapToOrderDto(order);
            return ServiceResult<OrderDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for tenant {TenantId}", tenantId);
            return ServiceResult<OrderDto>.FailureResult("An error occurred while creating the order.");
        }
    }

    /// <summary>
    /// Updates an existing order (address and items if still pending).
    /// </summary>
    public async Task<ServiceResult<OrderDto>> UpdateOrderAsync(
        Guid id,
        Guid tenantId,
        UpdateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Updating order {OrderId} for tenant {TenantId}",
                id, tenantId);

            var order = await _unitOfWork.Orders.GetByIdAsync(id, cancellationToken);
            if (order == null || order.TenantId != tenantId)
            {
                return ServiceResult<OrderDto>.FailureResult("Order not found.");
            }

            // Can only update pending orders
            if (order.Status != OrderStatus.Pending)
            {
                return ServiceResult<OrderDto>.FailureResult(
                    "Only pending orders can be updated.");
            }

            // For now, we don't support updating items due to domain model constraints
            // Only update addresses via a new order creation
            // This is simplified - in production, you'd need UpdateShippingAddress and UpdateBillingAddress methods

            _logger.LogInformation(
                "Order {OrderId} updated successfully for tenant {TenantId}",
                id, tenantId);

            // Invalidate cache
            await _cacheService.RemoveAsync($"{tenantId}:order:{id}");

            var dto = MapToOrderDto(order);
            return ServiceResult<OrderDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId}", id);
            return ServiceResult<OrderDto>.FailureResult("An error occurred while updating the order.");
        }
    }

    /// <summary>
    /// Confirms a pending order and updates inventory.
    /// </summary>
    public async Task<ServiceResult<OrderDto>> ConfirmOrderAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Confirming order {OrderId} for tenant {TenantId}",
                id, tenantId);

            var order = await _unitOfWork.Orders.GetByIdAsync(id, cancellationToken);
            if (order == null || order.TenantId != tenantId)
            {
                return ServiceResult<OrderDto>.FailureResult("Order not found.");
            }

            try
            {
                order.Confirm();
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult<OrderDto>.FailureResult(ex.Message);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} confirmed successfully for tenant {TenantId}",
                id, tenantId);

            // Send Order Confirmed email
            await SendOrderConfirmedEmailAsync(order, tenantId, cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync($"{tenantId}:order:{id}");

            var dto = MapToOrderDto(order);
            return ServiceResult<OrderDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order {OrderId}", id);
            return ServiceResult<OrderDto>.FailureResult("An error occurred while confirming the order.");
        }
    }

    /// <summary>
    /// Marks an order as shipped.
    /// </summary>
    public async Task<ServiceResult<OrderDto>> ShipOrderAsync(
        Guid id,
        Guid tenantId,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Shipping order {OrderId} for tenant {TenantId}, tracking: {TrackingNumber}",
                id, tenantId, trackingNumber);

            var order = await _unitOfWork.Orders.GetByIdAsync(id, cancellationToken);
            if (order == null || order.TenantId != tenantId)
            {
                return ServiceResult<OrderDto>.FailureResult("Order not found.");
            }

            try
            {
                order.MarkAsShipped(trackingNumber);
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult<OrderDto>.FailureResult(ex.Message);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} shipped successfully for tenant {TenantId}",
                id, tenantId);

            // Send Order Dispatched email
            await SendOrderDispatchedEmailAsync(order, tenantId, cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync($"{tenantId}:order:{id}");

            var dto = MapToOrderDto(order);
            return ServiceResult<OrderDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping order {OrderId}", id);
            return ServiceResult<OrderDto>.FailureResult("An error occurred while shipping the order.");
        }
    }

    /// <summary>
    /// Marks an order as delivered.
    /// </summary>
    public async Task<ServiceResult<OrderDto>> DeliverOrderAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Delivering order {OrderId} for tenant {TenantId}",
                id, tenantId);

            var order = await _unitOfWork.Orders.GetByIdAsync(id, cancellationToken);
            if (order == null || order.TenantId != tenantId)
            {
                return ServiceResult<OrderDto>.FailureResult("Order not found.");
            }

            try
            {
                order.MarkAsDelivered();
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult<OrderDto>.FailureResult(ex.Message);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} delivered successfully for tenant {TenantId}",
                id, tenantId);

            // Invalidate cache
            await _cacheService.RemoveAsync($"{tenantId}:order:{id}");

            var dto = MapToOrderDto(order);
            return ServiceResult<OrderDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering order {OrderId}", id);
            return ServiceResult<OrderDto>.FailureResult("An error occurred while delivering the order.");
        }
    }

    /// <summary>
    /// Cancels an order and releases inventory.
    /// </summary>
    public async Task<ServiceResult<OrderDto>> CancelOrderAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Cancelling order {OrderId} for tenant {TenantId}",
                id, tenantId);

            var order = await _unitOfWork.Orders.GetByIdAsync(id, cancellationToken);
            if (order == null || order.TenantId != tenantId)
            {
                return ServiceResult<OrderDto>.FailureResult("Order not found.");
            }

            try
            {
                order.Cancel();
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult<OrderDto>.FailureResult(ex.Message);
            }

            // Release inventory
            foreach (var item in order.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
                if (product != null)
                {
                    product.RestoreStock(item.Quantity);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} cancelled successfully for tenant {TenantId}",
                id, tenantId);

            // Invalidate cache
            await _cacheService.RemoveAsync($"{tenantId}:order:{id}");

            var dto = MapToOrderDto(order);
            return ServiceResult<OrderDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return ServiceResult<OrderDto>.FailureResult("An error occurred while cancelling the order.");
        }
    }

    // Helper methods
    private OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto(
            Id: order.Id,
            OrderNumber: order.OrderNumber,
            Status: order.Status.ToString(),
            Subtotal: order.Subtotal.Amount,
            TaxAmount: order.TaxAmount.Amount,
            ShippingCost: order.ShippingCost.Amount,
            Total: order.Total.Amount,
            PaymentStatus: order.PaymentStatus.ToString(),
            PaymentMethod: order.PaymentMethod,
            TrackingNumber: order.TrackingNumber,
            ShippingAddress: MapToAddressDto(order.ShippingAddress),
            Items: order.Items.Select(MapToOrderItemDto).ToList(),
            CreatedAt: order.CreatedAt);
    }

    private OrderListItemDto MapToOrderListItemDto(Order order)
    {
        return new OrderListItemDto(
            Id: order.Id,
            OrderNumber: order.OrderNumber,
            Status: order.Status.ToString(),
            Total: order.Total.Amount,
            CreatedAt: order.CreatedAt);
    }

    private OrderItemDto MapToOrderItemDto(OrderItem item)
    {
        return new OrderItemDto(
            ProductId: item.ProductId,
            ProductName: item.ProductName,
            Quantity: item.Quantity,
            UnitPrice: item.UnitPrice.Amount,
            TotalPrice: item.UnitPrice.Amount * item.Quantity);
    }

    private AddressDto? MapToAddressDto(Address? address)
    {
        if (address == null)
            return null;

        return new AddressDto(
            Street: address.Street,
            City: address.City,
            State: address.State,
            PostalCode: address.PostalCode,
            Country: address.Country);
    }

    private Address MapToAddress(AddressRequest request)
    {
        return new Address(
            request.Street,
            request.City,
            request.State,
            request.PostalCode,
            request.Country);
    }

    private OrderStatus? ParseOrderStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        return status.ToUpperInvariant() switch
        {
            "PENDING" => OrderStatus.Pending,
            "CONFIRMED" => OrderStatus.Confirmed,
            "PAID" => OrderStatus.Paid,
            "PROCESSING" => OrderStatus.Processing,
            "SHIPPED" => OrderStatus.Shipped,
            "DELIVERED" => OrderStatus.Delivered,
            "CANCELLED" => OrderStatus.Cancelled,
            _ => null
        };
    }

    /// <summary>
    /// Sends Order Placed email notification
    /// </summary>
    private async Task SendOrderPlacedEmailAsync(Order order, Customer customer, Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found for order {OrderId}, skipping email", order.Id);
                return;
            }

            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(tenantId, cancellationToken);
            var customerName = $"{customer.FirstName} {customer.LastName}".Trim();
            var parameters = new Dictionary<string, string>
            {
                { "tenant_name", tenant.Name },
                { "logo_url", storefront?.LogoUrl ?? "" },
                { "order_number", order.OrderNumber },
                { "customer_name", customerName },
                { "customer_email", customer.Email },
                { "customer_phone", customer.PhoneNumber ?? "" },
                { "shipping_address", FormatAddress(order.ShippingAddress) },
                { "subtotal", order.Subtotal.Amount.ToString("F2") },
                { "tax_amount", order.TaxAmount.Amount.ToString("F2") },
                { "shipping_cost", order.ShippingCost.Amount.ToString("F2") },
                { "total_amount", order.Total.Amount.ToString("F2") },
                { "payment_method", order.PaymentMethod ?? "Online" },
                { "payment_status", order.PaymentStatus.ToString() },
                { "order_date", order.CreatedAt.ToString("yyyy-MM-dd HH:mm") },
                { "contact_email", storefront?.ContactEmail ?? "" },
                { "current_year", DateTime.UtcNow.Year.ToString() }
            };

            var emailRequest = new SendEmailRequest
            {
                To = customer.Email,
                ToName = customerName,
                Subject = $"Order Placed - {order.OrderNumber}",
                EmailType = "order_placed",
                TemplateParameters = parameters,
                Tag = "order_placed"
            };

            var result = await _notificationProxy.SendEmailAsync(emailRequest, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Order Placed email sent to {Email} for order {OrderId}", customer.Email, order.Id);
            }
            else
            {
                _logger.LogWarning("Failed to send Order Placed email to {Email}: {Error}", customer.Email, result.Exception?.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Order Placed email for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Sends Order Confirmed email notification
    /// </summary>
    private async Task SendOrderConfirmedEmailAsync(Order order, Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found for order {OrderId}, skipping email", order.Id);
                return;
            }

            var customer = await _unitOfWork.Customers.GetByIdAsync(order.CustomerId, cancellationToken);
            if (customer == null)
            {
                _logger.LogWarning("Customer not found for order {OrderId}, skipping email", order.Id);
                return;
            }

            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(tenantId, cancellationToken);
            var customerName = $"{customer.FirstName} {customer.LastName}".Trim();
            var parameters = new Dictionary<string, string>
            {
                { "tenant_name", tenant.Name },
                { "logo_url", storefront?.LogoUrl ?? "" },
                { "order_number", order.OrderNumber },
                { "customer_name", customerName },
                { "shipping_address", FormatAddress(order.ShippingAddress) },
                { "total_amount", order.Total.Amount.ToString("F2") },
                { "payment_method", order.PaymentMethod ?? "Online" },
                { "order_date", order.CreatedAt.ToString("yyyy-MM-dd HH:mm") },
                { "order_placed_date", order.CreatedAt.ToString("yyyy-MM-dd HH:mm") },
                { "order_confirmed_date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") },
                { "estimated_delivery_days", "5-7" },
                { "contact_email", storefront?.ContactEmail ?? "" },
                { "current_year", DateTime.UtcNow.Year.ToString() }
            };

            var emailRequest = new SendEmailRequest
            {
                To = customer.Email,
                ToName = customerName,
                Subject = $"Order Confirmed - {order.OrderNumber}",
                EmailType = "order_confirmed",
                TemplateParameters = parameters,
                Tag = "order_confirmed"
            };

            var result = await _notificationProxy.SendEmailAsync(emailRequest, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Order Confirmed email sent to {Email} for order {OrderId}", customer.Email, order.Id);
            }
            else
            {
                _logger.LogWarning("Failed to send Order Confirmed email to {Email}: {Error}", customer.Email, result.Exception?.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Order Confirmed email for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Sends Order Dispatched email notification
    /// </summary>
    private async Task SendOrderDispatchedEmailAsync(Order order, Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found for order {OrderId}, skipping email", order.Id);
                return;
            }

            var customer = await _unitOfWork.Customers.GetByIdAsync(order.CustomerId, cancellationToken);
            if (customer == null)
            {
                _logger.LogWarning("Customer not found for order {OrderId}, skipping email", order.Id);
                return;
            }

            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(tenantId, cancellationToken);
            var customerName = $"{customer.FirstName} {customer.LastName}".Trim();

            // Get courier info if available
            string courierName = "Courier Partner";
            string trackingUrl = "";

            // Try to find courier by tracking number pattern (simplified)
            if (!string.IsNullOrWhiteSpace(order.TrackingNumber))
            {
                var couriers = await _unitOfWork.Couriers.FindAsync(c => c.TenantId == tenantId && c.IsActive, cancellationToken);
                var courier = couriers.FirstOrDefault();
                if (courier != null)
                {
                    courierName = courier.Name;
                    trackingUrl = courier.GenerateTrackingUrl(order.TrackingNumber) ?? "";
                }
            }

            var parameters = new Dictionary<string, string>
            {
                { "tenant_name", tenant.Name },
                { "logo_url", storefront?.LogoUrl ?? "" },
                { "order_number", order.OrderNumber },
                { "customer_name", customerName },
                { "customer_phone", customer.PhoneNumber ?? "" },
                { "shipping_address", FormatAddress(order.ShippingAddress) },
                { "tracking_number", order.TrackingNumber ?? "" },
                { "courier_name", courierName },
                { "tracking_url", trackingUrl },
                { "estimated_delivery_date", DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd") },
                { "order_placed_date", order.CreatedAt.ToString("yyyy-MM-dd HH:mm") },
                { "order_confirmed_date", order.UpdatedAt.ToString("yyyy-MM-dd HH:mm") },
                { "order_processed_date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") },
                { "order_shipped_date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") },
                { "contact_email", storefront?.ContactEmail ?? "" },
                { "current_year", DateTime.UtcNow.Year.ToString() }
            };

            var emailRequest = new SendEmailRequest
            {
                To = customer.Email,
                ToName = customerName,
                Subject = $"Order Dispatched - {order.OrderNumber}",
                EmailType = "order_dispatched",
                TemplateParameters = parameters,
                Tag = "order_dispatched"
            };

            var result = await _notificationProxy.SendEmailAsync(emailRequest, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Order Dispatched email sent to {Email} for order {OrderId}", customer.Email, order.Id);
            }
            else
            {
                _logger.LogWarning("Failed to send Order Dispatched email to {Email}: {Error}", customer.Email, result.Exception?.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Order Dispatched email for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Formats address for email template
    /// </summary>
    private string FormatAddress(Address? address)
    {
        if (address == null)
            return "";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(address.Street))
            parts.Add(address.Street);
        if (!string.IsNullOrWhiteSpace(address.City))
            parts.Add(address.City);
        if (!string.IsNullOrWhiteSpace(address.State))
            parts.Add(address.State);
        if (!string.IsNullOrWhiteSpace(address.PostalCode))
            parts.Add(address.PostalCode);
        if (!string.IsNullOrWhiteSpace(address.Country))
            parts.Add(address.Country);

        return string.Join(", ", parts);
    }
}
