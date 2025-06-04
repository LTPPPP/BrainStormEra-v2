using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using BrainStormEra_MVC.Models;
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

            // Add SignalR
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            });

            // Register Services with SOLID principles
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.IUserService, BrainStormEra_MVC.Services.UserService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.ICourseService, BrainStormEra_MVC.Services.CourseService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.ICourseRepository, BrainStormEra_MVC.Services.Repositories.CourseRepository>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.IChapterService, BrainStormEra_MVC.Services.ChapterService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.ILessonService, BrainStormEra_MVC.Services.LessonService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.IEnrollmentService, BrainStormEra_MVC.Services.EnrollmentService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.IAchievementService, BrainStormEra_MVC.Services.AchievementService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.IAchievementRepository, BrainStormEra_MVC.Services.Repositories.AchievementRepository>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.ICertificateService, BrainStormEra_MVC.Services.CertificateService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.ICertificateRepository, BrainStormEra_MVC.Services.Repositories.CertificateRepository>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.IUserContextService, BrainStormEra_MVC.Services.UserContextService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.IResponseService, BrainStormEra_MVC.Services.ResponseService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.INotificationService, BrainStormEra_MVC.Services.NotificationService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.IAvatarService, BrainStormEra_MVC.Services.AvatarService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.ICourseImageService, BrainStormEra_MVC.Services.CourseImageService>();
            builder.Services.AddSingleton<BrainStormEra_MVC.Services.Interfaces.ICacheService, BrainStormEra_MVC.Services.CacheService>();

            // Add Safe Delete Service for secure delete operations
            builder.Services.AddScoped<BrainStormEra_MVC.Services.Interfaces.ISafeDeleteService, BrainStormEra_MVC.Services.SafeDeleteService>();

            // Seed services
            builder.Services.AddScoped<BrainStormEra_MVC.Services.CategorySeedService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.StatusSeedService>();
            builder.Services.AddScoped<BrainStormEra_MVC.Services.LessonTypeSeedService>();

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
                // Continue running the application but log the error
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

                        // Seed categories if they don't exist
                        var categorySeeder = scope.ServiceProvider.GetRequiredService<BrainStormEra_MVC.Services.CategorySeedService>();
                        await categorySeeder.SeedCategoriesAsync();

                        // Seed statuses if they don't exist
                        var statusSeeder = scope.ServiceProvider.GetRequiredService<BrainStormEra_MVC.Services.StatusSeedService>();
                        await statusSeeder.SeedStatusesAsync();

                        // Seed lesson types if they don't exist
                        var lessonTypeSeeder = scope.ServiceProvider.GetRequiredService<BrainStormEra_MVC.Services.LessonTypeSeedService>();
                        await lessonTypeSeeder.SeedLessonTypesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Database connection test failed, but continue running
                Console.WriteLine($"Database connection failed: {ex.Message}");
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
            app.UseAuthorization();

            // Configure routing
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Configure SignalR Hub
            app.MapHub<BrainStormEra_MVC.Hubs.NotificationHub>("/notificationHub");

            app.Run();
        }
    }
}
