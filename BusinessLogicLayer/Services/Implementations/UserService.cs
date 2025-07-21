using Microsoft.Extensions.Logging;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Utilities;
using Microsoft.Extensions.Caching.Memory;
using BusinessLogicLayer.Constants;

namespace BusinessLogicLayer.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepo _userRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly IAuthRepo _authRepo; // Added for authentication operations
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserService> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

        public UserService(
            IUserRepo userRepo,
            ICourseRepo courseRepo,
            IAuthRepo authRepo,
            IMemoryCache cache,
            ILogger<UserService> logger)
        {
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _authRepo = authRepo;
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
                // Use the repository method for getting users with enrollments
                var users = await _userRepo.GetUsersWithEnrollmentsAsync(instructorId, courseId, search, statusFilter, page, pageSize);

                // Convert to view models - this would need to be implemented in the repository
                // For now, we'll return an empty list and the repository should handle the conversion
                return new List<EnrolledUserViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrolled users for instructor: {InstructorId}", instructorId);
                return new List<EnrolledUserViewModel>();
            }
        }

        public async Task<UserDetailViewModel?> GetUserDetailForInstructorAsync(string instructorId, string userId, string? courseId = null)
        {
            try
            {
                var user = await _userRepo.GetUserWithDetailsAsync(userId);
                if (user == null)
                    return null;

                // Convert to view model
                return new UserDetailViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName ?? "",
                    Email = user.UserEmail,
                    UserImage = user.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                    // Add other properties as needed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user detail for instructor: {InstructorId}, user: {UserId}", instructorId, userId);
                return null;
            }
        }

        public async Task<bool> UpdateUserEnrollmentProgressAsync(string instructorId, string userId, string courseId, decimal progressPercentage, string? currentLessonId = null)
        {
            try
            {
                return await _userRepo.UpdateUserEnrollmentProgressAsync(instructorId, userId, courseId, progressPercentage, currentLessonId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user enrollment progress: {InstructorId}, {UserId}, {CourseId}", instructorId, userId, courseId);
                return false;
            }
        }

        public async Task<bool> UpdateUserEnrollmentStatusAsync(string instructorId, string userId, string courseId, int status)
        {
            try
            {
                return await _userRepo.UpdateUserEnrollmentStatusAsync(instructorId, userId, courseId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user enrollment status: {InstructorId}, {UserId}, {CourseId}", instructorId, userId, courseId);
                return false;
            }
        }

        public async Task<List<CourseFilterOption>> GetInstructorCoursesForFilterAsync(string instructorId)
        {
            try
            {
                var courses = await _courseRepo.GetInstructorCoursesAsync(instructorId, null, null, 1, int.MaxValue);

                return courses.Select(c => new CourseFilterOption
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor courses for filter: {InstructorId}", instructorId);
                return new List<CourseFilterOption>();
            }
        }

        public async Task<UserManagementViewModel> GetUserManagementDataAsync(string instructorId, string? courseId = null, string? search = null, string? statusFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var users = await _userRepo.GetUsersWithEnrollmentsAsync(instructorId, courseId, search, statusFilter, page, pageSize);
                var courses = await GetInstructorCoursesForFilterAsync(instructorId);

                // Get instructor info
                var instructor = await _userRepo.GetByIdAsync(instructorId);

                return new UserManagementViewModel
                {
                    InstructorId = instructorId,
                    InstructorName = instructor?.FullName ?? "Unknown",
                    EnrolledUsers = new List<EnrolledUserViewModel>(), // Convert users to view models
                    CourseFilters = courses,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalUsers = users.Count,
                    SearchQuery = search,
                    StatusFilter = statusFilter,
                    SelectedCourseId = courseId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user management data for instructor: {InstructorId}", instructorId);
                return new UserManagementViewModel
                {
                    InstructorId = instructorId,
                    InstructorName = "Unknown",
                    EnrolledUsers = new List<EnrolledUserViewModel>(),
                    CourseFilters = new List<CourseFilterOption>(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalUsers = 0,
                    SearchQuery = search,
                    StatusFilter = statusFilter,
                    SelectedCourseId = courseId
                };
            }
        }

        public async Task<bool> UnenrollUserFromCourseAsync(string instructorId, string userId, string courseId)
        {
            try
            {
                return await _userRepo.UnenrollUserFromCourseAsync(instructorId, userId, courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unenrolling user from course: {InstructorId}, {UserId}, {CourseId}", instructorId, userId, courseId);
                return false;
            }
        }

        public async Task<int> BulkUpdateUserStatusAsync(string instructorId, List<string> userIds, string courseId, int status)
        {
            try
            {
                return await _userRepo.BulkUpdateUserStatusAsync(instructorId, userIds, courseId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating user status: {InstructorId}, {CourseId}", instructorId, courseId);
                return 0;
            }
        }

        private string GetAchievementIcon(string achievementType)
        {
            return achievementType switch
            {
                "course_completion" => "🎓",
                "quiz_mastery" => "🧠",
                "streak" => "🔥",
                "social" => "👥",
                "first_course" => "🥇",
                "perfect_score" => "💯",
                _ => "🏆"
            };
        }
    }
}