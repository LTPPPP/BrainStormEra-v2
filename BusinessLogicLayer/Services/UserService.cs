using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BusinessLogicLayer.Constants;

namespace BusinessLogicLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepo _userRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly IAuthRepo _authRepo; // Added for authentication operations
        private readonly BrainStormEraContext _context; // Keep for complex queries that involve multiple entities
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserService> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

        public UserService(
            IUserRepo userRepo,
            ICourseRepo courseRepo,
            IAuthRepo authRepo,
            BrainStormEraContext context,
            IMemoryCache cache,
            ILogger<UserService> logger)
        {
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _authRepo = authRepo;
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Account?> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _authRepo.GetUserByUsernameAsync(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
                throw;
            }
        }

        public async Task<Account?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _authRepo.GetUserByEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                return await _authRepo.UsernameExistsAsync(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username existence: {Username}", username);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                return await _authRepo.EmailExistsAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence: {Email}", email);
                throw;
            }
        }

        public async Task<bool> CreateUserAsync(Account user)
        {
            try
            {
                return await _authRepo.CreateUserAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(Account user)
        {
            try
            {
                return await _authRepo.UpdateUserAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                throw;
            }
        }

        public async Task<bool> VerifyPasswordAsync(string password, string storedHash)
        {
            return await _authRepo.VerifyPasswordAsync(password, storedHash);
        }

        public async Task UpdateLastLoginAsync(string userId)
        {
            try
            {
                await _authRepo.UpdateLastLoginAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> BanUserAsync(string userId, bool isBanned)
        {
            try
            {
                return await _authRepo.BanUserAsync(userId, isBanned);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ban status for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<EnrolledUserViewModel>> GetEnrolledUsersForInstructorAsync(string instructorId, string? courseId = null, string? search = null, string? statusFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                // For complex queries involving multiple entities and projections, 
                // we can still use the context directly or create specialized repository methods
                var query = _context.Enrollments
                    .Include(e => e.User)
                    .Include(e => e.Course)
                    .Where(e => e.Course.AuthorId == instructorId);

                if (!string.IsNullOrWhiteSpace(courseId))
                {
                    query = query.Where(e => e.CourseId == courseId);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(e => e.User.FullName!.Contains(search) ||
                                           e.User.UserEmail.Contains(search) ||
                                           e.User.Username.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    var status = int.Parse(statusFilter);
                    query = query.Where(e => e.EnrollmentStatus == status);
                }

                var enrollments = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new EnrolledUserViewModel
                    {
                        UserId = e.User.UserId,
                        Username = e.User.Username,
                        FullName = e.User.FullName ?? "",
                        Email = e.User.UserEmail,
                        UserImage = e.User.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                        CourseId = e.CourseId,
                        CourseName = e.Course.CourseName,
                        EnrollmentDate = e.EnrollmentCreatedAt,
                        LastAccessDate = e.EnrollmentUpdatedAt,
                        ProgressPercentage = e.ProgressPercentage ?? 0,
                        EnrollmentStatus = e.EnrollmentStatus,
                        CurrentLessonName = e.CurrentLessonId != null ? "Current Lesson" : null,
                        LastAccessedLessonName = e.LastAccessedLessonId != null ? "Last Lesson" : null
                    })
                    .ToListAsync();

                return enrollments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving enrolled users for instructor: {InstructorId}", instructorId);
                throw;
            }
        }

        public async Task<UserDetailViewModel?> GetUserDetailForInstructorAsync(string instructorId, string userId)
        {
            try
            {
                var user = await _context.Accounts
                    .Include(u => u.Enrollments.Where(e => e.Course.AuthorId == instructorId))
                        .ThenInclude(e => e.Course)
                    .Include(u => u.UserAchievements)
                        .ThenInclude(ua => ua.Achievement)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return null;

                var enrollments = user.Enrollments.Select(e => new UserCourseEnrollment
                {
                    CourseId = e.CourseId,
                    CourseName = e.Course.CourseName,
                    CourseImage = e.Course.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    EnrollmentDate = e.EnrollmentCreatedAt,
                    LastAccessDate = e.EnrollmentUpdatedAt,
                    ProgressPercentage = e.ProgressPercentage ?? 0,
                    CurrentLessonName = e.CurrentLessonId != null ? "Current Lesson" : null,
                    LastAccessedLessonName = e.LastAccessedLessonId != null ? "Last Lesson" : null,
                    EnrollmentStatus = e.EnrollmentStatus
                }).ToList(); var achievements = user.UserAchievements.Select(ua => new UserAchievementSummary
                {
                    AchievementName = ua.Achievement.AchievementName ?? ua.Achievement.AchievementType ?? "",
                    Description = ua.Achievement.AchievementDescription ?? "",
                    EarnedDate = ua.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                    AchievementIcon = ua.Achievement.AchievementIcon ?? GetAchievementIcon(ua.Achievement.AchievementType ?? "default")
                }).ToList();

                return new UserDetailViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName ?? "",
                    Email = user.UserEmail,
                    PhoneNumber = user.PhoneNumber,
                    UserImage = user.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                    AccountCreatedAt = user.AccountCreatedAt,
                    Enrollments = enrollments,
                    Achievements = achievements,
                    TotalCertificates = user.Certificates.Count,
                    OverallProgress = enrollments.Any() ? (double)enrollments.Average(e => e.ProgressPercentage) : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user detail for instructor: {InstructorId}, user: {UserId}", instructorId, userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserEnrollmentProgressAsync(string instructorId, string userId, string courseId, decimal progressPercentage, string? currentLessonId = null)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.UserId == userId &&
                                           e.CourseId == courseId &&
                                           e.Course.AuthorId == instructorId);

                if (enrollment == null)
                    return false;

                enrollment.ProgressPercentage = progressPercentage;
                enrollment.CurrentLessonId = currentLessonId;
                enrollment.EnrollmentUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user enrollment progress");
                throw;
            }
        }

        public async Task<bool> UpdateUserEnrollmentStatusAsync(string instructorId, string userId, string courseId, int status)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.UserId == userId &&
                                           e.CourseId == courseId &&
                                           e.Course.AuthorId == instructorId);

                if (enrollment == null)
                    return false;

                enrollment.EnrollmentStatus = status;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user enrollment status");
                throw;
            }
        }

        public async Task<List<CourseFilterOption>> GetInstructorCoursesForFilterAsync(string instructorId)
        {
            try
            {
                return await _context.Courses
                    .Where(c => c.AuthorId == instructorId)
                    .Select(c => new CourseFilterOption
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        EnrollmentCount = c.Enrollments.Count
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving instructor courses for filter");
                throw;
            }
        }

        public async Task<UserManagementViewModel> GetUserManagementDataAsync(string instructorId, string? courseId = null, string? search = null, string? statusFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var instructor = await _context.Accounts.FindAsync(instructorId);
                var enrolledUsers = await GetEnrolledUsersForInstructorAsync(instructorId, courseId, search, statusFilter, page, pageSize);
                var courseFilters = await GetInstructorCoursesForFilterAsync(instructorId);

                var totalQuery = _context.Enrollments
                    .Include(e => e.Course)
                    .Where(e => e.Course.AuthorId == instructorId);

                if (!string.IsNullOrWhiteSpace(courseId))
                {
                    totalQuery = totalQuery.Where(e => e.CourseId == courseId);
                }

                var totalUsers = await totalQuery.CountAsync();
                var activeUsers = await totalQuery.CountAsync(e => e.EnrollmentStatus == 1);
                var completedUsers = await totalQuery.CountAsync(e => e.EnrollmentStatus == 3);
                var averageProgress = totalUsers > 0 ? await totalQuery.AverageAsync(e => (double)(e.ProgressPercentage ?? 0)) : 0;

                return new UserManagementViewModel
                {
                    InstructorId = instructorId,
                    InstructorName = instructor?.FullName ?? "Unknown",
                    EnrolledUsers = enrolledUsers,
                    CourseFilters = courseFilters,
                    SelectedCourseId = courseId,
                    SearchQuery = search,
                    StatusFilter = statusFilter,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
                    TotalUsers = totalUsers,
                    PageSize = pageSize,
                    TotalEnrolledUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    CompletedUsers = completedUsers,
                    AverageProgress = averageProgress
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user management data for instructor: {InstructorId}", instructorId);
                throw;
            }
        }

        public async Task<bool> UnenrollUserFromCourseAsync(string instructorId, string userId, string courseId)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.UserId == userId &&
                                           e.CourseId == courseId &&
                                           e.Course.AuthorId == instructorId);

                if (enrollment == null)
                    return false;

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unenrolling user from course");
                throw;
            }
        }

        public async Task<int> BulkUpdateUserStatusAsync(string instructorId, List<string> userIds, string courseId, int status)
        {
            try
            {
                var enrollments = await _context.Enrollments
                    .Include(e => e.Course)
                    .Where(e => userIds.Contains(e.UserId) &&
                               e.CourseId == courseId &&
                               e.Course.AuthorId == instructorId)
                    .ToListAsync();

                foreach (var enrollment in enrollments)
                {
                    enrollment.EnrollmentStatus = status;
                }

                await _context.SaveChangesAsync();
                return enrollments.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk user status update");
                throw;
            }
        }

        // Helper methods
        private string GetAchievementIcon(string achievementType)
        {
            return achievementType.ToLower() switch
            {
                "completion" => "fas fa-trophy",
                "streak" => "fas fa-fire",
                "perfect_score" => "fas fa-star",
                "first_course" => "fas fa-graduation-cap",
                "speed_learner" => "fas fa-bolt",
                _ => "fas fa-medal"
            };
        }
    }
}








