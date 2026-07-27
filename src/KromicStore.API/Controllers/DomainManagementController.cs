namespace KromicStore.API.Controllers;

using KromicStore.API.Authorization;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

/// <summary>
/// Tenant domain management and ownership verification endpoints.
/// </summary>
[ApiController]
[Route("api/v1/domains")]
[Authorize(Policy = Permissions.DomainsRead)]
[Produces("application/json")]
public class DomainManagementController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DomainManagementController> _logger;

    public DomainManagementController(AppDbContext context, ILogger<DomainManagementController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListDomains(CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty)
            return Unauthorized(new { error = "Tenant context is required." });

        var domains = await _context.TenantDomains.AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.IsPrimary)
            .ThenBy(d => d.Domain)
            .Select(d => new
            {
                d.Id,
                d.Domain,
                d.IsPrimary,
                d.IsVerified,
                d.VerifiedAt,
                d.VerificationToken,
                d.SslStatus,
                expectedDnsRecord = new
                {
                    type = "TXT",
                    name = "_kromicstore." + d.Domain,
                    value = d.VerificationToken
                }
            })
            .ToListAsync(cancellationToken);

        return Ok(new { data = domains });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.DomainsWrite)]
    public async Task<IActionResult> AddDomain([FromBody] AddDomainRequest request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty)
            return Unauthorized(new { error = "Tenant context is required." });
        if (string.IsNullOrWhiteSpace(request.Domain))
            return BadRequest(new { error = "Domain is required." });

        var normalized = NormalizeDomain(request.Domain);
        var exists = await _context.TenantDomains.AnyAsync(d => d.Domain == normalized, cancellationToken);
        if (exists)
            return Conflict(new { error = "Domain is already registered." });

        var domain = TenantDomain.Create(tenantId, normalized, request.IsPrimary);
        await _context.TenantDomains.AddAsync(domain, cancellationToken);
        await AddAuditAsync(tenantId, "Domain.Added", null, normalized, request.Reason, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Domain {Domain} added for tenant {TenantId}", normalized, tenantId);

        return CreatedAtAction(nameof(ListDomains), new
        {
            domain.Id,
            domain.Domain,
            domain.VerificationToken,
            expectedDnsRecord = new { type = "TXT", name = "_kromicstore." + domain.Domain, value = domain.VerificationToken }
        });
    }

    [HttpPost("{domainId:guid}/verify")]
    [Authorize(Policy = Permissions.DomainsWrite)]
    public async Task<IActionResult> VerifyDomain(Guid domainId, [FromBody] VerifyDomainRequest request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var domain = await _context.TenantDomains.FirstOrDefaultAsync(d => d.Id == domainId && d.TenantId == tenantId, cancellationToken);
        if (domain == null)
            return NotFound(new { error = "Domain not found." });

        if (!string.Equals(domain.VerificationToken, request.VerificationToken?.Trim(), StringComparison.Ordinal))
        {
            return BadRequest(new { error = "Verification token does not match.", expectedDnsRecord = new { type = "TXT", name = "_kromicstore." + domain.Domain, value = domain.VerificationToken } });
        }

        domain.MarkAsVerified();
        await AddAuditAsync(tenantId, "Domain.Verified", "false", "true", request.Reason, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { domain.Id, domain.Domain, domain.IsVerified, domain.VerifiedAt, domain.SslStatus });
    }

    [HttpPost("{domainId:guid}/rotate-token")]
    [Authorize(Policy = Permissions.DomainsWrite)]
    public async Task<IActionResult> RotateVerificationToken(Guid domainId, [FromBody] DomainReasonRequest request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var domain = await _context.TenantDomains.FirstOrDefaultAsync(d => d.Id == domainId && d.TenantId == tenantId, cancellationToken);
        if (domain == null)
            return NotFound(new { error = "Domain not found." });

        var oldToken = domain.VerificationToken;
        domain.MarkAsUnverified();
        await AddAuditAsync(tenantId, "Domain.VerificationTokenRotated", oldToken, domain.VerificationToken, request.Reason, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { domain.Id, domain.Domain, domain.VerificationToken, domain.IsVerified });
    }

    [HttpDelete("{domainId:guid}")]
    [Authorize(Policy = Permissions.DomainsWrite)]
    public async Task<IActionResult> DeleteDomain(Guid domainId, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var domain = await _context.TenantDomains.FirstOrDefaultAsync(d => d.Id == domainId && d.TenantId == tenantId, cancellationToken);
        if (domain == null)
            return NotFound(new { error = "Domain not found." });

        _context.TenantDomains.Remove(domain);
        await AddAuditAsync(tenantId, "Domain.Deleted", domain.Domain, null, null, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
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

    private async Task AddAuditAsync(Guid tenantId, string key, string? oldValue, string? newValue, string? reason, CancellationToken cancellationToken)
    {
        await _context.ConfigurationAuditLogs.AddAsync(ConfigurationAuditLog.Create(tenantId, key, oldValue, newValue, GetActorId(), reason), cancellationToken);
    }

    private static string NormalizeDomain(string domain)
    {
        return domain.Trim().TrimEnd('.').ToLowerInvariant();
    }
}

public sealed record AddDomainRequest(string Domain, bool IsPrimary = false, string? Reason = null);
public sealed record VerifyDomainRequest(string VerificationToken, string? Reason = null);
public sealed record DomainReasonRequest(string? Reason = null);