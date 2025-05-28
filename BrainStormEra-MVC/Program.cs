using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Hubs;

namespace BrainStormEra_MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add SignalR for real-time chat
            builder.Services.AddSignalR();

            // Add session support
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Thêm xử lý lỗi kết nối DB
            try
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("Connection string is missing or empty!");
                }

                builder.Services.AddDbContext<BrainStormEraContext>(options =>
                    options.UseSqlServer(connectionString));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database configuration error: {ex.Message}");
                // Vẫn tiếp tục chạy ứng dụng nhưng ghi lại lỗi
            }

            var app = builder.Build();

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
            app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}"); app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization(); app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Map SignalR hub
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}
