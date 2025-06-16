using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Repositories.Interfaces;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepo _adminRepo;
        private readonly IUserRepo _userRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            IAdminRepo adminRepo,
            IUserRepo userRepo,
            ICourseRepo courseRepo,
            ILogger<AdminService> logger)
        {
            _adminRepo = adminRepo;
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<AdminDashboardViewModel> GetAdminDashboardAsync(string userId)
        {
            try
            {
                var statistics = await _adminRepo.GetAdminStatisticsAsync();
                var recentUsers = await _adminRepo.GetRecentUsersAsync(5);
                var recentCourses = await _adminRepo.GetRecentCoursesAsync(5);

                // Get admin user details
                var adminUser = await _userRepo.GetByIdAsync(userId);

                // Get real chart data
                var userGrowthData = await _adminRepo.GetUserGrowthDataAsync();
                var revenueData = await _adminRepo.GetRevenueDataAsync();
                var enrollmentData = await _adminRepo.GetWeeklyEnrollmentDataAsync();

                // Get chatbot analytics data
                var chatbotStats = await _adminRepo.GetChatbotStatisticsAsync();
                var chatbotDailyUsage = await _adminRepo.GetDailyChatbotUsageAsync();
                var chatbotFeedback = await _adminRepo.GetChatbotFeedbackStatsAsync();
                var chatbotHourlyUsage = await _adminRepo.GetChatbotHourlyUsageAsync();

                return new AdminDashboardViewModel
                {
                    AdminName = adminUser?.FullName ?? "Admin",
                    AdminImage = adminUser?.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                    TotalUsers = statistics.ContainsKey("TotalUsers") ? (int)statistics["TotalUsers"] : 0,
                    TotalCourses = statistics.ContainsKey("TotalCourses") ? (int)statistics["TotalCourses"] : 0,
                    TotalEnrollments = statistics.ContainsKey("TotalEnrollments") ? (int)statistics["TotalEnrollments"] : 0,
                    TotalRevenue = statistics.ContainsKey("TotalRevenue") ? (decimal)statistics["TotalRevenue"] : 0,

                    // Extended statistics for charts
                    TotalLearners = statistics.ContainsKey("TotalLearners") ? (int)statistics["TotalLearners"] : 0,
                    TotalInstructors = statistics.ContainsKey("TotalInstructors") ? (int)statistics["TotalInstructors"] : 0,
                    TotalAdmins = statistics.ContainsKey("TotalAdmins") ? (int)statistics["TotalAdmins"] : 0,
                    ApprovedCourses = statistics.ContainsKey("ApprovedCourses") ? (int)statistics["ApprovedCourses"] : 0,
                    PendingCourses = statistics.ContainsKey("PendingCourses") ? (int)statistics["PendingCourses"] : 0,
                    RejectedCourses = statistics.ContainsKey("RejectedCourses") ? (int)statistics["RejectedCourses"] : 0,

                    // Real chart data
                    UserGrowthData = userGrowthData,
                    RevenueData = revenueData,
                    EnrollmentData = enrollmentData,

                    // Certificate analytics data
                    TotalCertificates = statistics.ContainsKey("TotalCertificates") ? (int)statistics["TotalCertificates"] : 0,
                    ValidCertificates = statistics.ContainsKey("ValidCertificates") ? (int)statistics["ValidCertificates"] : 0,
                    ExpiredCertificates = statistics.ContainsKey("ExpiredCertificates") ? (int)statistics["ExpiredCertificates"] : 0,
                    CertificateData = await _adminRepo.GetMonthlyCertificateDataAsync(),
                    CourseCompletionRates = await _adminRepo.GetCourseCompletionRatesAsync(),

                    // Point analytics data
                    TotalPointsInSystem = statistics.ContainsKey("TotalPointsInSystem") ? (decimal)statistics["TotalPointsInSystem"] : 0,
                    AverageUserPoints = statistics.ContainsKey("AverageUserPoints") ? (decimal)statistics["AverageUserPoints"] : 0,
                    PointDistributionData = await _adminRepo.GetPointDistributionDataAsync(),
                    MonthlyPointsData = await _adminRepo.GetMonthlyPointsDataAsync(),

                    // Chatbot analytics data
                    ChatbotStatistics = chatbotStats,
                    ChatbotDailyUsage = chatbotDailyUsage,
                    ChatbotFeedback = chatbotFeedback,
                    ChatbotHourlyUsage = chatbotHourlyUsage,
                    RecentUsers = recentUsers.Select(u => new UserViewModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName ?? "",
                        UserEmail = u.UserEmail,
                        UserRole = u.UserRole,
                        UserImage = u.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                        AccountCreatedAt = u.AccountCreatedAt,
                        IsBanned = u.IsBanned ?? false
                    }).ToList(),
                    RecentCourses = recentCourses.Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName ?? "",
                        CoursePicture = c.CourseImage ?? "/SharedMedia/defaults/default-course.svg",
                        Price = c.Price,
                        CreatedBy = c.Author?.FullName ?? "",
                        Description = c.CourseDescription ?? "",
                        StarRating = 0, // Will be calculated from feedback
                        EnrollmentCount = c.Enrollments?.Count ?? 0
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin dashboard for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetAdminStatisticsAsync()
        {
            try
            {
                return await _adminRepo.GetAdminStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin statistics");
                throw;
            }
        }

        public async Task<List<UserViewModel>> GetRecentUsersAsync(int count)
        {
            try
            {
                var users = await _adminRepo.GetRecentUsersAsync(count);
                return users.Select(u => new UserViewModel
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName ?? "",
                    UserEmail = u.UserEmail,
                    UserRole = u.UserRole,
                    UserImage = u.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                    AccountCreatedAt = u.AccountCreatedAt,
                    IsBanned = u.IsBanned ?? false
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent users with count {Count}", count);
                throw;
            }
        }

        public async Task<List<CourseViewModel>> GetRecentCoursesAsync(int count)
        {
            try
            {
                var courses = await _adminRepo.GetRecentCoursesAsync(count);
                return courses.Select(c => new CourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName ?? "",
                    CoursePicture = c.CourseImage ?? "/SharedMedia/defaults/default-course.svg",
                    Price = c.Price,
                    CreatedBy = c.Author?.FullName ?? "",
                    Description = c.CourseDescription ?? "",
                    StarRating = 0, // Will be calculated from feedback
                    EnrollmentCount = c.Enrollments?.Count ?? 0
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent courses with count {Count}", count);
                throw;
            }
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            try
            {
                return await _adminRepo.GetTotalRevenueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total revenue");
                throw;
            }
        }

        public async Task<AdminUsersViewModel> GetAllUsersAsync(string? search = null, string? roleFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                List<DataAccessLayer.Models.Account> users;

                if (!string.IsNullOrEmpty(search))
                {
                    users = await _adminRepo.SearchUsersAsync(search, page, pageSize);
                }
                else if (!string.IsNullOrEmpty(roleFilter))
                {
                    users = await _adminRepo.GetUsersByRoleAsync(roleFilter, page, pageSize);
                }
                else
                {
                    users = await _adminRepo.GetAllUsersAsync(page, pageSize);
                }

                var totalUsers = await _adminRepo.GetTotalUsersCountAsync();

                return new AdminUsersViewModel
                {
                    Users = users.Select(u => new AdminUserViewModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName ?? "",
                        UserEmail = u.UserEmail,
                        UserRole = u.UserRole,
                        UserImage = u.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                        AccountCreatedAt = u.AccountCreatedAt,
                        LastLoginDate = u.LastLogin,
                        IsBanned = u.IsBanned ?? false,
                        IsActive = !(u.IsBanned ?? false)
                    }).ToList(),
                    TotalUsers = totalUsers,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users with search: {Search}, roleFilter: {RoleFilter}, page: {Page}, pageSize: {PageSize}",
                    search, roleFilter, page, pageSize);
                throw;
            }
        }

        public async Task<AdminCoursesViewModel> GetAllCoursesAsync(string? search = null, string? categoryFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                List<DataAccessLayer.Models.Course> courses;

                if (!string.IsNullOrEmpty(search))
                {
                    courses = await _adminRepo.SearchCoursesAsync(search, page, pageSize);
                }
                else
                {
                    courses = await _adminRepo.GetAllCoursesAsync(page, pageSize);
                }

                var totalCourses = await _adminRepo.GetTotalCoursesCountAsync();

                return new AdminCoursesViewModel
                {
                    Courses = courses.Select(c => new AdminCourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName ?? "",
                        CourseDescription = c.CourseDescription ?? "",
                        CoursePicture = c.CourseImage ?? "/SharedMedia/defaults/default-course.svg",
                        Price = c.Price,
                        CreatedAt = c.CourseCreatedAt,
                        UpdatedAt = c.CourseUpdatedAt,
                        IsApproved = c.ApprovalStatus == "Approved",
                        IsFeatured = c.IsFeatured ?? false,
                        IsActive = c.CourseStatus == 1, // Assuming 1 means active
                        InstructorId = c.AuthorId,
                        InstructorName = c.Author?.FullName ?? "",
                        EnrollmentCount = c.Enrollments?.Count ?? 0,
                        AverageRating = 0, // Will be calculated from feedback
                        Revenue = 0 // Will be calculated from payments
                    }).ToList(),
                    TotalCourses = totalCourses,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCourses / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all courses with search: {Search}, categoryFilter: {CategoryFilter}, page: {Page}, pageSize: {PageSize}",
                    search, categoryFilter, page, pageSize);
                throw;
            }
        }

        public async Task<bool> UpdateUserStatusAsync(string userId, bool isBanned)
        {
            try
            {
                return await _adminRepo.BanUserAsync(userId, isBanned);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for userId: {UserId}, isBanned: {IsBanned}", userId, isBanned);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                return await _adminRepo.DeleteAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateCourseStatusAsync(string courseId, bool isApproved, string? adminId = null)
        {
            try
            {
                if (isApproved)
                {
                    // Use adminId if provided, otherwise use "system"
                    return await _adminRepo.ApproveCourseAsync(courseId, adminId ?? "system");
                }
                else
                {
                    return await _adminRepo.RejectCourseAsync(courseId, adminId ?? "system", "Course rejected by admin");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course status for courseId: {CourseId}, isApproved: {IsApproved}", courseId, isApproved);
                throw;
            }
        }

        public async Task<bool> BanCourseAsync(string courseId, string? adminId = null)
        {
            try
            {
                return await _adminRepo.BanCourseAsync(courseId, adminId ?? "system", "Course banned by admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning course for courseId: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<bool> DeleteCourseAsync(string courseId)
        {
            try
            {
                return await _courseRepo.DeleteAsync(courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course with courseId: {CourseId}", courseId);
                throw;
            }
        }
    }
}
