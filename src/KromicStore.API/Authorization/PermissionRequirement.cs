namespace KromicStore.API.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Authorization requirement for permission-based RBAC.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}