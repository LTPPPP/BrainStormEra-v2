using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface IUserService
    {
        Task<Account?> GetUserByUsernameAsync(string username);
        Task<Account?> GetUserByEmailAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> CreateUserAsync(Account user);
        Task<bool> UpdateUserAsync(Account user);
        Task<bool> VerifyPasswordAsync(string password, string storedHash);
        Task UpdateLastLoginAsync(string userId);
        Task<bool> BanUserAsync(string userId, bool isBanned);

        // Instructor user management methods
        Task<List<EnrolledUserViewModel>> GetEnrolledUsersForInstructorAsync(string instructorId, string? courseId = null, string? search = null, string? statusFilter = null, int page = 1, int pageSize = 10);
        Task<UserDetailViewModel?> GetUserDetailForInstructorAsync(string instructorId, string userId);
        Task<bool> UpdateUserEnrollmentProgressAsync(string instructorId, string userId, string courseId, decimal progressPercentage, string? currentLessonId = null);
        Task<bool> UpdateUserEnrollmentStatusAsync(string instructorId, string userId, string courseId, int status);
        Task<List<CourseFilterOption>> GetInstructorCoursesForFilterAsync(string instructorId);
        Task<UserManagementViewModel> GetUserManagementDataAsync(string instructorId, string? courseId = null, string? search = null, string? statusFilter = null, int page = 1, int pageSize = 10);
        Task<bool> UnenrollUserFromCourseAsync(string instructorId, string userId, string courseId);
        Task<int> BulkUpdateUserStatusAsync(string instructorId, List<string> userIds, string courseId, int status);
    }
}