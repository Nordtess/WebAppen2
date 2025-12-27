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

        // EF Core (DbContext lives in Infrastructure) + SQL Server
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

        // App services
        builder.Services.AddScoped<IUnreadMessagesService, UnreadMessagesService>();
        // Account deletion service (scoped because it uses DbContext)
        builder.Services.AddScoped<AccountDeletionService>();

        var app = builder.Build();

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
