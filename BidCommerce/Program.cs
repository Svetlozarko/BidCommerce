using BidCommerce.Data;
using BidCommerce.Interfaces;
using BidCommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;  // Redis namespace

namespace BidCommerce
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<BidDb>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Identity with roles
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddEntityFrameworkStores<BidDb>()
            .AddDefaultUI()
            .AddDefaultTokenProviders();

            builder.Services.AddControllersWithViews();

            // *** Updated Redis connection registration ***
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configurationString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379,abortConnect=false";
                var options = ConfigurationOptions.Parse(configurationString);
                options.AbortOnConnectFail = false;  // critical to avoid app crash on startup
                return ConnectionMultiplexer.Connect(options);
            });

            // Register your Redis caching service
            builder.Services.AddScoped<ICategoryCountCacheService, CategoryCountCacheService>();

            var app = builder.Build();

            // Seed roles and admin user (your existing code)
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                // Your seed logic here
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
