using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Identity;
using WebApp.Infrastructure.Data;
using WebApp.Middleware;
using WebApp.Infrastructure.Services;
using WebApp.Services;

namespace WebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // MVC (Controllers + Views)
        builder.Services.AddControllersWithViews();

        // Required for Identity UI endpoints (/Identity/Account/...)
        builder.Services.AddRazorPages();

        // EF Core (DbContext lives in Infrastructure) + SQL Server LocalDB
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly("Infrastructure")));

        // Identity (cookie auth)
        builder.Services
            .AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // App services (DI)
        builder.Services.AddScoped<IUnreadMessagesService, UnreadMessagesService>();
        builder.Services.AddScoped<AccountDeletionService>();

        var app = builder.Build();

        // Apply migrations / create DB on first run for ALL registered DbContexts
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            try
            {
                // Migrate any DbContext that is registered in DI
                var contextTypes = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList();

                foreach (var ctxType in contextTypes)
                {
                    try
                    {
                        if (scope.ServiceProvider.GetService(ctxType) is DbContext ctx)
                        {
                            ctx.Database.Migrate();
                        }
                    }
                    catch (Exception exCtx)
                    {
                        logger.LogError(exCtx, "Failed to apply migrations for DbContext {DbContext}", ctxType.FullName);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply EF Core migrations at startup.");
                throw; // Surface failure so issues are visible during review
            }
        }

        // Pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        // Enforce user profile completion after login
        app.UseMiddleware<ProfileCompletionMiddleware>();

        // Identity UI endpoints
        app.MapRazorPages();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
