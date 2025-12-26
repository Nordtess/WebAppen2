using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using WebApp.Domain.Identity;

namespace WebApp.Middleware;

/// <summary>
/// Tvingar inloggade användare att fylla i obligatoriska profilfält innan de kan använda webbplatsen.
/// Omdirigerar till /AccountProfile/Edit om fälten saknas.
/// </summary>
public class ProfileCompletionMiddleware
{
    private readonly RequestDelegate _next;

    public ProfileCompletionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager, ITempDataDictionaryFactory tempDataFactory)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Hoppa över kontrollen för sidor som måste vara åtkomliga (t.ex. inloggning, profilredigering och statiska filer).
            if (!path.StartsWith("/Identity", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/AccountProfile/Edit", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/AccountProfile/ChangePassword", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/EditCV", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/MyCv", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase))
            {
                var user = await userManager.GetUserAsync(context.User);

                if (user != null)
                {
                    var isProfileIncomplete = string.IsNullOrWhiteSpace(user.FirstName)
                        || string.IsNullOrWhiteSpace(user.LastName)
                        || string.IsNullOrWhiteSpace(user.City)
                        || string.IsNullOrWhiteSpace(user.PostalCode);

                    if (isProfileIncomplete)
                    {
                        // Set toast for the next request.
                        var tempData = tempDataFactory.GetTempData(context);
                        tempData["ToastTitle"] = "Välkommen!";
                        tempData["ToastMessage"] = "Komplettera ditt konto med dina personliga uppgifter så att du blir synlig för andra.";

                        context.Response.Redirect("/AccountProfile/Edit");
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}
