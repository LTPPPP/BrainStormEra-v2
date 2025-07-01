using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using BusinessLogicLayer;
using BusinessLogicLayer.Constants;

namespace BrainStormEra_Razor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            // Add HttpContextAccessor
            builder.Services.AddHttpContextAccessor();

            // Add Memory Cache
            builder.Services.AddMemoryCache();

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
            builder.Services.AddScoped<DataAccessLayer.Repositories.Interfaces.ISafeDeleteRepo, DataAccessLayer.Repositories.SafeDeleteRepo>();            // Register Service Interfaces and Implementations
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IUserService, BusinessLogicLayer.Services.UserService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAvatarService, BusinessLogicLayer.Services.AvatarService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAchievementIconService, BusinessLogicLayer.Services.AchievementIconService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IMediaPathService, BusinessLogicLayer.Services.MediaPathService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IAdminService, BusinessLogicLayer.Services.AdminService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.Interfaces.IEmailService, BusinessLogicLayer.Services.EmailService>();

            // Register other Services from BusinessLogicLayer
            builder.Services.AddScoped<BusinessLogicLayer.Services.UserService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.AvatarService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.AchievementIconService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.MediaPathService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.AdminService>();
            builder.Services.AddScoped<BusinessLogicLayer.Services.EmailService>();

            // Register ServiceImpls from BusinessLogicLayer  
            builder.Services.AddScoped<BusinessLogicLayer.Services.Implementations.AuthServiceImpl>();



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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

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
