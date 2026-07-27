namespace KromicStore.API.Controllers;

using Hangfire;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

/// <summary>
/// SuperUser platform operations for tenant lifecycle, analytics, global search, and operational visibility.
/// </summary>
[ApiController]
[Route("api/v1/superuser/platform")]
[Authorize(Policy = "SuperUserOnly")]
[Produces("application/json")]
public class SuperUserPlatformController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SuperUserPlatformController> _logger;

    public SuperUserPlatformController(AppDbContext context, ILogger<SuperUserPlatformController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> ListTenants([FromQuery] string? status, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = _context.Tenants.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(t => t.Name.ToLower().Contains(term) || t.ContactEmail.ToLower().Contains(term) || t.Subdomain.ToLower().Contains(term));
        }

        query = status?.ToLowerInvariant() switch
        {
            "active" => query.Where(t => t.IsActive && !t.IsArchived && !t.IsDeleted),
            "suspended" => query.Where(t => !t.IsActive && !t.IsArchived && !t.IsDeleted),
            "archived" => query.Where(t => t.IsArchived),
            "deleted" => query.Where(t => t.IsDeleted),
            _ => query
        };

        var tenants = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.TenantId,
                t.Name,
                t.Subdomain,
                t.ContactEmail,
                t.SubscriptionPlan,
                status = GetTenantStatus(t),
                t.CreatedAt,
                t.UpdatedAt,
                t.SuspendedAt,
                t.ArchivedAt,
                t.DeletedAt,
                t.LifecycleReason
            })
            .ToListAsync(cancellationToken);

        return Ok(new { data = tenants });
    }

    [HttpPost("tenants/{tenantId:guid}/suspend")]
    public Task<IActionResult> SuspendTenant(Guid tenantId, [FromBody] LifecycleRequest request, CancellationToken cancellationToken)
    {
        return ApplyLifecycleAsync(tenantId, "Suspended", tenant => tenant.Suspend(request.Reason), request.Reason, cancellationToken);
    }

    [HttpPost("tenants/{tenantId:guid}/archive")]
    public Task<IActionResult> ArchiveTenant(Guid tenantId, [FromBody] LifecycleRequest request, CancellationToken cancellationToken)
    {
        return ApplyLifecycleAsync(tenantId, "Archived", tenant => tenant.Archive(request.Reason), request.Reason, cancellationToken);
    }

    [HttpPost("tenants/{tenantId:guid}/delete")]
    public Task<IActionResult> SoftDeleteTenant(Guid tenantId, [FromBody] LifecycleRequest request, CancellationToken cancellationToken)
    {
        return ApplyLifecycleAsync(tenantId, "Deleted", tenant => tenant.SoftDelete(request.Reason), request.Reason, cancellationToken);
    }

    [HttpPost("tenants/{tenantId:guid}/restore")]
    public Task<IActionResult> RestoreTenant(Guid tenantId, [FromBody] LifecycleRequest request, CancellationToken cancellationToken)
    {
        return ApplyLifecycleAsync(tenantId, "Restored", tenant => tenant.Restore(request.Reason), request.Reason, cancellationToken);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(CancellationToken cancellationToken)
    {
        var tenantCounts = await _context.Tenants.AsNoTracking()
            .GroupBy(t => new { t.IsActive, t.IsArchived, t.IsDeleted })
            .Select(g => new { g.Key.IsActive, g.Key.IsArchived, g.Key.IsDeleted, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var subscriptionCounts = await _context.Subscriptions.AsNoTracking()
            .GroupBy(s => new { s.PlanType, s.Status })
            .Select(g => new { Plan = g.Key.PlanType, g.Key.Status, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var storageApproximation = await _context.Products.AsNoTracking().CountAsync(cancellationToken)
            + await _context.Orders.AsNoTracking().CountAsync(cancellationToken)
            + await _context.Customers.AsNoTracking().CountAsync(cancellationToken);

        return Ok(new
        {
            tenants = tenantCounts,
            subscriptions = subscriptionCounts,
            activeTenants = await _context.Tenants.CountAsync(t => t.IsActive && !t.IsArchived && !t.IsDeleted, cancellationToken),
            storageUsageUnits = storageApproximation,
            generatedAt = DateTime.UtcNow
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> GlobalSearch([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Search query is required." });

        var term = q.Trim().ToLowerInvariant();
        var tenants = await _context.Tenants.AsNoTracking()
            .Where(t => t.Name.ToLower().Contains(term) || t.ContactEmail.ToLower().Contains(term) || t.Subdomain.ToLower().Contains(term))
            .Select(t => new { type = "tenant", t.Id, label = t.Name, secondary = t.ContactEmail })
            .Take(10)
            .ToListAsync(cancellationToken);

        var products = await _context.Products.AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term))
            .Select(p => new { type = "product", p.Id, label = p.Name, secondary = p.Sku })
            .Take(10)
            .ToListAsync(cancellationToken);

        var customers = await _context.Customers.AsNoTracking()
            .Where(c => c.Email.ToLower().Contains(term) || c.FirstName.ToLower().Contains(term) || c.LastName.ToLower().Contains(term))
            .Select(c => new { type = "customer", c.Id, label = c.FirstName + " " + c.LastName, secondary = c.Email })
            .Take(10)
            .ToListAsync(cancellationToken);

        return Ok(new { data = tenants.Concat<object>(products).Concat(customers) });
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(CancellationToken cancellationToken)
    {
        var logs = await _context.ConfigurationAuditLogs.AsNoTracking()
            .OrderByDescending(l => l.ChangedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return Ok(new { data = logs });
    }

    [HttpGet("jobs")]
    public IActionResult GetBackgroundJobs()
    {
        try
        {
            var api = JobStorage.Current.GetMonitoringApi();
            return Ok(new
            {
                queues = api.Queues(),
                processing = api.ProcessingCount(),
                scheduled = api.ScheduledCount(),
                failed = api.FailedCount(),
                succeeded = api.SucceededListCount(),
                servers = api.Servers()
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to read Hangfire monitoring API");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Background job monitoring is not available." });
        }
    }

    private async Task<IActionResult> ApplyLifecycleAsync(Guid tenantId, string action, Action<Tenant> change, string? reason, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant == null)
            return NotFound(new { error = "Tenant not found." });

        var oldStatus = GetTenantStatus(tenant);
        change(tenant);
        await AddAuditAsync(tenant.Id, $"TenantLifecycle.{action}", oldStatus, GetTenantStatus(tenant), reason, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { tenant.Id, status = GetTenantStatus(tenant), tenant.LifecycleReason });
    }

    private async Task AddAuditAsync(Guid tenantId, string key, string? oldValue, string? newValue, string? reason, CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        await _context.ConfigurationAuditLogs.AddAsync(ConfigurationAuditLog.Create(tenantId, key, oldValue, newValue, actorId, reason), cancellationToken);
    }

    private Guid GetActorId()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var actorId) ? actorId : Guid.Empty;
    }

    private static string GetTenantStatus(Tenant tenant)
    {
        if (tenant.IsDeleted) return "Deleted";
        if (tenant.IsArchived) return "Archived";
        return tenant.IsActive ? "Active" : "Suspended";
    }
}

public sealed record LifecycleRequest(string? Reason);