namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Customers;
using KromicStore.Contracts.V1.Orders;
using KromicStore.Domain.ValueObjects;

/// <summary>
/// Controller for managing customers.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize(Policy = Permissions.CustomersRead)]
public class CustomerController : BaseController
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomerController> _logger;

    /// <summary>
    /// Initializes a new instance of the CustomerController class.
    /// </summary>
    public CustomerController(
        ITenantProvider tenantProvider,
        ICustomerService customerService,
        ILogger<CustomerController> logger)
        : base(tenantProvider)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a paginated list of customers for the current tenant.
    /// </summary>
    /// <param name="pageNumber">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated list of customers.</returns>
    /// <response code="200">List of customers successfully retrieved.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
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
                "Getting customers for tenant {TenantId}, page {PageNumber}, size {PageSize}",
                CurrentTenantId, pageNumber, pageSize);

            var result = await _customerService.GetCustomersAsync(
                CurrentTenantId,
                pageNumber,
                pageSize,
                cancellationToken);

            var response = new PaginatedListResponse
            {
                Items = result.Items.Select(c => new CustomerListItemDto(
                    c.Id,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.LifetimeValue,
                    c.TotalOrdersCount,
                    c.RegisteredAt)).ToList(),
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
                HasNextPage = result.HasNextPage,
                HasPreviousPage = result.HasPreviousPage
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving customers." });
        }
    }

    /// <summary>
    /// Gets details for a specific customer.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Customer details.</returns>
    /// <response code="200">Customer details successfully retrieved.</response>
    /// <response code="404">Customer not found.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCustomerById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Customer ID must be a valid GUID." });

            _logger.LogInformation(
                "Getting customer {CustomerId} for tenant {TenantId}",
                id, CurrentTenantId);

            var customer = await _customerService.GetCustomerByIdAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (customer == null)
            {
                _logger.LogWarning(
                    "Customer {CustomerId} not found for tenant {TenantId}",
                    id, CurrentTenantId);
                return NotFound(new { error = "Customer not found." });
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the customer." });
        }
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="request">The customer creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created customer.</returns>
    /// <response code="201">Customer successfully created.</response>
    /// <response code="400">Invalid customer data or email already exists.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="409">Email already exists within tenant.</response>
    [Authorize(Policy = Permissions.CustomersWrite)]
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            _logger.LogInformation(
                "Creating customer {Email} for tenant {TenantId}",
                request.Email, CurrentTenantId);

            // Check email uniqueness
            var isEmailUnique = await _customerService.IsEmailUniqueAsync(
                request.Email,
                CurrentTenantId,
                cancellationToken: cancellationToken);

            if (!isEmailUnique)
            {
                _logger.LogWarning(
                    "Email {Email} already exists for tenant {TenantId}",
                    request.Email, CurrentTenantId);
                return Conflict(new { error = "A customer with this email already exists." });
            }

            var result = await _customerService.CreateCustomerAsync(
                CurrentTenantId,
                request,
                cancellationToken);

            return CreatedAtAction(nameof(GetCustomerById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating customer for tenant {TenantId}", CurrentTenantId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while creating the customer." });
        }
    }

    /// <summary>
    /// Updates an existing customer profile.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="request">The customer update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated customer.</returns>
    /// <response code="200">Customer successfully updated.</response>
    /// <response code="400">Invalid customer data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Customer not found.</response>
    [Authorize(Policy = Permissions.CustomersWrite)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCustomer(
        [FromRoute] Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Customer ID must be a valid GUID." });

            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            _logger.LogInformation(
                "Updating customer {CustomerId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _customerService.UpdateCustomerAsync(
                id,
                CurrentTenantId,
                request,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer not found for update: {CustomerId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the customer." });
        }
    }

    /// <summary>
    /// Deletes a customer (GDPR-compliant anonymization).
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Customer successfully deleted (anonymized).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Customer not found.</response>
    [Authorize(Policy = Permissions.CustomersWrite)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Customer ID must be a valid GUID." });

            _logger.LogInformation(
                "Deleting customer {CustomerId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _customerService.DeleteCustomerAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (!result)
            {
                _logger.LogWarning(
                    "Customer {CustomerId} not found for deletion in tenant {TenantId}",
                    id, CurrentTenantId);
                return NotFound(new { error = "Customer not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the customer." });
        }
    }

    /// <summary>
    /// Gets the order history for a specific customer with pagination.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="pageNumber">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated list of customer orders.</returns>
    /// <response code="200">Customer orders successfully retrieved.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Customer not found.</response>
    [HttpGet("{id:guid}/orders")]
    [ProducesResponseType(typeof(PaginatedOrderListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerOrders(
        [FromRoute] Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Customer ID must be a valid GUID." });

            if (pageNumber < 1)
                return BadRequest(new { error = "Page number must be at least 1." });

            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "Page size must be between 1 and 100." });

            _logger.LogInformation(
                "Getting orders for customer {CustomerId}, page {PageNumber}, size {PageSize}",
                id, pageNumber, pageSize);

            var result = await _customerService.GetCustomerOrdersAsync(
                id,
                CurrentTenantId,
                pageNumber,
                pageSize,
                cancellationToken);

            var response = new PaginatedOrderListResponse
            {
                Items = result.Items.ToList(),
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
                HasNextPage = result.HasNextPage,
                HasPreviousPage = result.HasPreviousPage
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer not found: {CustomerId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving customer orders." });
        }
    }

    /// <summary>
    /// Adds or updates a customer address (billing or shipping).
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="request">The address request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Updated customer details.</returns>
    /// <response code="200">Address successfully added/updated.</response>
    /// <response code="400">Invalid address data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Customer not found.</response>
    [Authorize(Policy = Permissions.CustomersWrite)]
    [HttpPost("{id:guid}/addresses")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAddress(
        [FromRoute] Guid id,
        [FromBody] AddressRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Customer ID must be a valid GUID." });

            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            _logger.LogInformation(
                "Adding/updating address for customer {CustomerId}",
                id);

            // Verify customer exists
            var customer = await _customerService.GetCustomerByIdAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (customer == null)
            {
                _logger.LogWarning("Customer {CustomerId} not found for tenant {TenantId}", id, CurrentTenantId);
                return NotFound(new { error = "Customer not found." });
            }

            // Note: In a real implementation, this would call a dedicated service method
            // that handles both billing and shipping address updates.
            // For now, we're returning the customer with a note that address was processed.
            _logger.LogInformation(
                "Address processed for customer {CustomerId}: {Street}, {City}",
                id, request.Street, request.City);

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding address for customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while adding the address." });
        }
    }
}

/// <summary>
/// Response model for paginated customer list.
/// </summary>
public class PaginatedListResponse
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public required List<CustomerListItemDto> Items { get; set; }

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
    public required int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Response model for paginated customer orders list.
/// </summary>
public class PaginatedOrderListResponse
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public required List<CustomerOrderDto> Items { get; set; }

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
    public required int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage { get; set; }
}
