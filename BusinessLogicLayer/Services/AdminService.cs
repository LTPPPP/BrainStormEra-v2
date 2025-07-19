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
        private readonly IAchievementService _achievementService;
        private readonly ICertificateService _certificateService;
        private readonly IAchievementRepo _achievementRepo;

        private readonly ILogger<AdminService> _logger;
        private readonly IPointsService _pointsService;

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
                    string? relatedCourseName = null;
                    if (!string.IsNullOrEmpty(ua.RelatedCourseId))
                    {
                        relatedCourseName = await _achievementRepo.GetCourseNameAsync(ua.RelatedCourseId);
                    }

                    achievements.Add(new AchievementSummaryViewModel
                    {
                        AchievementId = ua.AchievementId,
                        AchievementName = ua.Achievement.AchievementName,
                        AchievementDescription = ua.Achievement.AchievementDescription ?? "",
                        AchievementIcon = ua.Achievement.AchievementIcon ?? "fas fa-trophy",
                        AchievementType = ua.Achievement.AchievementType ?? "",
                        PointsReward = ua.Achievement.PointsReward,
                        ReceivedDate = ua.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                        RelatedCourseName = relatedCourseName
                    });
                }

                // Calculate totals
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
                var course = await _adminRepo.GetCourseWithDetailsAsync(courseId);
                if (course == null)
                {
                    return null;
                }

                // Calculate statistics
                var averageRating = course.Feedbacks?.Any() == true
                    ? course.Feedbacks.Average(f => f.StarRating ?? 0)
                    : 0;

                // Calculate revenue (would need to be calculated separately from payment transactions)
                var totalRevenue = 0m; // Placeholder - would need to query payment transactions separately

                var completionRate = 0; // Would need to calculate from enrollments progress

                return new AdminCourseDetailsViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
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
                    EnforceSequentialAccess = course.EnforceSequentialAccess ?? false,
                    AllowLessonPreview = course.AllowLessonPreview ?? false,
                    ApprovedBy = course.ApprovedBy,
                    ApprovedAt = course.ApprovedAt,

                    // Instructor information
                    InstructorId = course.AuthorId,
                    InstructorName = course.Author?.FullName ?? "",
                    InstructorEmail = course.Author?.UserEmail ?? "",
                    InstructorBio = "", // Bio property doesn't exist in Account model
                    InstructorImage = course.Author?.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",

                    // Course structure
                    Chapters = course.Chapters?.Select(ch => new CourseChapterSummary
                    {
                        ChapterId = ch.ChapterId,
                        ChapterName = ch.ChapterName,
                        ChapterOrder = ch.ChapterOrder ?? 0,
                        LessonCount = ch.Lessons?.Count ?? 0,
                        IsLocked = ch.IsLocked ?? false
                    }).OrderBy(ch => ch.ChapterOrder).ToList() ?? new List<CourseChapterSummary>(),

                    Categories = course.CourseCategories?.Select(cc => cc.CourseCategoryName ?? "").ToList() ?? new List<string>(),

                    // Statistics
                    EnrollmentCount = course.Enrollments?.Count ?? 0,
                    AverageRating = (decimal)averageRating,
                    ReviewCount = course.Feedbacks?.Count ?? 0,
                    Revenue = totalRevenue,
                    TotalLessons = course.Chapters?.Sum(ch => ch.Lessons?.Count ?? 0) ?? 0,
                    TotalQuizzes = course.Quizzes?.Count ?? 0,
                    CompletionRate = completionRate,

                    // Recent reviews (limit to 5 most recent)
                    RecentReviews = course.Feedbacks?.OrderByDescending(f => f.FeedbackCreatedAt)
                        .Take(5)
                        .Select(f => new CourseReviewSummary
                        {
                            UserId = f.UserId,
                            UserName = f.User?.FullName ?? "Unknown User",
                            UserImage = f.User?.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                            Rating = (decimal)(f.StarRating ?? 0),
                            Comment = f.Comment ?? "",
                            CreatedAt = f.FeedbackCreatedAt
                        }).ToList() ?? new List<CourseReviewSummary>(),

                    // Recent enrollments (limit to 5 most recent)
                    RecentEnrollments = course.Enrollments?.OrderByDescending(e => e.EnrollmentCreatedAt)
                        .Take(5)
                        .Select(e => new CourseEnrollmentSummary
                        {
                            UserId = e.UserId,
                            UserName = e.User?.FullName ?? "Unknown User",
                            UserImage = e.User?.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                            EnrolledAt = e.EnrollmentCreatedAt,
                            ProgressPercentage = (int)Math.Round(e.ProgressPercentage ?? 0),
                            PaymentStatus = "Unknown" // PaymentStatus doesn't exist in Enrollment model
                        }).ToList() ?? new List<CourseEnrollmentSummary>()
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
                return await _adminRepo.BanUserAsync(userId, isBanned);
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
                return await _pointsService.UpdateUserPointsAsync(userId, pointsChange);
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
                // Validate the new role
                var validRoles = new[] { "learner", "instructor", "admin" };
                if (!validRoles.Contains(newRole, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid role attempted: {NewRole} for user {UserId}", newRole, userId);
                    return false;
                }

                // Get user details to check current role and validate promotion
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                // Check if user is already in the target role
                if (string.Equals(user.UserRole, newRole, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("User {UserId} is already in role {Role}", userId, newRole);
                    return true; // Consider this a success
                }

                // For promoting to instructor, check if user has any content
                if (string.Equals(newRole, "instructor", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if user has any courses, enrollments, or other content
                    var hasCourses = await _courseRepo.GetInstructorCoursesAsync(userId, null, null, 1, 1);
                    var hasEnrollments = user.Enrollments?.Any() == true;
                    var hasProgress = user.UserProgresses?.Any() == true;
                    var hasQuizAttempts = user.QuizAttempts?.Any() == true;

                    if (hasCourses.Any() || hasEnrollments || hasProgress || hasQuizAttempts)
                    {
                        _logger.LogWarning("Cannot promote user {UserId} to instructor - user has existing content", userId);
                        return false;
                    }
                }

                // Change the user role
                var result = await _adminRepo.ChangeUserRoleAsync(userId, newRole);

                if (result)
                {
                    _logger.LogInformation("Successfully changed user {UserId} role from {OldRole} to {NewRole}",
                        userId, user.UserRole, newRole);
                }
                else
                {
                    _logger.LogError("Failed to change user {UserId} role to {NewRole}", userId, newRole);
                }

                return result;
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

        // User Ranking Methods
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
                    FullName = u.FullName,
                    Email = u.Email,
                    UserImage = u.UserImage,
                    UserRole = u.UserRole,
                    AccountCreatedAt = u.AccountCreatedAt,
                    LastLoginDate = u.LastLoginDate,
                    CompletedLessonsCount = u.CompletedLessonsCount,
                    TotalEnrolledCourses = u.TotalEnrolledCourses,
                    CompletedCourses = u.CompletedCourses,
                    Rank = u.Rank,
                    AverageProgress = u.AverageProgress,
                    TotalTimeSpent = u.TotalTimeSpent,
                    CertificatesEarned = u.CertificatesEarned,
                    AchievementsEarned = u.AchievementsEarned,
                    LastActivityDate = u.LastActivityDate,
                    LastAccessedCourse = u.LastAccessedCourse,
                    CurrentCourse = u.CurrentCourse
                }).ToList();

                return new UserRankingViewModel
                {
                    Users = userRankingItems,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
                    PageSize = pageSize,
                    TotalUsers = totalUsers,
                    TotalCompletedLessons = userRankingItems.Sum(u => u.CompletedLessonsCount),
                    AverageCompletedLessons = averageCompletedLessons
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ranking data");
                throw;
            }
        }

        // Chatbot History Methods
        public async Task<ChatbotHistoryViewModel> GetChatbotHistoryAsync(string? search = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var conversations = await _adminRepo.GetChatbotHistoryAsync(search, userId, fromDate, toDate, page, pageSize);
                var totalConversations = await _adminRepo.GetChatbotHistoryTotalCountAsync(search, userId, fromDate, toDate);
                var statistics = await _adminRepo.GetChatbotHistoryStatisticsAsync();

                var conversationItems = conversations.Select(c => new ChatbotConversationItem
                {
                    ConversationId = c.ConversationId,
                    UserId = c.UserId,
                    Username = c.User.Username ?? "",
                    FullName = c.User.FullName ?? "",
                    UserEmail = c.User.UserEmail ?? "",
                    UserImage = c.User.UserImage ?? "/SharedMedia/defaults/default-avatar.svg",
                    ConversationTime = c.ConversationTime,
                    UserMessage = c.UserMessage,
                    BotResponse = c.BotResponse,
                    ConversationContext = c.ConversationContext,
                    FeedbackRating = c.FeedbackRating
                }).ToList();

                return new ChatbotHistoryViewModel
                {
                    Conversations = conversationItems,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalConversations / pageSize),
                    PageSize = pageSize,
                    TotalConversations = totalConversations,
                    SearchQuery = search,
                    UserIdFilter = userId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalUsers = (int)statistics["TotalUsers"],
                    AverageRating = (double)statistics["AverageRating"],
                    TotalRatings = (int)statistics["TotalRatings"],
                    RatingDistribution = (Dictionary<int, int>)statistics["RatingDistribution"]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chatbot history data");
                throw;
            }
        }
    }
}
