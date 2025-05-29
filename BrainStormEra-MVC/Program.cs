using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BrainStormEra_MVC
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add Response Compression for better performance
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            // Add Memory Cache for better performance
            builder.Services.AddMemoryCache();

            // Add Response Caching
            builder.Services.AddResponseCaching();

            // Add SignalR for real-time chat
            builder.Services.AddSignalR();

            // Add Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login/Index";
                    options.LogoutPath = "/Login/Logout";
                    options.AccessDeniedPath = "/Login/Index";
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    options.SlidingExpiration = true;
                    options.Cookie.Name = "BrainStormEraAuth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                });

            // Add Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "admin"));
                options.AddPolicy("InstructorOnly", policy => policy.RequireRole("Instructor", "instructor"));
                options.AddPolicy("LearnerOnly", policy => policy.RequireRole("Learner", "learner"));
            });

            // Add session support
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            try
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("Connection string is missing or empty!");
                }

                builder.Services.AddDbContext<BrainStormEraContext>(options =>
                {
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(60); // Increased timeout for complex queries
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                    });
                    // Remove sensitive data logging in production for better performance
                    if (builder.Environment.IsDevelopment())
                    {
                        options.EnableSensitiveDataLogging();
                    }
                    options.EnableServiceProviderCaching();
                });
            }
            catch
            {
                // Vẫn tiếp tục chạy ứng dụng nhưng ghi lại lỗi
            }

            var app = builder.Build();

            // Test database connection
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<BrainStormEraContext>();
                    var canConnect = await context.Database.CanConnectAsync();
                    if (canConnect)
                    {
                        // Test a simple query to verify database structure
                        var accountCount = await context.Accounts.CountAsync();
                    }
                }
            }
            catch
            {
                // Database connection test failed
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            // Add status code pages handling - this will handle 404s
            app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

            // Enable response compression for better performance
            app.UseResponseCompression();

            // Enable response caching
            app.UseResponseCaching();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Add Session middleware before Authentication
            app.UseSession();

            // Add Authentication and Authorization middleware
            app.UseAuthentication();
            app.UseAuthorization(); app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Map SignalR hub
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}
