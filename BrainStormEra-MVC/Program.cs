using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using Rotativa.AspNetCore;
using BrainStormEra_MVC.Middlewares;
using BusinessLogicLayer.Utilities;
using StackExchange.Redis;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using BusinessLogicLayer.Services;

namespace BrainStormEra_MVC
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Load environment variables from .env file
            EnvironmentLoader.LoadEnvironmentFile();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add HttpContextAccessor for services that need access to HTTP context
            builder.Services.AddHttpContextAccessor();

            // === HIGH PERFORMANCE CONFIGURATIONS ===

            // Enhanced Response Compression for better performance
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = new[] { "text/plain", "text/html", "text/css", "text/javascript", "application/javascript", "application/json" };
            });

            // Add Memory Cache for better performance
            builder.Services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1024 * 1024 * 100; // 100MB
                options.CompactionPercentage = 0.2;
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
            });

            // === REDIS AND CACHING SETUP ===
            bool useRedis = false;
            try
            {
                // Try to connect to Redis
                var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
                var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);

                // Test Redis connection
                var database = connectionMultiplexer.GetDatabase();
                database.StringSet("test", "connection");
                var testValue = database.StringGet("test");

                if (testValue == "connection")
                {
                    useRedis = true;
                    builder.Services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

                    builder.Services.AddStackExchangeRedisCache(options =>
                    {
                        options.ConfigurationOptions = ConfigurationOptions.Parse(redisConnectionString);
                        options.InstanceName = "BrainStormEra";
                    });

                    Console.WriteLine("✅ Redis connected successfully - Using Redis services");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Redis connection failed: {ex.Message}");
                Console.WriteLine("📝 Falling back to in-memory services");
                useRedis = false;
            }

            // Fallback to memory cache if Redis is not available
            if (!useRedis)
            {
                builder.Services.AddDistributedMemoryCache();
            }

            // Add Response Caching with enhanced settings
            builder.Services.AddResponseCaching(options =>
            {
                options.MaximumBodySize = 1024 * 1024 * 10; // 10MB
                options.UseCaseSensitivePaths = false;
            });

            // Enhanced SignalR Configuration for High Load
            var signalRBuilder = builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
                options.StreamBufferCapacity = 10;
                options.MaximumParallelInvocationsPerClient = 5;
            });

            // Add Redis backplane only if Redis is available
            if (useRedis)
            {
                signalRBuilder.AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", options =>
                {
                    options.Configuration.ChannelPrefix = RedisChannel.Literal("BrainStormEra.SignalR");
                });
            }

            // === DATABASE CONFIGURATION WITH OPTIMIZATION ===

            // Enhanced Database Configuration with Connection Pooling
            builder.Services.AddDbContext<BrainStormEraContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);

                    sqlOptions.CommandTimeout(30);
                });

                // Performance optimizations
                options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
                options.EnableDetailedErrors(builder.Environment.IsDevelopment());
                options.EnableServiceProviderCaching();
                options.EnableSensitiveDataLogging(false); // Disable in production
            });

            // === MESSAGE QUEUE AND CHAT SERVICES ===

            // Register Message Queue Service based on Redis availability
            if (useRedis)
            {
                builder.Services.AddScoped<IMessageQueueService, RedisMessageQueueService>();
                Console.WriteLine("📨 Using Redis Message Queue Service");
            }
            else
            {
                builder.Services.AddScoped<IMessageQueueService, InMemoryMessageQueueService>();
                Console.WriteLine("📨 Using In-Memory Message Queue Service");
            }

            // Register High-Performance Chat Service
            builder.Services.AddScoped<IChatService, HighPerformanceChatService>();

            // Register Message Processing Background Service
            builder.Services.AddHostedService<MessageProcessingService>();

            // === EXISTING SERVICES REGISTRATION ===

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
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IFeedbackRepo, DataAccessLayer.Repositories.FeedbackRepo>();

            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IChatbotRepo, DataAccessLayer.Repositories.ChatbotRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IChatRepo, DataAccessLayer.Repositories.ChatRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.ISafeDeleteRepo, DataAccessLayer.Repositories.SafeDeleteRepo>();
            // Register Services with SOLID principles - Now using BusinessLogicLayer
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

            // Register Achievement Unlock Service for automatic achievement unlocking
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAchievementUnlockService, BusinessLogicLayer.Services.Implementations.AchievementUnlockServiceImpl>();

            // Register Achievement Notification Service for achievement notifications
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAchievementNotificationService, BusinessLogicLayer.Services.Implementations.AchievementNotificationServiceImpl>();

            // Register Certificate Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.CertificateServiceImpl>();

            // Register Security Service for brute force protection and rate limiting
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ISecurityService, BusinessLogicLayer.Services.Implementations.SecurityServiceInMemory>();

            // Register Security Cleanup Background Service
            builder.Services.AddHostedService<BrainStormEra_MVC.Services.SecurityCleanupService>();

            // Register Feedback Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IFeedbackService, BusinessLogicLayer.Services.Implementations.FeedbackServiceImpl>();

            // Register Notification Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.NotificationServiceImpl>();

            // Register Auth Service Implementation for business logic layer
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.AuthServiceImpl>();

            // Register Home Services for data access and business logic
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IHomeService, BusinessLogicLayer.Services.HomeService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.HomeServiceImpl>();

            // Register Payment Service
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IPaymentService, BusinessLogicLayer.Services.Implementations.PaymentService>();

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
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IMediaPathService, BusinessLogicLayer.Services.MediaPathService>();
            builder.Services.AddSingleton<BusinessLogicLayer.Services.Interfaces.ICacheService, BusinessLogicLayer.Services.CacheService>();

            // Add Email Service
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IEmailService, BusinessLogicLayer.Services.EmailService>();

            // Add Safe Delete Service for secure delete operations
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ISafeDeleteService, BusinessLogicLayer.Services.SafeDeleteService>();

            // Add Chatbot Service
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IChatbotService, BusinessLogicLayer.Services.ChatbotService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.ChatbotServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IChatService, BusinessLogicLayer.Services.Implementations.ChatService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IChatBusinessService, BusinessLogicLayer.Services.Implementations.ChatBusinessService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IPageContextService, BusinessLogicLayer.Services.PageContextService>();
            builder.Services.AddHttpClient<BusinessLogicLayer.Services.ChatbotService>();



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

                // The original code had this block commented out, but it's now part of the enhanced DB config.
                // Keeping it commented out as per instructions to only apply specified changes.
                // builder.Services.AddDbContext<BrainStormEraContext>(options =>
                // {
                //     options.UseSqlServer(connectionString, sqlOptions =>
                //     {
                //         sqlOptions.CommandTimeout(60); // Increased timeout for complex queries
                //         sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                //     });
                //     // Remove sensitive data logging in production for better performance
                //     if (builder.Environment.IsDevelopment())
                //     {
                //         options.EnableSensitiveDataLogging();
                //     }
                //     options.EnableServiceProviderCaching();
                // });
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
                    var context = scope.ServiceProvider.GetRequiredService<BrainStormEraContext>(); // Changed to BrainStormEraContext
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
            catch (Exception)
            {
                // Database connection test failed, but continue running

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

            // Configure Rotativa for PDF generation
            RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

            app.UseRouting();

            // Add Security middleware for brute force protection
            app.UseSecurityMiddleware();

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
            app.MapHub<BusinessLogicLayer.Hubs.ChatHub>("/chatHub");

            app.Run();
        }
    }
}
