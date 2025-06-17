using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Constants;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IAdminService _adminService;

        public string? AdminName { get; set; }
        public string? UserId { get; set; }
        public DateTime LoginTime { get; set; }
        public AdminDashboardViewModel DashboardData { get; set; } = new AdminDashboardViewModel
        {
            AdminName = "Admin",
            AdminImage = "/SharedMedia/defaults/default-avatar.svg"
        };

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? FilterYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterMonth { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterCategory { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IAdminService adminService)
        {
            _logger = logger;
            _adminService = adminService;
        }
        public async Task OnGetAsync()
        {
            try
            {
                AdminName = HttpContext.User?.Identity?.Name ?? "Admin";
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";

                if (DateTime.TryParse(HttpContext.User?.FindFirst("LoginTime")?.Value, out var loginTime))
                {
                    LoginTime = loginTime;
                }
                else
                {
                    LoginTime = DateTime.UtcNow;
                }

                // Initialize DashboardData with default values
                DashboardData.AdminName = AdminName ?? "Admin";
                DashboardData.AdminImage = string.IsNullOrEmpty(User.FindFirst("Avatar")?.Value)
                    ? MediaConstants.Defaults.DefaultAvatarPath
                    : User.FindFirst("Avatar")?.Value ?? MediaConstants.Defaults.DefaultAvatarPath;

                // Load dashboard data with error handling
                if (!string.IsNullOrEmpty(UserId))
                {
                    DashboardData = await _adminService.GetAdminDashboardAsync(UserId);
                    // Ensure the admin name and image are still set after loading from service
                    DashboardData.AdminName = AdminName ?? "Admin";
                    DashboardData.AdminImage = string.IsNullOrEmpty(User.FindFirst("Avatar")?.Value)
                        ? MediaConstants.Defaults.DefaultAvatarPath
                        : User.FindFirst("Avatar")?.Value ?? MediaConstants.Defaults.DefaultAvatarPath;
                }
                else
                {
                    _logger.LogWarning("User ID not found in claims");
                }

                _logger.LogInformation("Admin dashboard accessed by: {AdminName} at {AccessTime}", AdminName, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data for user: {UserId}", UserId);
                // Initialize with default data on error
                DashboardData.AdminName = AdminName ?? "Admin";
                DashboardData.AdminImage = MediaConstants.Defaults.DefaultAvatarPath;
                // Consider adding error message to view
            }
        }
        public async Task<IActionResult> OnGetChartDataAsync(string chartType, string? year = null, string? month = null, string? category = null)
        {
            try
            {
                // Validate parameters
                if (string.IsNullOrWhiteSpace(chartType))
                {
                    return BadRequest("Chart type is required");
                }

                if (string.IsNullOrEmpty(UserId))
                {
                    return BadRequest("User ID not found");
                }

                // Add rate limiting/caching if needed
                var dashboardData = await _adminService.GetAdminDashboardAsync(UserId);

                if (dashboardData == null)
                {
                    return StatusCode(500, "Failed to retrieve dashboard data");
                }
                return chartType.ToLower() switch
                {
                    "users" => new JsonResult(new
                    {
                        totalUsers = dashboardData.TotalUsers,
                        totalLearners = dashboardData.TotalLearners,
                        totalInstructors = dashboardData.TotalInstructors,
                        totalAdmins = dashboardData.TotalAdmins,
                        userGrowthData = dashboardData.UserGrowthData?.Select(u => new
                        {
                            month = u.Month,
                            newUsers = u.NewUsers,
                            date = u.Date.ToString("yyyy-MM-dd")
                        }) ?? Enumerable.Empty<object>()
                    }),
                    "courses" => new JsonResult(new
                    {
                        totalCourses = dashboardData.TotalCourses,
                        approvedCourses = dashboardData.ApprovedCourses,
                        pendingCourses = dashboardData.PendingCourses,
                        rejectedCourses = dashboardData.RejectedCourses,
                        enrollmentData = dashboardData.EnrollmentData?.Select(e => new
                        {
                            week = e.Week,
                            newEnrollments = e.NewEnrollments,
                            completedCourses = e.CompletedCourses,
                            date = e.Date.ToString("yyyy-MM-dd")
                        }) ?? Enumerable.Empty<object>()
                    }),
                    "certificates" => new JsonResult(new
                    {
                        totalCertificates = dashboardData.TotalCertificates,
                        validCertificates = dashboardData.ValidCertificates,
                        expiredCertificates = dashboardData.ExpiredCertificates,
                        certificateData = dashboardData.CertificateData?.Select(c => new
                        {
                            month = c.Month,
                            certificatesIssued = c.CertificatesIssued,
                            date = c.Date.ToString("yyyy-MM-dd")
                        }) ?? Enumerable.Empty<object>(),
                        completionRates = dashboardData.CourseCompletionRates?.Select(r => new
                        {
                            courseId = r.CourseId,
                            courseName = r.CourseName,
                            totalEnrollments = r.TotalEnrollments,
                            completedCount = r.CompletedCount,
                            completionRate = r.CompletionRate
                        }) ?? Enumerable.Empty<object>()
                    }),
                    "points" => new JsonResult(new
                    {
                        totalPoints = dashboardData.TotalPointsInSystem,
                        averagePoints = dashboardData.AverageUserPoints,
                        distributionData = dashboardData.PointDistributionData?.Select(p => new
                        {
                            pointRange = p.PointRange,
                            userCount = p.UserCount
                        }) ?? Enumerable.Empty<object>(),
                        monthlyData = dashboardData.MonthlyPointsData?.Select(m => new
                        {
                            month = m.Month,
                            totalPointsEarned = m.TotalPointsEarned,
                            date = m.Date.ToString("yyyy-MM-dd")
                        }) ?? Enumerable.Empty<object>()
                    }),
                    "chatbot" => new JsonResult(new
                    {
                        statistics = dashboardData.ChatbotStatistics ?? new Dictionary<string, object>(),
                        dailyUsage = dashboardData.ChatbotDailyUsage?.Select(d => new
                        {
                            date = d.Date.ToString("yyyy-MM-dd"),
                            conversationCount = d.ConversationCount
                        }) ?? Enumerable.Empty<object>(),
                        feedback = dashboardData.ChatbotFeedback ?? new List<FeedbackRatingStats>(),
                        hourlyUsage = dashboardData.ChatbotHourlyUsage ?? new List<HourlyUsageStats>()
                    }),
                    _ => BadRequest($"Invalid chart type: {chartType}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart data for type: {ChartType}, Year: {Year}, Month: {Month}, Category: {Category}",
                    chartType, year, month, category);
                return StatusCode(500, "Internal server error occurred while retrieving chart data");
            }
        }
    }
}
