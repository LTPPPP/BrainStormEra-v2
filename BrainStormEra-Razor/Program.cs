using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Repositories;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services;
using Microsoft.Extensions.FileProviders;

namespace BrainStormEra_Razor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add DbContext
            builder.Services.AddDbContext<BrainStormEraContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add repositories
            builder.Services.AddScoped<IUserRepo, UserRepo>();
            builder.Services.AddScoped<IAdminRepo, AdminRepo>();
            builder.Services.AddScoped<IAuthRepo, AuthRepo>();
            builder.Services.AddScoped<ICourseRepo, CourseRepo>();

            // Add services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAdminService, AdminService>();

            // Add authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Admin/Login";
                    options.LogoutPath = "/Admin/Logout";
                    options.AccessDeniedPath = "/Admin/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireClaim("UserRole", "admin"));
            });

            // Add memory cache
            builder.Services.AddMemoryCache();

            // Add services to the container.
            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeAreaFolder("Admin", "/", "AdminOnly");
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

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

            app.UseAuthentication();
            app.UseAuthorization();

            // Redirect root to admin login
            app.MapGet("/", context =>
            {
                context.Response.Redirect("/Admin/Login");
                return Task.CompletedTask;
            });

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

            app.MapRazorPages();

            app.Run();
        }
    }
}
