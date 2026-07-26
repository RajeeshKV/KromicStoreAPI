namespace KromicStore.Infrastructure.Services;

using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Customers;
using KromicStore.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service for managing customers.
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CustomerService> _logger;

    /// <summary>
    /// Initializes a new instance of the CustomerService class.
    /// </summary>
    public CustomerService(
        IUnitOfWork unitOfWork,
        ILogger<CustomerService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<CustomerDto>> GetCustomersAsync(
        Guid tenantId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Max(1, Math.Min(pageSize, 100));

        _logger.LogInformation(
            "Getting customers for tenant {TenantId}, page {PageNumber}, size {PageSize}",
            tenantId, pageNumber, pageSize);

        try
        {
            // Get total count
            var totalCount = await _unitOfWork.Customers.CountAsync(
                c => c.TenantId == tenantId,
                cancellationToken);

            if (totalCount == 0)
            {
                return new PaginatedResponse<CustomerDto>
                {
                    Items = Enumerable.Empty<CustomerDto>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }

            // Get all customers for this tenant
            var allCustomers = await _unitOfWork.Customers.FindAsync(
                c => c.TenantId == tenantId,
                cancellationToken);

            // Apply pagination
            var skip = (pageNumber - 1) * pageSize;
            var customers = allCustomers
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            var customerDtos = customers.Select(c => MapToDto(c)).ToList();

            return new PaginatedResponse<CustomerDto>
            {
                Items = customerDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CustomerDto?> GetCustomerByIdAsync(
        Guid customerId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID must not be empty", nameof(customerId));

        _logger.LogInformation(
            "Getting customer {CustomerId} for tenant {TenantId}",
            customerId, tenantId);

        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);

            if (customer == null || customer.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Customer {CustomerId} not found for tenant {TenantId}",
                    customerId, tenantId);
                return null;
            }

            return MapToDto(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", customerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CustomerDto> CreateCustomerAsync(
        Guid tenantId,
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Validate email is unique within tenant
        var emailExists = await _unitOfWork.Customers.AnyAsync(
            c => c.TenantId == tenantId && c.Email.ToLower() == request.Email.ToLower(),
            cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning(
                "Email {Email} already exists for tenant {TenantId}",
                request.Email, tenantId);
            throw new InvalidOperationException(
                $"A customer with email '{request.Email}' already exists for this tenant.");
        }

        _logger.LogInformation(
            "Creating customer {Email} for tenant {TenantId}",
            request.Email, tenantId);

        try
        {
            var customer = Domain.Entities.Customer.Create(
                tenantId,
                request.FirstName,
                request.LastName,
                request.Email.ToLower(),
                request.PhoneNumber);

            await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Customer {CustomerId} created successfully",
                customer.Id);

            return MapToDto(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CustomerDto> UpdateCustomerAsync(
        Guid customerId,
        Guid tenantId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID must not be empty", nameof(customerId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation(
            "Updating customer {CustomerId} for tenant {TenantId}",
            customerId, tenantId);

        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);

            if (customer == null || customer.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Customer {CustomerId} not found for tenant {TenantId}",
                    customerId, tenantId);
                throw new InvalidOperationException("Customer not found.");
            }

            // Update fields
            customer.Update(request.FirstName, request.LastName, request.PhoneNumber);
            customer.UpdateTimestamp();

            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Customer {CustomerId} updated successfully",
                customerId);

            return MapToDto(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", customerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCustomerAsync(
        Guid customerId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID must not be empty", nameof(customerId));

        _logger.LogInformation(
            "Deleting customer {CustomerId} for tenant {TenantId}",
            customerId, tenantId);

        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);

            if (customer == null || customer.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Customer {CustomerId} not found for tenant {TenantId}",
                    customerId, tenantId);
                return false;
            }

            // GDPR-compliant deletion: anonymize rather than hard delete
            customer.Anonymize(customerId);
            customer.UpdateTimestamp();

            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Customer {CustomerId} anonymized for GDPR compliance",
                customerId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", customerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<CustomerOrderDto>> GetCustomerOrdersAsync(
        Guid customerId,
        Guid tenantId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID must not be empty", nameof(customerId));

        // Validate pagination parameters
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Max(1, Math.Min(pageSize, 100));

        _logger.LogInformation(
            "Getting orders for customer {CustomerId}, page {PageNumber}, size {PageSize}",
            customerId, pageNumber, pageSize);

        try
        {
            // Verify customer exists and belongs to tenant
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);
            if (customer == null || customer.TenantId != tenantId)
            {
                throw new InvalidOperationException("Customer not found.");
            }

            // Get total count of orders
            var totalCount = await _unitOfWork.Orders.CountAsync(
                o => o.CustomerId == customerId && o.TenantId == tenantId,
                cancellationToken);

            if (totalCount == 0)
            {
                return new PaginatedResponse<CustomerOrderDto>
                {
                    Items = Enumerable.Empty<CustomerOrderDto>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }

            // Get paginated orders
            var skip = (pageNumber - 1) * pageSize;
            var allOrders = await _unitOfWork.Orders.FindAsync(
                o => o.CustomerId == customerId && o.TenantId == tenantId,
                cancellationToken);

            var orders = allOrders
                .OrderByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            var orderDtos = orders.Select(o => new CustomerOrderDto(
                o.Id,
                o.OrderNumber,
                o.Total.Amount,
                o.Status.ToString(),
                o.CreatedAt,
                o.Status == OrderStatus.Shipped || o.Status == OrderStatus.Delivered ? o.UpdatedAt : null
            )).ToList();

            return new PaginatedResponse<CustomerOrderDto>
            {
                Items = orderDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for customer {CustomerId}", customerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailUniqueAsync(
        string email,
        Guid tenantId,
        Guid? excludeCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email must not be empty", nameof(email));

        try
        {
            var exists = await _unitOfWork.Customers.AnyAsync(
                c => c.TenantId == tenantId &&
                     c.Email.ToLower() == email.ToLower() &&
                     (excludeCustomerId == null || c.Id != excludeCustomerId),
                cancellationToken);

            return !exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email uniqueness for {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Maps a customer entity to a DTO.
    /// </summary>
    private static CustomerDto MapToDto(Domain.Entities.Customer customer)
    {
        var billingAddress = customer.BillingAddress != null
            ? new AddressDto(
                customer.BillingAddress.Street,
                customer.BillingAddress.City,
                customer.BillingAddress.State,
                customer.BillingAddress.PostalCode,
                customer.BillingAddress.Country)
            : null;

        var shippingAddress = customer.ShippingAddress != null
            ? new AddressDto(
                customer.ShippingAddress.Street,
                customer.ShippingAddress.City,
                customer.ShippingAddress.State,
                customer.ShippingAddress.PostalCode,
                customer.ShippingAddress.Country)
            : null;

        return new CustomerDto(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.Email,
            customer.PhoneNumber,
            billingAddress,
            shippingAddress,
            customer.LifetimeValue,
            customer.TotalOrdersCount,
            customer.IsActive,
            customer.CreatedAt);
    }
}
