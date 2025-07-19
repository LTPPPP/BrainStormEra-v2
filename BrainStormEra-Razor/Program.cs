using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using BusinessLogicLayer;
using BusinessLogicLayer.Constants;
using BrainStormEra_Razor.Middlewares;

namespace BrainStormEra_Razor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            // Add SignalR
            builder.Services.AddSignalR();

            // Add HttpContextAccessor
            builder.Services.AddHttpContextAccessor();

            // Add Memory Cache
            builder.Services.AddMemoryCache();

            // Add SignalR
            builder.Services.AddSignalR();

            // Add Entity Framework
            builder.Services.AddDbContext<BrainStormEraContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                });

            // Register ALL Repositories from DataAccessLayer
            builder.Services.AddScoped(typeof(DataAccessLayer.Repositories.Interfaces.IBaseRepo<>), typeof(DataAccessLayer.Repositories.BaseRepo<>));
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IAuthRepo, DataAccessLayer.Repositories.AuthRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IUserRepo, DataAccessLayer.Repositories.UserRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.ICourseRepo, DataAccessLayer.Repositories.CourseRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IAchievementRepo, DataAccessLayer.Repositories.AchievementRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IAdminRepo, DataAccessLayer.Repositories.AdminRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.ICertificateRepo, DataAccessLayer.Repositories.CertificateRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IChapterRepo, DataAccessLayer.Repositories.ChapterRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IChatbotRepo, DataAccessLayer.Repositories.ChatbotRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.ILessonRepo, DataAccessLayer.Repositories.LessonRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.INotificationRepo, DataAccessLayer.Repositories.NotificationRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IQuestionRepo, DataAccessLayer.Repositories.QuestionRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IQuizRepo, DataAccessLayer.Repositories.QuizRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IFeedbackRepo, DataAccessLayer.Repositories.FeedbackRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.IChatRepo, DataAccessLayer.Repositories.ChatRepo>();
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.ISafeDeleteRepo, DataAccessLayer.Repositories.SafeDeleteRepo>();

            // Register Service Interfaces and Implementations
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IUserService, BusinessLogicLayer.Services.UserService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAvatarService, BusinessLogicLayer.Services.AvatarService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAchievementService, BusinessLogicLayer.Services.Implementations.AchievementServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAchievementUnlockService, BusinessLogicLayer.Services.Implementations.AchievementUnlockServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAchievementNotificationService, BusinessLogicLayer.Services.Implementations.AchievementNotificationServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAchievementIconService, BusinessLogicLayer.Services.AchievementIconService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ICourseImageService, BusinessLogicLayer.Services.CourseImageService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IMediaPathService, BusinessLogicLayer.Services.MediaPathService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAdminService, BusinessLogicLayer.Services.AdminService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IEmailService, BusinessLogicLayer.Services.EmailService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.INotificationService, BusinessLogicLayer.Services.NotificationService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ICourseService, BusinessLogicLayer.Services.Implementations.CourseServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ILessonService, BusinessLogicLayer.Services.Implementations.LessonServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IQuizService, BusinessLogicLayer.Services.Implementations.QuizServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IChapterService, BusinessLogicLayer.Services.Implementations.ChapterServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IQuestionService, BusinessLogicLayer.Services.QuestionService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IFeedbackService, BusinessLogicLayer.Services.Implementations.FeedbackServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IPaymentService, BusinessLogicLayer.Services.Implementations.PaymentService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IHomeService, BusinessLogicLayer.Services.HomeService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IEnrollmentService, BusinessLogicLayer.Services.EnrollmentService>();
            builder.Services.AddSingleton<BusinessLogicLayer.Services.Interfaces.ICacheService, BusinessLogicLayer.Services.CacheService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IUserContextService, BusinessLogicLayer.Services.UserContextService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IResponseService, BusinessLogicLayer.Services.ResponseService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ICertificateService, BusinessLogicLayer.Services.Implementations.CertificateServiceImpl>();

            // Register Points Service
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IPointsService, BusinessLogicLayer.Services.Implementations.PointsService>();

            // Register other Services from BusinessLogicLayer
            builder.Services.AddScoped<BusinessLogicLayer.Services.UserService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.AvatarService>();

            builder.Services.AddScoped<BusinessLogicLayer.Services.AchievementIconService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.MediaPathService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.AdminService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.EmailService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.NotificationService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.CourseServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.EnrollmentService>();

            // Register ServiceImpls from BusinessLogicLayer  
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.AuthServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.NotificationServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.AdminServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.LessonServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.ChapterServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.QuizServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.QuestionServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.FeedbackServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.HomeServiceImpl>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.PaymentService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.RecommendationHelper>();

            // Register Security Service for brute force protection and rate limiting
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.ISecurityService, BusinessLogicLayer.Services.Implementations.SecurityServiceInMemory>();

            // Register Security Cleanup Background Service
            builder.Services.AddHostedService<BrainStormEra_Razor.Services.SecurityCleanupService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Configure additional static files from SharedMedia folder
            var sharedMediaPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SharedMedia");
            var absoluteSharedMediaPath = Path.GetFullPath(sharedMediaPath);

            if (Directory.Exists(absoluteSharedMediaPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(absoluteSharedMediaPath),
                    RequestPath = "/SharedMedia",
                    ServeUnknownFileTypes = true
                });

            }
            else
            {

            }

            app.UseRouting();

            // Add Security middleware for brute force protection
            app.UseSecurityMiddleware();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            // Map SignalR hubs
            app.MapHub<BusinessLogicLayer.Hubs.NotificationHub>("/notificationHub");

            // Debug endpoint for SharedMedia
            app.MapGet("/debug/sharedmedia", () =>
            {
                var sharedMediaPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SharedMedia");
                var absoluteSharedMediaPath = Path.GetFullPath(sharedMediaPath);

                return new
                {
                    SharedMediaPath = sharedMediaPath,
                    AbsoluteSharedMediaPath = absoluteSharedMediaPath,
                    Exists = Directory.Exists(absoluteSharedMediaPath),
                    Files = Directory.Exists(absoluteSharedMediaPath)
                        ? Directory.GetFiles(absoluteSharedMediaPath, "*", SearchOption.AllDirectories).Take(10).ToArray()
                        : Array.Empty<string>()
                };
            });

            await app.RunAsync();
        }
    }
}
