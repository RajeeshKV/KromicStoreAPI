namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;

/// <summary>
/// Controller for managing courier configurations.
/// </summary>
[ApiController]
[Route("api/v1/couriers")]
[Produces("application/json")]
[Authorize(Policy = Permissions.SettingsWrite)]
public class CourierController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CourierController> _logger;

    /// <summary>
    /// Initializes a new instance of the CourierController class.
    /// </summary>
    public CourierController(
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<CourierController> logger)
        : base(tenantProvider)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all couriers for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of couriers.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Courier>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCouriers(CancellationToken cancellationToken = default)
    {
        try
        {
            var couriers = await _unitOfWork.Couriers.FindAsync(
                c => c.TenantId == CurrentTenantId,
                cancellationToken);

            return Ok(couriers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving couriers for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving couriers." });
        }
    }

    /// <summary>
    /// Gets a specific courier by ID.
    /// </summary>
    /// <param name="id">The courier ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The courier details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Courier), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCourierById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var courier = (await _unitOfWork.Couriers.FindAsync(
                c => c.Id == id && c.TenantId == CurrentTenantId,
                cancellationToken)).FirstOrDefault();

            if (courier == null)
            {
                return NotFound(new { error = "Courier not found." });
            }

            return Ok(courier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courier {CourierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the courier." });
        }
    }

    /// <summary>
    /// Creates a new courier.
    /// </summary>
    /// <param name="request">The courier creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created courier.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Courier), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCourier(
        [FromBody] CreateCourierRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Courier name is required." });
            }

            var courier = Courier.Create(CurrentTenantId, request.Name, request.Description);

            await _unitOfWork.Couriers.AddAsync(courier, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created courier {CourierId} for tenant {TenantId}", courier.Id, CurrentTenantId);

            return CreatedAtAction(nameof(GetCourierById), new { id = courier.Id }, courier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating courier for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while creating the courier." });
        }
    }

    /// <summary>
    /// Updates an existing courier.
    /// </summary>
    /// <param name="id">The courier ID.</param>
    /// <param name="request">The courier update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated courier.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Courier), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateCourier(
        [FromRoute] Guid id,
        [FromBody] UpdateCourierRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var courier = (await _unitOfWork.Couriers.FindAsync(
                c => c.Id == id && c.TenantId == CurrentTenantId,
                cancellationToken)).FirstOrDefault();

            if (courier == null)
            {
                return NotFound(new { error = "Courier not found." });
            }

            courier.UpdateInfo(
                request.Name,
                request.Description,
                request.TrackingUrlTemplate,
                request.ContactPhone,
                request.ContactEmail,
                request.AverageDeliveryDays
            );

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated courier {CourierId} for tenant {TenantId}", id, CurrentTenantId);

            return Ok(courier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating courier {CourierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the courier." });
        }
    }

    /// <summary>
    /// Activates or deactivates a courier.
    /// </summary>
    /// <param name="id">The courier ID.</param>
    /// <param name="request">The activation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated courier.</returns>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(Courier), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateCourierStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateCourierStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var courier = (await _unitOfWork.Couriers.FindAsync(
                c => c.Id == id && c.TenantId == CurrentTenantId,
                cancellationToken)).FirstOrDefault();

            if (courier == null)
            {
                return NotFound(new { error = "Courier not found." });
            }

            if (request.IsActive)
            {
                courier.Activate();
            }
            else
            {
                courier.Deactivate();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated status for courier {CourierId} for tenant {TenantId}", id, CurrentTenantId);

            return Ok(courier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating courier status {CourierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the courier status." });
        }
    }

    /// <summary>
    /// Deletes a courier.
    /// </summary>
    /// <param name="id">The courier ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCourier(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var courier = (await _unitOfWork.Couriers.FindAsync(
                c => c.Id == id && c.TenantId == CurrentTenantId,
                cancellationToken)).FirstOrDefault();

            if (courier == null)
            {
                return NotFound(new { error = "Courier not found." });
            }

            _unitOfWork.Couriers.Delete(courier);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted courier {CourierId} for tenant {TenantId}", id, CurrentTenantId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting courier {CourierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the courier." });
        }
    }
}

/// <summary>
/// Request model for creating a courier.
/// </summary>
public class CreateCourierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Request model for updating a courier.
/// </summary>
public class UpdateCourierRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? TrackingUrlTemplate { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public int? AverageDeliveryDays { get; set; }
}

/// <summary>
/// Request model for updating courier status.
/// </summary>
public class UpdateCourierStatusRequest
{
    public bool IsActive { get; set; }
}
