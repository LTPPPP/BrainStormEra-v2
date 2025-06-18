using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using BusinessLogicLayer;
namespace BrainStormEra_MVC
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add HttpContextAccessor for services that need access to HTTP context
            builder.Services.AddHttpContextAccessor();

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

            // Register ALL Repositories from DataAccessLayer (14 repositories)
            builder.Services.AddScoped(typeof(DataAccessLayer.Repositories.Interfaces.IBaseRepo<>), typeof(DataAccessLayer.Repositories.BaseRepo<>));
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IAuthRepo, DataAccessLayer.Repositories.AuthRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IUserRepo, DataAccessLayer.Repositories.UserRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.ICourseRepo, DataAccessLayer.Repositories.CourseRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IChapterRepo, DataAccessLayer.Repositories.ChapterRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.ILessonRepo, DataAccessLayer.Repositories.LessonRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IQuizRepo, DataAccessLayer.Repositories.QuizRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IQuestionRepo, DataAccessLayer.Repositories.QuestionRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.INotificationRepo, DataAccessLayer.Repositories.NotificationRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IAchievementRepo, DataAccessLayer.Repositories.AchievementRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.ICertificateRepo, DataAccessLayer.Repositories.CertificateRepo>();

            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IChatbotRepo, DataAccessLayer.Repositories.ChatbotRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.ISafeDeleteRepo, DataAccessLayer.Repositories.SafeDeleteRepo>();            // Register Services with SOLID principles - Now using BusinessLogicLayer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IUserService, BusinessLogicLayer.Services.UserService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ICourseService, BusinessLogicLayer.Services.CourseService>();

            // Register Course Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.CourseServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IChapterService, BusinessLogicLayer.Services.ChapterService>();

            // Register Chapter Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.ChapterServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ILessonService, BusinessLogicLayer.Services.LessonService>();

            // Register Lesson Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.LessonServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IQuestionService, BusinessLogicLayer.Services.QuestionService>();

            // Register Question Service Implementation for business logic layer  
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.QuestionServiceImpl>();

            // Register Quiz Service and Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IQuizService, BusinessLogicLayer.Services.Implementations.QuizServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.QuizServiceImpl>();

            // Register Achievement Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.AchievementServiceImpl>();

            // Register Certificate Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.CertificateServiceImpl>();

            // Register Notification Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.NotificationServiceImpl>();

            // Register Auth Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.AuthServiceImpl>();            // Register Home Services for data access and business logic
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IHomeService, BusinessLogicLayer.Services.HomeService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.HomeServiceImpl>();

            // Register Recommendation Helper
            builder.Services.AddScoped<BusinessLogicLayer.Services.RecommendationHelper>();

            // Register Services for data access and business logic


            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IEnrollmentService, BusinessLogicLayer.Services.EnrollmentService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAchievementService, BusinessLogicLayer.Services.AchievementService>();

            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ICertificateService, BusinessLogicLayer.Services.CertificateService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IUserContextService, BusinessLogicLayer.Services.UserContextService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IResponseService, BusinessLogicLayer.Services.ResponseService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.INotificationService, BusinessLogicLayer.Services.NotificationService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAvatarService, BusinessLogicLayer.Services.AvatarService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ICourseImageService, BusinessLogicLayer.Services.CourseImageService>();
            builder.Services.AddSingleton<BusinessLogicLayer.Services.Interfaces.ICacheService, BusinessLogicLayer.Services.CacheService>();

            // Add Safe Delete Service for secure delete operations
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ISafeDeleteService, BusinessLogicLayer.Services.SafeDeleteService>();

            // Add Chatbot Service
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IChatbotService, BusinessLogicLayer.Services.ChatbotService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IPageContextService, BusinessLogicLayer.Services.PageContextService>();
            builder.Services.AddHttpClient<BusinessLogicLayer.Services.ChatbotService>();

            // Register Chatbot Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.ChatbotServiceImpl>();

            // Register SafeDelete Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.SafeDeleteServiceImpl>();

            // Register Media Path Service for centralized media path management
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IMediaPathService, BusinessLogicLayer.Services.MediaPathService>();

            // Register URL Hash Service for secure URL handling
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IUrlHashService, BusinessLogicLayer.Services.UrlHashServiceImproved>();

            // Seed services
            builder.Services.AddScoped<BusinessLogicLayer.Services.StatusSeedService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.LessonTypeSeedService>();

            // Add Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    options.SlidingExpiration = true;
                    options.Cookie.Name = "BrainStormEraAuth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                });            // Add Authorization
            builder.Services.AddAuthorization(options =>
            {

                options.AddPolicy("InstructorOnly", policy => policy.RequireRole("instructor", "Instructor"));
                options.AddPolicy("LearnerOnly", policy => policy.RequireRole("learner", "Learner"));
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

                        // Seed statuses if they don't exist
                        var statusSeeder = scope.ServiceProvider.GetRequiredService<BusinessLogicLayer.Services.StatusSeedService>();
                        await statusSeeder.SeedStatusesAsync();

                        // Seed lesson types if they don't exist
                        var lessonTypeSeeder = scope.ServiceProvider.GetRequiredService<BusinessLogicLayer.Services.LessonTypeSeedService>();
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

            // Configure default static files from wwwroot
            app.UseStaticFiles();

            // Configure additional static files from SharedMedia folder
            var sharedMediaPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SharedMedia");
            var absoluteSharedMediaPath = Path.GetFullPath(sharedMediaPath);

            // Log the path for debugging
            Console.WriteLine($"SharedMedia path: {absoluteSharedMediaPath}");
            Console.WriteLine($"SharedMedia exists: {Directory.Exists(absoluteSharedMediaPath)}");

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(absoluteSharedMediaPath),
                RequestPath = "/SharedMedia",
                OnPrepareResponse = ctx =>
                {
                    // Add caching headers for better performance
                    ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
                }
            });

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

            // Test endpoint for debugging SharedMedia
            app.MapGet("/debug/sharedmedia", () =>
            {
                var sharedMediaPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SharedMedia");
                var absoluteSharedMediaPath = Path.GetFullPath(sharedMediaPath);
                return Results.Ok(new
                {
                    CurrentDirectory = Directory.GetCurrentDirectory(),
                    SharedMediaPath = sharedMediaPath,
                    AbsoluteSharedMediaPath = absoluteSharedMediaPath,
                    Exists = Directory.Exists(absoluteSharedMediaPath),
                    Files = Directory.Exists(absoluteSharedMediaPath)
                        ? Directory.GetFiles(absoluteSharedMediaPath, "*", SearchOption.AllDirectories).Take(10).ToArray()
                        : new string[0]
                });
            });

            // Test route for SharedMedia HTML
            app.MapGet("/test/sharedmedia", async () =>
            {
                var testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SharedMedia", "test.html");
                if (File.Exists(testFilePath))
                {
                    var content = await File.ReadAllTextAsync(testFilePath);
                    return Results.Content(content, "text/html");
                }
                return Results.NotFound("Test file not found");
            });            // Configure SignalR Hub
            app.MapHub<BusinessLogicLayer.Hubs.NotificationHub>("/notificationHub");

            app.Run();
        }
    }
}
