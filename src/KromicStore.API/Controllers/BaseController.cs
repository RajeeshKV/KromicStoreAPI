namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;

/// <summary>
/// Base controller with common functionality.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Gets the current tenant provider.
    /// </summary>
    protected ITenantProvider TenantProvider { get; }

    /// <summary>
    /// Initializes a new instance of the BaseController class.
    /// </summary>
    protected BaseController(ITenantProvider tenantProvider)
    {
        TenantProvider = tenantProvider;
    }

    /// <summary>
    /// Gets the current tenant ID.
    /// </summary>
    protected Guid CurrentTenantId => TenantProvider.TenantId;
}
