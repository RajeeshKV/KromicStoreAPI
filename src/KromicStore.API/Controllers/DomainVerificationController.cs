// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Domains;
using KromicStore.Domain.Entities;

/// <summary>
/// Controller for domain verification and SSL management.
/// </summary>
[ApiController]
[Route("api/v1/domains")]
[Authorize(Roles = "TenantAdmin")]
[Produces("application/json")]
public class DomainVerificationController : BaseController
{
    private readonly IDomainVerificationService _domainVerificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DomainVerificationController> _logger;

    public DomainVerificationController(
        ITenantProvider tenantProvider,
        IDomainVerificationService domainVerificationService,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<DomainVerificationController> logger)
        : base(tenantProvider)
    {
        _domainVerificationService = domainVerificationService ?? throw new ArgumentNullException(nameof(domainVerificationService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Verifies domain ownership via DNS TXT record.
    /// </summary>
    /// <param name="domainId">The domain ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Verification result.</returns>
    /// <response code="200">Verification completed.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">Domain not found.</response>
    /// <response code="500">Server error.</response>
    [HttpPost("{domainId}/verify-dns")]
    [ProducesResponseType(typeof(DomainVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyDomain(
        Guid domainId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying domain {DomainId} for tenant {TenantId}", domainId, CurrentTenantId);

            var domain = await _unitOfWork.TenantDomains.GetByIdAsync(domainId, cancellationToken);
            if (domain == null || domain.TenantId != CurrentTenantId)
            {
                return NotFound(new { error = "Domain not found" });
            }

            var isVerified = await _domainVerificationService.VerifyDomainOwnershipAsync(
                domain.Domain,
                domain.VerificationToken,
                cancellationToken);

            if (isVerified)
            {
                domain.MarkAsVerified();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Log audit entry
                var userId = GetCurrentUserId();
                await _auditLogService.LogActionAsync(
                    CurrentTenantId,
                    userId,
                    "User",
                    "TenantDomain",
                    domainId,
                    "Verify",
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Domain {DomainId} verified successfully", domainId);

                return Ok(new DomainVerificationResponse
                {
                    DomainId = domainId,
                    Domain = domain.Domain,
                    IsVerified = true,
                    VerifiedAt = domain.VerifiedAt,
                    Message = "Domain ownership verified successfully"
                });
            }
            else
            {
                return Ok(new DomainVerificationResponse
                {
                    DomainId = domainId,
                    Domain = domain.Domain,
                    IsVerified = false,
                    Message = "Domain ownership verification failed. Please check your DNS TXT record."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying domain {DomainId}", domainId);
            return StatusCode(500, new { error = "An error occurred while verifying the domain" });
        }
    }

    /// <summary>
    /// Checks DNS configuration for a domain.
    /// </summary>
    /// <param name="domainId">The domain ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>DNS configuration status.</returns>
    /// <response code="200">DNS check completed.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">Domain not found.</response>
    /// <response code="500">Server error.</response>
    [HttpPost("{domainId}/check-dns")]
    [ProducesResponseType(typeof(DnsCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckDns(
        Guid domainId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking DNS configuration for domain {DomainId}", domainId);

            var domain = await _unitOfWork.TenantDomains.GetByIdAsync(domainId, cancellationToken);
            if (domain == null || domain.TenantId != CurrentTenantId)
            {
                return NotFound(new { error = "Domain not found" });
            }

            var isConfigured = await _domainVerificationService.CheckDnsConfigurationAsync(
                domain.Domain,
                cancellationToken);

            _logger.LogInformation("DNS check completed for domain {DomainId}: {Result}", domainId, isConfigured);

            return Ok(new DnsCheckResponse
            {
                DomainId = domainId,
                Domain = domain.Domain,
                IsConfigured = isConfigured,
                Message = isConfigured 
                    ? "DNS is correctly configured" 
                    : "DNS is not configured. Please add a CNAME record pointing to your platform."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking DNS for domain {DomainId}", domainId);
            return StatusCode(500, new { error = "An error occurred while checking DNS configuration" });
        }
    }

    /// <summary>
    /// Initiates SSL certificate provisioning for a domain.
    /// </summary>
    /// <param name="domainId">The domain ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>SSL provisioning status.</returns>
    /// <response code="200">SSL provisioning initiated.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">Domain not found.</response>
    /// <response code="500">Server error.</response>
    [HttpPost("{domainId}/provision-ssl")]
    [ProducesResponseType(typeof(SslProvisioningResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProvisionSsl(
        Guid domainId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initiating SSL provisioning for domain {DomainId}", domainId);

            var domain = await _unitOfWork.TenantDomains.GetByIdAsync(domainId, cancellationToken);
            if (domain == null || domain.TenantId != CurrentTenantId)
            {
                return NotFound(new { error = "Domain not found" });
            }

            if (!domain.IsVerified)
            {
                return BadRequest(new { error = "Domain must be verified before SSL provisioning" });
            }

            var status = await _domainVerificationService.InitiateSslProvisioningAsync(
                domain.Domain,
                cancellationToken);

            domain.UpdateSslStatus(status);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log audit entry
            var userId = GetCurrentUserId();
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "TenantDomain",
                domainId,
                "ProvisionSsl",
                cancellationToken: cancellationToken);

            _logger.LogInformation("SSL provisioning initiated for domain {DomainId} with status {Status}", domainId, status);

            return Ok(new SslProvisioningResponse
            {
                DomainId = domainId,
                Domain = domain.Domain,
                Status = status,
                Message = $"SSL provisioning {status.ToLower()}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning SSL for domain {DomainId}", domainId);
            return StatusCode(500, new { error = "An error occurred while provisioning SSL" });
        }
    }

    /// <summary>
    /// Checks SSL certificate status for a domain.
    /// </summary>
    /// <param name="domainId">The domain ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>SSL status.</returns>
    /// <response code="200">SSL status retrieved.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">Domain not found.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("{domainId}/ssl-status")]
    [ProducesResponseType(typeof(SslStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSslStatus(
        Guid domainId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking SSL status for domain {DomainId}", domainId);

            var domain = await _unitOfWork.TenantDomains.GetByIdAsync(domainId, cancellationToken);
            if (domain == null || domain.TenantId != CurrentTenantId)
            {
                return NotFound(new { error = "Domain not found" });
            }

            var status = await _domainVerificationService.CheckSslStatusAsync(
                domain.Domain,
                cancellationToken);

            // Update the stored status
            domain.UpdateSslStatus(status);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("SSL status for domain {DomainId}: {Status}", domainId, status);

            return Ok(new SslStatusResponse
            {
                DomainId = domainId,
                Domain = domain.Domain,
                Status = status,
                Message = $"SSL certificate is {status.ToLower()}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SSL status for domain {DomainId}", domainId);
            return StatusCode(500, new { error = "An error occurred while checking SSL status" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value 
            ?? User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
