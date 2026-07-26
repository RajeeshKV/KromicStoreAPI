namespace KromicStore.API.Configuration;

using Hangfire.Dashboard;

/// <summary>
/// Authorization filter for Hangfire dashboard access.
/// Restricts dashboard access to SuperUser role only.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <summary>
    /// Authorizes dashboard access based on user role.
    /// </summary>
    /// <param name="context">Dashboard context containing HTTP context.</param>
    /// <returns>True if user is authorized, false otherwise.</returns>
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? false)
        {
            return false;
        }

        // Check if user has SuperUser role
        var isSuperUser = httpContext.User.IsInRole("SuperUser") || 
                         httpContext.User.IsInRole("Admin");

        if (!isSuperUser)
        {
            // Return 403 Forbidden
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        }

        return isSuperUser;
    }
}
