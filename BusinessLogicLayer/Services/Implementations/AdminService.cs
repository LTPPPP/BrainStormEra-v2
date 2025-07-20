using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Repositories.Interfaces;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BusinessLogicLayer.Services.Implementations
{
    #region Result Classes

    /// <summary>
    /// Result class for admin dashboard operations
    /// </summary>
    public class AdminDashboardResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public AdminDashboardViewModel? Data { get; set; }
    }

    /// <summary>
    /// Result class for admin statistics operations
    /// </summary>
    public class AdminStatisticsResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
    }

    /// <summary>
    /// Result class for recent users operations
    /// </summary>
    public class RecentUsersResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<UserViewModel>? Data { get; set; }
    }

    /// <summary>
    /// Result class for recent courses operations
    /// </summary>
    public class RecentCoursesResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CourseViewModel>? Data { get; set; }
    }

    /// <summary>
    /// Result class for total revenue operations
    /// </summary>
    public class TotalRevenueResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal Data { get; set; }
    }

    /// <summary>
    /// Result class for admin user management operations
    /// </summary>
    public class AdminUsersResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public AdminUsersViewModel? Data { get; set; }
    }

    /// <summary>
    /// Result class for admin course management operations
    /// </summary>
    public class AdminCoursesResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public AdminCoursesViewModel? Data { get; set; }
    }

    /// <summary>
    /// Result class for admin operations
    /// </summary>
    public class AdminOperationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result class for user ranking operations
    /// </summary>
    public class UserRankingResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserRankingViewModel? Data { get; set; }
    }

    /// <summary>
    /// Result class for chatbot history operations
    /// </summary>
    public class ChatbotHistoryResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public ChatbotHistoryViewModel? Data { get; set; }
    }

    #endregion

    /// <summary>
    /// Interface for AdminServiceImpl business logic operations
    /// </summary>
    public interface IAdminServiceImpl
    {
        Task<AdminDashboardResult> GetAdminDashboardAsync(ClaimsPrincipal user);
        Task<AdminStatisticsResult> GetAdminStatisticsAsync(ClaimsPrincipal user);
        Task<RecentUsersResult> GetRecentUsersAsync(ClaimsPrincipal user, int count = 5);
        Task<RecentCoursesResult> GetRecentCoursesAsync(ClaimsPrincipal user, int count = 5);
        Task<TotalRevenueResult> GetTotalRevenueAsync(ClaimsPrincipal user);
        Task<AdminUsersResult> GetAllUsersAsync(ClaimsPrincipal user, string? search = null, string? roleFilter = null, int page = 1, int pageSize = 10);
        Task<AdminCoursesResult> GetAllCoursesAsync(ClaimsPrincipal user, string? search = null, string? categoryFilter = null, int page = 1, int pageSize = 10);
        Task<AdminOperationResult> UpdateUserStatusAsync(ClaimsPrincipal user, string userId, bool isBanned);
        Task<AdminOperationResult> DeleteUserAsync(ClaimsPrincipal user, string userId);
        Task<AdminOperationResult> UpdateCourseStatusAsync(ClaimsPrincipal user, string courseId, bool isApproved);
        Task<AdminOperationResult> DeleteCourseAsync(ClaimsPrincipal user, string courseId);
        Task<UserRankingResult> GetUserRankingAsync(ClaimsPrincipal user, int page = 1, int pageSize = 20);
        Task<ChatbotHistoryResult> GetChatbotHistoryAsync(ClaimsPrincipal user, string? search = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 20);
    }

    /// <summary>
    /// Business logic implementation for Admin service operations.
    /// Handles authentication, authorization, validation, and error handling.
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly IAdminRepo _adminRepo;
        private readonly IUserRepo _userRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly IAchievementService _achievementService;
        private readonly ICertificateService _certificateService;
        private readonly IAchievementRepo _achievementRepo;
        private readonly IPointsService _pointsService;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            IAdminRepo adminRepo,
            IUserRepo userRepo,
            ICourseRepo courseRepo,
            IAchievementService achievementService,
            ICertificateService certificateService,
            IAchievementRepo achievementRepo,
            IPointsService pointsService,
            ILogger<AdminService> logger)
        {
            _adminRepo = adminRepo;
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _achievementService = achievementService;
            _certificateService = certificateService;
            _achievementRepo = achievementRepo;
            _pointsService = pointsService;
            _logger = logger;
        }

        #region IAdminService Implementation

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
                // Get all users first, then apply filters in memory for better flexibility
                var allUsers = await _adminRepo.GetAllUsersAsync();

                // Apply filters
                var filteredUsers = allUsers.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    filteredUsers = filteredUsers.Where(u =>
                        (u.FullName != null && u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (u.Username != null && u.Username.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (u.UserEmail != null && u.UserEmail.Contains(search, StringComparison.OrdinalIgnoreCase)));
                }

                // Apply role filter
                if (!string.IsNullOrEmpty(roleFilter))
                {
                    filteredUsers = filteredUsers.Where(u =>
                        u.UserRole != null && u.UserRole.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));
                }

                var users = filteredUsers.ToList();
                var totalUsers = users.Count;

                // Apply pagination
                var paginatedUsers = users
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Calculate statistics
                var totalAdmins = allUsers.Count(u => u.UserRole?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true);
                var totalInstructors = allUsers.Count(u => u.UserRole?.Equals("instructor", StringComparison.OrdinalIgnoreCase) == true);
                var totalLearners = allUsers.Count(u => u.UserRole?.Equals("learner", StringComparison.OrdinalIgnoreCase) == true);
                var bannedUsers = allUsers.Count(u => u.IsBanned == true);

                return new AdminUsersViewModel
                {
                    Users = paginatedUsers.Select(u => new AdminUserViewModel
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
                        IsActive = !(u.IsBanned ?? false),
                        PhoneNumber = u.PhoneNumber,
                        UserAddress = u.UserAddress,
                        DateOfBirth = u.DateOfBirth,
                        Gender = u.Gender,
                        PaymentPoint = u.PaymentPoint,
                        BankName = u.BankName,
                        BankAccountNumber = u.BankAccountNumber,
                        AccountHolderName = u.AccountHolderName
                    }).ToList(),
                    SearchQuery = search,
                    RoleFilter = roleFilter,
                    TotalUsers = totalUsers,
                    TotalAdmins = totalAdmins,
                    TotalInstructors = totalInstructors,
                    TotalLearners = totalLearners,
                    BannedUsers = bannedUsers,
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

        public async Task<AdminUserViewModel?> GetUserDetailAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return null;
                }

                var user = await _adminRepo.GetUserWithDetailsAsync(userId);
                if (user == null)
                {
                    return null;
                }

                // Get user achievements and certificates
                var userAchievements = await _achievementService.GetUserAchievementsAsync(userId);
                var certificates = await _certificateService.GetUserCertificatesAsync(userId);

                // Convert UserAchievement to AchievementSummaryViewModel
                var achievements = new List<AchievementSummaryViewModel>();
                foreach (var ua in userAchievements)
                {
                    var achievement = await _achievementRepo.GetByIdAsync(ua.AchievementId);
                    if (achievement != null)
                    {
                        achievements.Add(new AchievementSummaryViewModel
                        {
                            AchievementId = achievement.AchievementId,
                            AchievementName = achievement.AchievementName ?? "",
                            AchievementDescription = achievement.AchievementDescription ?? "",
                            AchievementIcon = achievement.AchievementIcon ?? "/SharedMedia/defaults/default-achievement.svg",
                            AchievementType = achievement.AchievementType ?? "",
                            PointsReward = achievement.PointsReward,
                            ReceivedDate = ua.ReceivedDate.ToDateTime(TimeOnly.MinValue)
                        });
                    }
                }

                var totalAchievements = achievements.Count;
                var totalCertificates = certificates.Count;
                var totalPointsEarned = achievements.Sum(a => a.PointsReward ?? 0);

                return new AdminUserViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName ?? "",
                    UserEmail = user.UserEmail,
                    UserRole = user.UserRole,
                    UserImage = user.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                    AccountCreatedAt = user.AccountCreatedAt,
                    LastLoginDate = user.LastLogin,
                    IsBanned = user.IsBanned ?? false,
                    IsActive = !(user.IsBanned ?? false),
                    PhoneNumber = user.PhoneNumber,
                    UserAddress = user.UserAddress,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    PaymentPoint = user.PaymentPoint,
                    BankName = user.BankName,
                    BankAccountNumber = user.BankAccountNumber,
                    AccountHolderName = user.AccountHolderName,
                    Achievements = achievements,
                    Certificates = certificates,
                    TotalAchievements = totalAchievements,
                    TotalCertificates = totalCertificates,
                    TotalPointsEarned = totalPointsEarned
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user detail for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<AdminCoursesViewModel> GetAllCoursesAsync(string? search = null, string? categoryFilter = null, string? statusFilter = null, string? priceFilter = null, string? difficultyFilter = null, string? instructorFilter = null, string? sortBy = null, int page = 1, int pageSize = 12)
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

                var courseViewModels = courses.Select(c =>
                {
                    var enrollmentCount = c.Enrollments?.Count ?? 0;
                    var feedbacks = c.Feedbacks?.Where(f => f.StarRating.HasValue).ToList() ?? new List<DataAccessLayer.Models.Feedback>();
                    var averageRating = feedbacks.Any() ? (decimal)feedbacks.Average(f => f.StarRating!.Value) : 0;
                    var reviewCount = feedbacks.Count;

                    // Calculate revenue (Price * EnrollmentCount for paid courses)
                    var revenue = c.Price > 0 ? c.Price * enrollmentCount : 0;

                    return new AdminCourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName ?? "",
                        CourseDescription = c.CourseDescription ?? "",
                        CoursePicture = c.CourseImage ?? "/SharedMedia/defaults/default-course.svg",
                        Price = c.Price,
                        DifficultyLevel = c.DifficultyLevel?.ToString(),
                        EstimatedDuration = c.EstimatedDuration,
                        CreatedAt = c.CourseCreatedAt,
                        UpdatedAt = c.CourseUpdatedAt,
                        ApprovalStatus = c.ApprovalStatus,
                        IsApproved = c.ApprovalStatus == "Approved",
                        IsFeatured = c.IsFeatured ?? false,
                        IsActive = c.CourseStatus == 1,
                        InstructorId = c.AuthorId,
                        InstructorName = c.Author?.FullName ?? "",
                        InstructorEmail = c.Author?.UserEmail ?? "",
                        EnrollmentCount = enrollmentCount,
                        AverageRating = averageRating,
                        ReviewCount = reviewCount,
                        Revenue = revenue,
                        Categories = c.CourseCategories?.Select(cc => cc.CourseCategoryName ?? "").ToList() ?? new List<string>()
                    };
                }).ToList();

                // Calculate summary statistics
                var approvedCourses = courseViewModels.Count(c => c.ApprovalStatus?.ToLower() == "approved");
                var pendingCourses = courseViewModels.Count(c => c.ApprovalStatus?.ToLower() == "pending");
                var rejectedCourses = courseViewModels.Count(c => c.ApprovalStatus?.ToLower() == "rejected");
                var freeCourses = courseViewModels.Count(c => c.Price == 0);
                var paidCourses = courseViewModels.Count(c => c.Price > 0);
                var totalRevenue = courseViewModels.Sum(c => c.Revenue);

                return new AdminCoursesViewModel
                {
                    Courses = courseViewModels,
                    TotalCourses = totalCourses,
                    ApprovedCourses = approvedCourses,
                    PendingCourses = pendingCourses,
                    RejectedCourses = rejectedCourses,
                    FreeCourses = freeCourses,
                    PaidCourses = paidCourses,
                    TotalRevenue = totalRevenue,
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

        public async Task<AdminCourseDetailsViewModel?> GetCourseDetailsAsync(string courseId)
        {
            try
            {
                if (string.IsNullOrEmpty(courseId))
                {
                    return null;
                }

                var course = await _courseRepo.GetCourseDetailAsync(courseId);
                if (course == null)
                {
                    return null;
                }

                // Get course statistics
                var enrollmentCount = course.Enrollments?.Count ?? 0;
                var feedbacks = course.Feedbacks?.Where(f => f.StarRating.HasValue).ToList() ?? new List<DataAccessLayer.Models.Feedback>();
                var averageRating = feedbacks.Any() ? (decimal)feedbacks.Average(f => f.StarRating!.Value) : 0;
                var reviewCount = feedbacks.Count;

                // Calculate revenue
                var revenue = course.Price > 0 ? course.Price * enrollmentCount : 0;

                // Get course chapters and lessons
                var chapters = course.Chapters?.OrderBy(c => c.ChapterOrder).ToList() ?? new List<DataAccessLayer.Models.Chapter>();
                var totalLessons = chapters.Sum(c => c.Lessons?.Count ?? 0);

                // Get course categories
                var categories = course.CourseCategories?.Select(cc => cc.CourseCategoryName ?? "").ToList() ?? new List<string>();

                return new AdminCourseDetailsViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName ?? "",
                    CourseDescription = course.CourseDescription ?? "",
                    CoursePicture = course.CourseImage ?? "/SharedMedia/defaults/default-course.svg",
                    Price = course.Price,
                    DifficultyLevel = course.DifficultyLevel?.ToString(),
                    EstimatedDuration = course.EstimatedDuration,
                    CreatedAt = course.CourseCreatedAt,
                    UpdatedAt = course.CourseUpdatedAt,
                    ApprovalStatus = course.ApprovalStatus,
                    IsApproved = course.ApprovalStatus == "Approved",
                    IsFeatured = course.IsFeatured ?? false,
                    IsActive = course.CourseStatus == 1,
                    InstructorId = course.AuthorId,
                    InstructorName = course.Author?.FullName ?? "",
                    InstructorEmail = course.Author?.UserEmail ?? "",
                    EnrollmentCount = enrollmentCount,
                    AverageRating = averageRating,
                    ReviewCount = reviewCount,
                    Revenue = revenue,
                    Categories = categories,
                    Chapters = chapters.Select(c => new CourseChapterSummary
                    {
                        ChapterId = c.ChapterId,
                        ChapterName = c.ChapterName ?? "",
                        ChapterOrder = c.ChapterOrder ?? 0,
                        LessonCount = c.Lessons?.Count ?? 0,
                        IsLocked = false // Default value, can be enhanced later
                    }).ToList(),
                    TotalLessons = totalLessons
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course details for courseId: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<bool> UpdateUserStatusAsync(string userId, bool isBanned)
        {
            try
            {
                return await _adminRepo.BanUserAsync(userId, isBanned, isBanned ? "User banned by admin" : "User unbanned by admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for userId: {UserId}, isBanned: {IsBanned}", userId, isBanned);
                throw;
            }
        }

        public async Task<bool> UpdateUserPointsAsync(string userId, decimal pointsChange)
        {
            try
            {
                return await _adminRepo.UpdateUserPointsAsync(userId, pointsChange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user points for userId: {UserId}, pointsChange: {PointsChange}", userId, pointsChange);
                throw;
            }
        }

        public async Task<bool> ChangeUserRoleAsync(string userId, string newRole)
        {
            try
            {
                // Validate role
                if (string.IsNullOrEmpty(newRole))
                {
                    _logger.LogWarning("Attempted to change user role to empty string for userId: {UserId}", userId);
                    return false;
                }

                // Check if user exists
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to change role for non-existent user: {UserId}", userId);
                    return false;
                }

                // Validate new role
                var validRoles = new[] { "admin", "instructor", "learner" };
                if (!validRoles.Contains(newRole.ToLower()))
                {
                    _logger.LogWarning("Attempted to change user role to invalid role '{NewRole}' for userId: {UserId}", newRole, userId);
                    return false;
                }

                // Prevent changing own role
                // Note: This check should be done at the controller level with the current user's ID
                // if (userId == currentUserId)
                // {
                //     _logger.LogWarning("User attempted to change their own role: {UserId}", userId);
                //     return false;
                // }

                return await _adminRepo.ChangeUserRoleAsync(userId, newRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user role for userId: {UserId}, newRole: {NewRole}", userId, newRole);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                // Check if user exists
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent user: {UserId}", userId);
                    return false;
                }

                // Prevent deleting admin users
                if (user.UserRole?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogWarning("Attempted to delete admin user: {UserId}", userId);
                    return false;
                }

                // Check if user has created courses
                var userCourses = await _courseRepo.GetInstructorCoursesAsync(userId, null, null, 1, int.MaxValue);
                if (userCourses.Any())
                {
                    _logger.LogWarning("Attempted to delete user with existing courses: {UserId}, CourseCount: {CourseCount}", userId, userCourses.Count);
                    return false;
                }

                // Check if user has enrollments
                var userEnrollments = await _courseRepo.GetUserEnrollmentsAsync(userId);
                if (userEnrollments.Any())
                {
                    _logger.LogWarning("Attempted to delete user with existing enrollments: {UserId}, EnrollmentCount: {EnrollmentCount}", userId, userEnrollments.Count);
                    return false;
                }

                // Perform soft delete by banning the user
                return await _adminRepo.BanUserAsync(userId, true, "User deleted by admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user for userId: {UserId}", userId);
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

        public async Task<bool> RejectCourseAsync(string courseId, string reason, string? adminId = null)
        {
            try
            {
                return await _adminRepo.RejectCourseAsync(courseId, adminId ?? "system", reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting course for courseId: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<bool> BanCourseAsync(string courseId, string reason, string? adminId = null)
        {
            try
            {
                return await _adminRepo.BanCourseAsync(courseId, adminId ?? "system", reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning course for courseId: {CourseId}", courseId);
                throw;
            }
        }

        public Task<bool> DeleteCourseAsync(string courseId)
        {
            try
            {
                // Admin cannot permanently delete courses created by instructors
                // They can only ban courses. Only instructors can delete their own courses.
                _logger.LogWarning("Admin attempted to delete course {CourseId}. Admins can only ban courses, not delete them permanently.", courseId);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in delete course operation for courseId: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<UserRankingViewModel> GetUserRankingAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var userRankingData = await _adminRepo.GetUserRankingAsync(page, pageSize);
                var totalUsers = await _adminRepo.GetUserRankingTotalCountAsync();
                var averageCompletedLessons = await _adminRepo.GetAverageCompletedLessonsAsync();

                var userRankingItems = userRankingData.Select(u => new UserRankingItem
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName ?? "",
                    Email = u.Email,
                    UserImage = u.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                    UserRole = u.UserRole,
                    AccountCreatedAt = u.AccountCreatedAt,
                    LastLoginDate = u.LastLoginDate,
                    CompletedLessonsCount = u.CompletedLessonsCount,
                    TotalEnrolledCourses = u.TotalEnrolledCourses,
                    CompletedCourses = u.CompletedCourses,
                    AverageProgress = u.AverageProgress,
                    TotalTimeSpent = u.TotalTimeSpent,
                    CertificatesEarned = u.CertificatesEarned,
                    AchievementsEarned = u.AchievementsEarned,
                    LastActivityDate = u.LastActivityDate,
                    LastAccessedCourse = u.LastAccessedCourse,
                    CurrentCourse = u.CurrentCourse,
                    Rank = u.Rank
                }).ToList();

                return new UserRankingViewModel
                {
                    Users = userRankingItems,
                    TotalUsers = totalUsers,
                    AverageCompletedLessons = averageCompletedLessons,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ranking with page: {Page}, pageSize: {PageSize}", page, pageSize);
                throw;
            }
        }

        public async Task<ChatbotHistoryViewModel> GetChatbotHistoryAsync(string? search = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var chatbotHistoryData = await _adminRepo.GetChatbotHistoryAsync(search, userId, fromDate, toDate, page, pageSize);
                var totalRecords = await _adminRepo.GetChatbotHistoryTotalCountAsync(search, userId, fromDate, toDate);

                var chatbotHistoryItems = chatbotHistoryData.Select(h => new ChatbotConversationItem
                {
                    ConversationId = h.ConversationId,
                    UserId = h.UserId,
                    Username = h.User?.Username ?? "",
                    FullName = h.User?.FullName ?? "",
                    UserEmail = h.User?.UserEmail ?? "",
                    UserImage = h.User?.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                    ConversationTime = h.ConversationTime,
                    UserMessage = h.UserMessage,
                    BotResponse = h.BotResponse,
                    ConversationContext = h.ConversationContext,
                    FeedbackRating = h.FeedbackRating
                }).ToList();

                return new ChatbotHistoryViewModel
                {
                    Conversations = chatbotHistoryItems,
                    TotalConversations = totalRecords,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chatbot history with search: {Search}, userId: {UserId}, fromDate: {FromDate}, toDate: {ToDate}, page: {Page}, pageSize: {PageSize}",
                    search, userId, fromDate, toDate, page, pageSize);
                throw;
            }
        }

        #endregion

        /// <summary>
        /// Get admin dashboard data with authentication and authorization
        /// </summary>
        public async Task<AdminDashboardResult> GetAdminDashboardAsync(ClaimsPrincipal user)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminDashboardResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Get user ID from claims
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new AdminDashboardResult
                    {
                        IsSuccess = false,
                        Message = "User ID not found in claims.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access admin dashboard
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminDashboardResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Get admin dashboard data
                var dashboard = await GetAdminDashboardAsync(userId);

                return new AdminDashboardResult
                {
                    IsSuccess = true,
                    Message = "Admin dashboard data retrieved successfully.",
                    Data = dashboard
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Admin not found for user {UserId}", user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new AdminDashboardResult
                {
                    IsSuccess = false,
                    Message = "Admin account not found.",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard data for user {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new AdminDashboardResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving admin dashboard data.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get admin statistics with authorization
        /// </summary>
        public async Task<AdminStatisticsResult> GetAdminStatisticsAsync(ClaimsPrincipal user)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminStatisticsResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access statistics
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminStatisticsResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Get admin statistics
                var statistics = await GetAdminStatisticsAsync();

                return new AdminStatisticsResult
                {
                    IsSuccess = true,
                    Message = "Admin statistics retrieved successfully.",
                    Data = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin statistics for user {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new AdminStatisticsResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving admin statistics.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get recent users with authorization and validation
        /// </summary>
        public async Task<RecentUsersResult> GetRecentUsersAsync(ClaimsPrincipal user, int count = 5)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new RecentUsersResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access user data
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new RecentUsersResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Validate count parameter
                if (count <= 0 || count > 100)
                {
                    return new RecentUsersResult
                    {
                        IsSuccess = false,
                        Message = "Count must be between 1 and 100.",
                        Data = null
                    };
                }

                // Get recent users
                var recentUsers = await GetRecentUsersAsync(count);

                return new RecentUsersResult
                {
                    IsSuccess = true,
                    Message = $"Retrieved {recentUsers.Count} recent users successfully.",
                    Data = recentUsers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent users for admin {UserId}, count: {Count}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value, count);
                return new RecentUsersResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving recent users.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get recent courses with authorization and validation
        /// </summary>
        public async Task<RecentCoursesResult> GetRecentCoursesAsync(ClaimsPrincipal user, int count = 5)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new RecentCoursesResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access course data
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new RecentCoursesResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Validate count parameter
                if (count <= 0 || count > 100)
                {
                    return new RecentCoursesResult
                    {
                        IsSuccess = false,
                        Message = "Count must be between 1 and 100.",
                        Data = null
                    };
                }

                // Get recent courses
                var recentCourses = await GetRecentCoursesAsync(count);

                return new RecentCoursesResult
                {
                    IsSuccess = true,
                    Message = $"Retrieved {recentCourses.Count} recent courses successfully.",
                    Data = recentCourses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent courses for admin {UserId}, count: {Count}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value, count);
                return new RecentCoursesResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving recent courses.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get total revenue with authorization
        /// </summary>
        public async Task<TotalRevenueResult> GetTotalRevenueAsync(ClaimsPrincipal user)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new TotalRevenueResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = 0
                    };
                }

                // Authorization check - only Admin role can access revenue data
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new TotalRevenueResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = 0
                    };
                }

                // Get total revenue
                var totalRevenue = await GetTotalRevenueAsync();

                return new TotalRevenueResult
                {
                    IsSuccess = true,
                    Message = "Total revenue retrieved successfully.",
                    Data = totalRevenue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total revenue for admin {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new TotalRevenueResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving total revenue.",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Get all users with authorization and pagination
        /// </summary>
        public async Task<AdminUsersResult> GetAllUsersAsync(ClaimsPrincipal user, string? search = null, string? roleFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminUsersResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access user management
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminUsersResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Get all users with filtering and pagination
                var usersData = await GetAllUsersAsync(search, roleFilter, page, pageSize);

                return new AdminUsersResult
                {
                    IsSuccess = true,
                    Message = "Users retrieved successfully.",
                    Data = usersData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for admin {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new AdminUsersResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving users.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get all courses with authorization and pagination
        /// </summary>
        public async Task<AdminCoursesResult> GetAllCoursesAsync(ClaimsPrincipal user, string? search = null, string? categoryFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminCoursesResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access course management
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminCoursesResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Get all courses with filtering and pagination
                var coursesData = await GetAllCoursesAsync(search, categoryFilter, null, null, null, null, null, page, pageSize);

                return new AdminCoursesResult
                {
                    IsSuccess = true,
                    Message = "Courses retrieved successfully.",
                    Data = coursesData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving courses for admin {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new AdminCoursesResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving courses.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Update user status with authorization
        /// </summary>
        public async Task<AdminOperationResult> UpdateUserStatusAsync(ClaimsPrincipal user, string userId, bool isBanned)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                // Authorization check - only Admin role can update user status
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required."
                    };
                }

                // Update user status
                var success = await UpdateUserStatusAsync(userId, isBanned);

                if (success)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = true,
                        Message = $"User status updated successfully."
                    };
                }
                else
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Failed to update user status."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for user {UserId}", userId);
                return new AdminOperationResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while updating user status."
                };
            }
        }

        /// <summary>
        /// Delete user with authorization
        /// </summary>
        public async Task<AdminOperationResult> DeleteUserAsync(ClaimsPrincipal user, string userId)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                // Authorization check - only Admin role can delete users
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required."
                    };
                }

                // Prevent admin from deleting themselves
                var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Cannot delete your own account."
                    };
                }

                // Delete user
                var success = await DeleteUserAsync(userId);

                if (success)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = true,
                        Message = "User deleted successfully."
                    };
                }
                else
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Failed to delete user."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return new AdminOperationResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while deleting user."
                };
            }
        }

        /// <summary>
        /// Update course status with authorization
        /// </summary>
        public async Task<AdminOperationResult> UpdateCourseStatusAsync(ClaimsPrincipal user, string courseId, bool isApproved)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                // Authorization check - only Admin role can update course status
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required."
                    };
                }

                // Get admin user ID
                var adminId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Update course status
                var success = await UpdateCourseStatusAsync(courseId, isApproved, adminId);

                if (success)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = true,
                        Message = "Course status updated successfully."
                    };
                }
                else
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Failed to update course status."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course status for course {CourseId}", courseId);
                return new AdminOperationResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while updating course status."
                };
            }
        }

        /// <summary>
        /// Delete course with authorization
        /// </summary>
        public async Task<AdminOperationResult> DeleteCourseAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                // Authorization check - only Admin role can delete courses
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required."
                    };
                }

                // Delete course
                var success = await DeleteCourseAsync(courseId);

                if (success)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = true,
                        Message = "Course deleted successfully."
                    };
                }
                else
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Failed to delete course."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", courseId);
                return new AdminOperationResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while deleting course."
                };
            }
        }

        /// <summary>
        /// Get user ranking with authorization
        /// </summary>
        public async Task<UserRankingResult> GetUserRankingAsync(ClaimsPrincipal user, int page = 1, int pageSize = 20)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new UserRankingResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access user ranking
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new UserRankingResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Validate pagination parameters
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return new UserRankingResult
                    {
                        IsSuccess = false,
                        Message = "Invalid pagination parameters.",
                        Data = null
                    };
                }

                // Get user ranking data
                var userRanking = await GetUserRankingAsync(page, pageSize);

                return new UserRankingResult
                {
                    IsSuccess = true,
                    Message = "User ranking data retrieved successfully.",
                    Data = userRanking
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user ranking data for admin {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new UserRankingResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving user ranking data.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get chatbot history with authorization
        /// </summary>
        public async Task<ChatbotHistoryResult> GetChatbotHistoryAsync(ClaimsPrincipal user, string? search = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 20)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new ChatbotHistoryResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access chatbot history
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new ChatbotHistoryResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Validate pagination parameters
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return new ChatbotHistoryResult
                    {
                        IsSuccess = false,
                        Message = "Invalid pagination parameters.",
                        Data = null
                    };
                }

                // Validate date range
                if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
                {
                    return new ChatbotHistoryResult
                    {
                        IsSuccess = false,
                        Message = "From date cannot be later than to date.",
                        Data = null
                    };
                }

                // Get chatbot history data
                var chatbotHistory = await GetChatbotHistoryAsync(search, userId, fromDate, toDate, page, pageSize);

                return new ChatbotHistoryResult
                {
                    IsSuccess = true,
                    Message = "Chatbot history data retrieved successfully.",
                    Data = chatbotHistory
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chatbot history data for admin {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new ChatbotHistoryResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving chatbot history data.",
                    Data = null
                };
            }
        }
    }
}







