using Microsoft.AspNetCore.Identity;
using WebApp.Domain.Identity;

namespace WebApp.Middleware;

/// <summary>
/// Forces logged-in users to complete required profile fields before using the site.
/// Redirects to /AccountProfile/Edit if required fields are missing.
/// </summary>
public class ProfileCompletionMiddleware
{
    private readonly RequestDelegate _next;

    public ProfileCompletionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        // Only enforce for authenticated users
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Allow Identity/account endpoints and static files
            if (!path.StartsWith("/Identity", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/AccountProfile/Edit", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/AccountProfile/ChangePassword", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase))
            {
                var user = await userManager.GetUserAsync(context.User);

                if (user != null)
                {
                    var missing = string.IsNullOrWhiteSpace(user.FirstName)
                                  || string.IsNullOrWhiteSpace(user.LastName)
                                  || string.IsNullOrWhiteSpace(user.City)
                                  || string.IsNullOrWhiteSpace(user.PostalCode);

                    if (missing)
                    {
                        context.Response.Redirect("/AccountProfile/Edit");
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}
