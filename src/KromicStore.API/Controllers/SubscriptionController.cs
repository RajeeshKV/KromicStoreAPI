namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Subscriptions;

/// <summary>
/// Controller for managing subscription plans and lifecycle.
/// </summary>
[ApiController]
[Route("api/v1/subscriptions")]
[Produces("application/json")]
[Authorize(Policy = Permissions.BillingWrite)]
public class SubscriptionController : BaseController
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUsageReportingService _usageReportingService;
    private readonly ILogger<SubscriptionController> _logger;

    /// <summary>
    /// Initializes a new instance of the SubscriptionController class.
    /// </summary>
    public SubscriptionController(
        ITenantProvider tenantProvider,
        ISubscriptionService subscriptionService,
        IUsageReportingService usageReportingService,
        ILogger<SubscriptionController> logger)
        : base(tenantProvider)
    {
        _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
        _usageReportingService = usageReportingService ?? throw new ArgumentNullException(nameof(usageReportingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current subscription details for the tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Current subscription details.</returns>
    /// <response code="200">Current subscription details successfully retrieved.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized (must be TenantAdmin or SuperUser).</response>
    /// <response code="404">Subscription not found.</response>
    [HttpGet("current")]
    [ProducesResponseType(typeof(CurrentSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentSubscription(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting current subscription for tenant {TenantId}",
                CurrentTenantId);

            var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(
                CurrentTenantId,
                cancellationToken);

            // Subscription is auto-created if not found, so this should never be null
            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current subscription for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the subscription." });
        }
    }

    /// <summary>
    /// Gets the current usage summary for the tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Current usage summary with quotas and exceeded status.</returns>
    /// <response code="200">Usage summary successfully retrieved.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized (must be TenantAdmin or SuperUser).</response>
    [HttpGet("current/usage")]
    [ProducesResponseType(typeof(UsageSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCurrentUsage(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting current usage for tenant {TenantId}",
                CurrentTenantId);

            var usageSummary = await _usageReportingService.GetUsageSummaryAsync(
                CurrentTenantId,
                cancellationToken);

            return Ok(usageSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current usage for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the usage." });
        }
    }

    /// <summary>
    /// Gets a list of all available subscription plans with feature comparison.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of available plans with comparison table.</returns>
    /// <response code="200">Plans successfully retrieved.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized (must be TenantAdmin or SuperUser).</response>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(PlansListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting available subscription plans for tenant {TenantId}",
                CurrentTenantId);

            var plansResponse = await _subscriptionService.GetAvailablePlansAsync(
                CurrentTenantId,
                cancellationToken);

            return Ok(plansResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plans for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the plans." });
        }
    }

    /// <summary>
    /// Upgrades the subscription to a higher plan.
    /// If a pro-rata charge is required, a payment is triggered automatically.
    /// </summary>
    /// <param name="request">The upgrade plan request containing the new plan ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Updated subscription details.</returns>
    /// <response code="200">Subscription successfully upgraded.</response>
    /// <response code="400">Invalid plan ID or cannot upgrade to same/lower tier.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized (must be TenantAdmin or SuperUser).</response>
    /// <response code="404">Subscription or target plan not found.</response>
    /// <response code="409">Subscription is already pending cancellation or in an invalid state.</response>
    [HttpPost("upgrade")]
    [ProducesResponseType(typeof(CurrentSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpgradePlan(
        [FromBody] UpgradePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            if (request.NewPlanId <= 0)
                return BadRequest(new { error = "New plan ID must be a positive integer." });

            _logger.LogInformation(
                "Upgrading subscription for tenant {TenantId} to plan {NewPlanId}",
                CurrentTenantId, request.NewPlanId);

            var result = await _subscriptionService.UpgradePlanAsync(
                CurrentTenantId,
                request.NewPlanId,
                request.EffectiveDate,
                cancellationToken);

            if (!result.Success)
            {
                // Determine the appropriate HTTP status code based on the error
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                if (result.Error?.Contains("cancellation", StringComparison.OrdinalIgnoreCase) == true ||
                    result.Error?.Contains("invalid state", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Conflict(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while upgrading the subscription." });
        }
    }

    /// <summary>
    /// Downgrades the subscription to a lower plan.
    /// A credit is applied to the next billing cycle for the price difference.
    /// </summary>
    /// <param name="request">The downgrade plan request containing the new plan ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Updated subscription details.</returns>
    /// <response code="200">Subscription successfully downgraded.</response>
    /// <response code="400">Invalid plan ID or cannot downgrade to same/higher tier.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized (must be TenantAdmin or SuperUser).</response>
    /// <response code="404">Subscription or target plan not found.</response>
    /// <response code="409">Subscription is already pending cancellation or in an invalid state.</response>
    [HttpPost("downgrade")]
    [ProducesResponseType(typeof(CurrentSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DowngradePlan(
        [FromBody] DowngradePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            if (request.NewPlanId <= 0)
                return BadRequest(new { error = "New plan ID must be a positive integer." });

            _logger.LogInformation(
                "Downgrading subscription for tenant {TenantId} to plan {NewPlanId}",
                CurrentTenantId, request.NewPlanId);

            var result = await _subscriptionService.DowngradePlanAsync(
                CurrentTenantId,
                request.NewPlanId,
                request.EffectiveDate,
                cancellationToken);

            if (!result.Success)
            {
                // Determine the appropriate HTTP status code based on the error
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                if (result.Error?.Contains("cancellation", StringComparison.OrdinalIgnoreCase) == true ||
                    result.Error?.Contains("invalid state", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Conflict(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downgrading subscription for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while downgrading the subscription." });
        }
    }

    /// <summary>
    /// Requests subscription cancellation with a 30-day grace period.
    /// After the grace period, the subscription will be fully deactivated.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Updated subscription details with cancellation details.</returns>
    /// <response code="200">Subscription cancellation successfully requested.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized (must be TenantAdmin or SuperUser).</response>
    /// <response code="404">Subscription not found.</response>
    /// <response code="409">Subscription is already pending cancellation or in an invalid state.</response>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(CurrentSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelSubscription(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Requesting cancellation for subscription of tenant {TenantId}",
                CurrentTenantId);

            var result = await _subscriptionService.CancelSubscriptionAsync(
                CurrentTenantId,
                cancellationToken);

            if (!result.Success)
            {
                // Determine the appropriate HTTP status code based on the error
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                if (result.Error?.Contains("already", StringComparison.OrdinalIgnoreCase) == true ||
                    result.Error?.Contains("invalid state", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Conflict(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while cancelling the subscription." });
        }
    }

    /// <summary>
    /// Reactivates a cancelled subscription during the 30-day grace period.
    /// After the grace period expires, the subscription cannot be reactivated.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Updated subscription details with active status.</returns>
    /// <response code="200">Subscription successfully reactivated.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized (must be TenantAdmin or SuperUser).</response>
    /// <response code="404">Subscription not found or is not pending cancellation.</response>
    /// <response code="409">Grace period has expired; subscription cannot be reactivated.</response>
    [HttpPost("reactivate")]
    [ProducesResponseType(typeof(CurrentSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateSubscription(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Requesting reactivation for subscription of tenant {TenantId}",
                CurrentTenantId);

            var result = await _subscriptionService.ReactivateSubscriptionAsync(
                CurrentTenantId,
                cancellationToken);

            if (!result.Success)
            {
                // Determine the appropriate HTTP status code based on the error
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ||
                    result.Error?.Contains("not pending", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                if (result.Error?.Contains("grace period", StringComparison.OrdinalIgnoreCase) == true ||
                    result.Error?.Contains("expired", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Conflict(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating subscription for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while reactivating the subscription." });
        }
    }
}
