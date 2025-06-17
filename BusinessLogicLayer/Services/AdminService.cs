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
        private readonly IAchievementRepo _achievementRepo;
        private readonly IAchievementIconService _achievementIconService;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            IAdminRepo adminRepo,
            IUserRepo userRepo,
            ICourseRepo courseRepo,
            IAchievementRepo achievementRepo,
            IAchievementIconService achievementIconService,
            ILogger<AdminService> logger)
        {
            _adminRepo = adminRepo;
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _achievementRepo = achievementRepo;
            _achievementIconService = achievementIconService;
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
                        ApprovalStatus = c.ApprovalStatus,
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

                var totalRevenue = course.PaymentTransactions?.Where(p => p.TransactionStatus == "completed")
                    .Sum(p => p.Amount) ?? 0;

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
                // Admin cannot permanently delete courses created by instructors
                // They can only ban courses. Only instructors can delete their own courses.
                _logger.LogWarning("Admin attempted to delete course {CourseId}. Admins can only ban courses, not delete them permanently.", courseId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in delete course operation for courseId: {CourseId}", courseId);
                throw;
            }
        }

        // Achievement Management Methods
        public async Task<AdminAchievementsViewModel> GetAllAchievementsAsync(string? search = null, string? typeFilter = null, string? pointsFilter = null, int page = 1, int pageSize = 12, string? sortBy = "date_desc")
        {
            try
            {
                var allAchievements = await _achievementRepo.GetAllAchievementsAsync();

                // Apply filters
                var filteredAchievements = allAchievements.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    filteredAchievements = filteredAchievements.Where(a =>
                        a.AchievementName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (a.AchievementDescription != null && a.AchievementDescription.Contains(search, StringComparison.OrdinalIgnoreCase)));
                }

                if (!string.IsNullOrEmpty(typeFilter))
                {
                    filteredAchievements = filteredAchievements.Where(a =>
                        a.AchievementType != null && a.AchievementType.Equals(typeFilter, StringComparison.OrdinalIgnoreCase));
                }

                var achievements = filteredAchievements.ToList();
                var totalAchievements = achievements.Count;

                // Apply sorting
                var sortedAchievements = sortBy?.ToLower() switch
                {
                    "name_asc" => achievements.OrderBy(a => a.AchievementName),
                    "name_desc" => achievements.OrderByDescending(a => a.AchievementName),
                    "date_asc" => achievements.OrderBy(a => a.AchievementCreatedAt),
                    "date_desc" => achievements.OrderByDescending(a => a.AchievementCreatedAt),
                    "type_asc" => achievements.OrderBy(a => a.AchievementType),
                    "type_desc" => achievements.OrderByDescending(a => a.AchievementType),
                    "awarded_desc" => achievements.OrderByDescending(a => a.UserAchievements?.Count ?? 0),
                    "awarded_asc" => achievements.OrderBy(a => a.UserAchievements?.Count ?? 0),
                    _ => achievements.OrderByDescending(a => a.AchievementCreatedAt) // default to newest first
                };

                // Apply pagination
                var paginatedAchievements = sortedAchievements
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Calculate statistics
                var courseAchievements = allAchievements.Count(a => a.AchievementType?.Equals("course_completion", StringComparison.OrdinalIgnoreCase) == true || a.AchievementType?.Equals("first_course", StringComparison.OrdinalIgnoreCase) == true);
                var quizAchievements = allAchievements.Count(a => a.AchievementType?.Equals("quiz_master", StringComparison.OrdinalIgnoreCase) == true);
                var specialAchievements = allAchievements.Count(a => a.AchievementType?.Equals("instructor", StringComparison.OrdinalIgnoreCase) == true || a.AchievementType?.Equals("student_engagement", StringComparison.OrdinalIgnoreCase) == true);
                var milestoneAchievements = allAchievements.Count(a => a.AchievementType?.Equals("streak", StringComparison.OrdinalIgnoreCase) == true);

                // Calculate total times awarded (would need user achievements data, using placeholder for now)
                var totalAwarded = 0; // This would require joining with UserAchievement table

                return new AdminAchievementsViewModel
                {
                    Achievements = paginatedAchievements.Select(a => new AdminAchievementViewModel
                    {
                        AchievementId = a.AchievementId,
                        AchievementName = a.AchievementName,
                        AchievementDescription = a.AchievementDescription ?? "",
                        AchievementIcon = a.AchievementIcon ?? "fas fa-trophy",
                        AchievementType = a.AchievementType ?? "general",

                        AchievementCreatedAt = a.AchievementCreatedAt,
                        IsActive = true, // Add this field to Achievement model if needed
                        TimesAwarded = a.UserAchievements?.Count ?? 0 // Get actual count from UserAchievements
                    }).ToList(),
                    SearchQuery = search,
                    TypeFilter = typeFilter,
                    PointsFilter = pointsFilter,
                    SortBy = sortBy,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalAchievements / pageSize),
                    TotalAchievements = allAchievements.Count,
                    CourseAchievements = courseAchievements,
                    QuizAchievements = quizAchievements,
                    SpecialAchievements = specialAchievements,
                    MilestoneAchievements = milestoneAchievements,
                    TotalAwarded = totalAwarded
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all achievements with search: {Search}, typeFilter: {TypeFilter}, pointsFilter: {PointsFilter}, page: {Page}, pageSize: {PageSize}",
                    search, typeFilter, pointsFilter, page, pageSize);
                throw;
            }
        }

        public async Task<AdminAchievementViewModel?> GetAchievementByIdAsync(string achievementId)
        {
            try
            {
                var achievement = await _achievementRepo.GetByIdAsync(achievementId);
                if (achievement == null) return null;

                return new AdminAchievementViewModel
                {
                    AchievementId = achievement.AchievementId,
                    AchievementName = achievement.AchievementName,
                    AchievementDescription = achievement.AchievementDescription ?? "",
                    AchievementIcon = achievement.AchievementIcon ?? "fas fa-trophy",
                    AchievementType = achievement.AchievementType ?? "general",
                    AchievementCreatedAt = achievement.AchievementCreatedAt,
                    IsActive = true,
                    TimesAwarded = achievement.UserAchievements?.Count ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement by ID: {AchievementId}", achievementId);
                throw;
            }
        }

        public async Task<(bool Success, string? AchievementId)> CreateAchievementAsync(CreateAchievementRequest request, string? adminId = null)
        {
            try
            {
                var achievementId = Guid.NewGuid().ToString();
                var achievement = new DataAccessLayer.Models.Achievement
                {
                    AchievementId = achievementId,
                    AchievementName = request.AchievementName,
                    AchievementDescription = request.AchievementDescription,
                    AchievementIcon = request.AchievementIcon,
                    AchievementType = request.AchievementType,
                    AchievementCreatedAt = DateTime.UtcNow
                };

                var result = await _achievementRepo.CreateAchievementAsync(achievement);
                var success = !string.IsNullOrEmpty(result);
                return (success, success ? achievementId : null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating achievement: {AchievementName}", request.AchievementName);
                throw;
            }
        }

        public async Task<bool> UpdateAchievementAsync(UpdateAchievementRequest request, string? adminId = null)
        {
            try
            {
                var existingAchievement = await _achievementRepo.GetByIdAsync(request.AchievementId);
                if (existingAchievement == null) return false;

                existingAchievement.AchievementName = request.AchievementName;
                existingAchievement.AchievementDescription = request.AchievementDescription;
                existingAchievement.AchievementIcon = request.AchievementIcon;
                existingAchievement.AchievementType = request.AchievementType;

                return await _achievementRepo.UpdateAchievementAsync(existingAchievement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating achievement: {AchievementId}", request.AchievementId);
                throw;
            }
        }

        public async Task<bool> DeleteAchievementAsync(string achievementId, string? adminId = null)
        {
            try
            {
                return await _achievementRepo.DeleteAchievementAsync(achievementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting achievement: {AchievementId}", achievementId);
                throw;
            }
        }

        public async Task<(bool Success, string? IconPath, string? ErrorMessage)> UploadAchievementIconAsync(Microsoft.AspNetCore.Http.IFormFile file, string achievementId, string? adminId = null)
        {
            try
            {
                // Check if achievement exists
                var achievement = await _achievementRepo.GetByIdAsync(achievementId);
                if (achievement == null)
                {
                    return (false, null, "Achievement not found.");
                }

                // Upload the icon
                var uploadResult = await _achievementIconService.UploadAchievementIconAsync(file, achievementId);
                if (!uploadResult.Success)
                {
                    return uploadResult;
                }

                // Update achievement with new icon path
                var updateResult = await _achievementRepo.UpdateAchievementIconAsync(achievementId, uploadResult.IconPath!);
                if (!updateResult)
                {
                    // Clean up uploaded file if database update fails
                    await _achievementIconService.DeleteAchievementIconAsync(uploadResult.IconPath);
                    return (false, null, "Failed to update achievement icon in database.");
                }

                _logger.LogInformation("Achievement icon uploaded successfully: {AchievementId} by admin {AdminId}", achievementId, adminId);
                return (true, uploadResult.IconPath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading achievement icon: {AchievementId}", achievementId);
                return (false, null, "An error occurred while uploading the achievement icon.");
            }
        }
    }
}
