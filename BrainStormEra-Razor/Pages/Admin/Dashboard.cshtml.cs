using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class DashboardModel : PageModel
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(IAdminService adminService, ILogger<DashboardModel> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        public AdminDashboardViewModel Dashboard { get; set; } = new()
        {
            AdminName = "Admin",
            AdminImage = "/img/defaults/default-avatar.svg"
        };

        public async Task OnGetAsync()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return;
                }

                Dashboard = await _adminService.GetAdminDashboardAsync(userId);
                _logger.LogInformation("Dashboard loaded successfully for admin: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for admin");
                Dashboard = new AdminDashboardViewModel
                {
                    AdminName = User.FindFirst("FullName")?.Value ?? "Admin",
                    AdminImage = "/img/defaults/default-avatar.svg",
                    TotalUsers = 0,
                    TotalCourses = 0,
                    TotalEnrollments = 0,
                    TotalRevenue = 0,
                    RecentUsers = new List<UserViewModel>(),
                    RecentCourses = new List<CourseViewModel>()
                };
            }
        }
    }
}