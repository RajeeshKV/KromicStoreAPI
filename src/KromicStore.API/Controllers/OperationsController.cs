namespace KromicStore.API.Controllers;

using KromicStore.API.Authorization;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

/// <summary>
/// SaaS operations settings: feature flags, maintenance mode, quotas, usage, and retention policy.
/// </summary>
[ApiController]
[Route("api/v1/operations")]
[Authorize(Policy = Permissions.SettingsRead)]
[Produces("application/json")]
public class OperationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public OperationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var settings = await _context.TenantConfigurations.AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.IsEncrypted &&
                        (c.ConfigKey.StartsWith("features:") ||
                         c.ConfigKey.StartsWith("maintenance:") ||
                         c.ConfigKey.StartsWith("quota:") ||
                         c.ConfigKey.StartsWith("retention:")))
            .OrderBy(c => c.ConfigKey)
            .Select(c => new { c.ConfigKey, c.ConfigValue, c.ExpiresAt })
            .ToListAsync(cancellationToken);

        return Ok(new { data = settings });
    }

    [HttpPut("feature-flags/{flagKey}")]
    [Authorize(Policy = Permissions.SettingsWrite)]
    public Task<IActionResult> SetFeatureFlag(string flagKey, [FromBody] ToggleSettingRequest request, CancellationToken cancellationToken)
    {
        return UpsertSettingAsync("features:" + NormalizeKey(flagKey), JsonSerializer.Serialize(request.Enabled), request.Reason, cancellationToken);
    }

    [HttpPut("maintenance")]
    [Authorize(Policy = Permissions.SettingsWrite)]
    public Task<IActionResult> SetMaintenanceMode([FromBody] MaintenanceModeRequest request, CancellationToken cancellationToken)
    {
        return UpsertSettingAsync("maintenance:mode", JsonSerializer.Serialize(new
        {
            request.Enabled,
            request.Message,
            request.StartsAt,
            request.EndsAt
        }), request.Reason, cancellationToken);
    }

    [HttpPut("quotas/{quotaKey}")]
    [Authorize(Policy = Permissions.SettingsWrite)]
    public Task<IActionResult> SetQuota(string quotaKey, [FromBody] QuotaSettingRequest request, CancellationToken cancellationToken)
    {
        if (request.Limit < 0)
            return Task.FromResult<IActionResult>(BadRequest(new { error = "Quota limit cannot be negative." }));

        return UpsertSettingAsync("quota:" + NormalizeKey(quotaKey), JsonSerializer.Serialize(new
        {
            request.Limit,
            request.Window,
            request.EnforceHardLimit
        }), request.Reason, cancellationToken);
    }

    [HttpPut("retention")]
    [Authorize(Policy = Permissions.SettingsWrite)]
    public Task<IActionResult> SetRetentionPolicy([FromBody] RetentionPolicyRequest request, CancellationToken cancellationToken)
    {
        if (request.RetentionDays <= 0)
            return Task.FromResult<IActionResult>(BadRequest(new { error = "Retention days must be positive." }));

        return UpsertSettingAsync("retention:policy", JsonSerializer.Serialize(request), request.Reason, cancellationToken);
    }

    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage(CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var usage = new
        {
            products = await _context.Products.CountAsync(p => p.TenantId == tenantId, cancellationToken),
            customers = await _context.Customers.CountAsync(c => c.TenantId == tenantId, cancellationToken),
            orders = await _context.Orders.CountAsync(o => o.TenantId == tenantId, cancellationToken),
            webhooks = await _context.WebhookConfigurations.CountAsync(w => w.TenantId == tenantId, cancellationToken),
            users = await _context.Users.CountAsync(u => u.TenantId == tenantId, cancellationToken)
        };

        return Ok(new { data = usage, generatedAt = DateTime.UtcNow });
    }

    private async Task<IActionResult> UpsertSettingAsync(string key, string value, string? reason, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty)
            return Unauthorized(new { error = "Tenant context is required." });

        var setting = await _context.TenantConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ConfigKey == key, cancellationToken);

        var oldValue = setting?.ConfigValue;
        if (setting == null)
        {
            setting = TenantConfiguration.Create(tenantId, key, value, ConfigScope.Tenant);
            await _context.TenantConfigurations.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Update(value);
        }

        await _context.ConfigurationAuditLogs.AddAsync(ConfigurationAuditLog.Create(tenantId, key, oldValue, value, GetActorId(), reason), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { key, value });
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var tenantId) ? tenantId : Guid.Empty;
    }

    private Guid GetActorId()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var actorId) ? actorId : Guid.Empty;
    }

    private static string NormalizeKey(string key)
    {
        return key.Trim().ToLowerInvariant().Replace(' ', '-');
    }
}

public sealed record ToggleSettingRequest(bool Enabled, string? Reason = null);
public sealed record MaintenanceModeRequest(bool Enabled, string? Message = null, DateTime? StartsAt = null, DateTime? EndsAt = null, string? Reason = null);
public sealed record QuotaSettingRequest(int Limit, string Window = "monthly", bool EnforceHardLimit = true, string? Reason = null);
public sealed record RetentionPolicyRequest(int RetentionDays, string Scope = "tenant", string? Reason = null);