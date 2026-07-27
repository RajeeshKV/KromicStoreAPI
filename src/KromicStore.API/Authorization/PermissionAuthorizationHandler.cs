namespace KromicStore.API.Authorization;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Validates explicit permission claims and role-derived permissions.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private static readonly string[] TenantOwnerPermissions =
    {
        Permissions.ProductsRead, Permissions.ProductsWrite,
        Permissions.OrdersRead, Permissions.OrdersWrite,
        Permissions.CustomersRead, Permissions.CustomersWrite,
        Permissions.ThemesRead, Permissions.ThemesWrite,
        Permissions.StoreRead, Permissions.StoreWrite,
        Permissions.BillingRead, Permissions.BillingWrite,
        Permissions.AnalyticsRead,
        Permissions.StaffRead, Permissions.StaffWrite,
        Permissions.SettingsRead, Permissions.SettingsWrite,
        Permissions.DomainsRead, Permissions.DomainsWrite
    };

    private static readonly string[] AllPermissions = TenantOwnerPermissions;

    private static readonly IReadOnlyDictionary<string, string[]> RolePermissions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["SuperUser"] = AllPermissions,
        ["PlatformAdmin"] = AllPermissions,
        ["TenantAdmin"] = TenantOwnerPermissions,
        ["TenantOwner"] = TenantOwnerPermissions,
        ["StoreManager"] = new[]
        {
            Permissions.ProductsRead, Permissions.ProductsWrite,
            Permissions.OrdersRead, Permissions.OrdersWrite,
            Permissions.CustomersRead, Permissions.CustomersWrite,
            Permissions.StoreRead, Permissions.StoreWrite,
            Permissions.AnalyticsRead,
            Permissions.SettingsRead,
            Permissions.DomainsRead
        },
        ["CatalogEditor"] = new[]
        {
            Permissions.ProductsRead, Permissions.ProductsWrite,
            Permissions.ThemesRead,
            Permissions.StoreRead
        },
        ["Support"] = new[]
        {
            Permissions.OrdersRead,
            Permissions.CustomersRead,
            Permissions.ProductsRead,
            Permissions.StoreRead
        },
        ["Customer"] = new[]
        {
            Permissions.ProductsRead,
            Permissions.OrdersRead,
            Permissions.CustomersRead,
            Permissions.StoreRead,
            Permissions.ThemesRead
        }
    };

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission) ||
            context.User.HasClaim("permissions", requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var roles = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Concat(context.User.FindAll("role").Select(c => c.Value));

        if (roles.Any(role => RolePermissions.TryGetValue(role, out var permissions) &&
                              permissions.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}