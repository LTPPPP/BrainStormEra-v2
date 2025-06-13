using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IUserRepo : IBaseRepo<Account>
    {
        // User-specific methods
        Task<Account?> GetUserByUsernameAsync(string username);
        Task<Account?> GetUserByEmailAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> CreateUserAsync(Account user);
        Task<bool> UpdateUserAsync(Account user);
        Task UpdateLastLoginAsync(string userId);
        Task<bool> BanUserAsync(string userId, bool isBanned);

        // User management methods
        Task<List<Account>> GetUsersWithEnrollmentsAsync(string instructorId, string? courseId = null, string? search = null, string? statusFilter = null, int page = 1, int pageSize = 10);
        Task<Account?> GetUserWithDetailsAsync(string userId);
        Task<bool> UpdateUserEnrollmentProgressAsync(string instructorId, string userId, string courseId, decimal progressPercentage, string? currentLessonId = null);
        Task<bool> UpdateUserEnrollmentStatusAsync(string instructorId, string userId, string courseId, int status);
        Task<bool> UnenrollUserFromCourseAsync(string instructorId, string userId, string courseId);
        Task<int> BulkUpdateUserStatusAsync(string instructorId, List<string> userIds, string courseId, int status);

        // Profile methods
        Task<bool> UpdateUserProfileAsync(Account user);
        Task<bool> UpdateUserPasswordAsync(string userId, string newPasswordHash);
        Task<bool> UpdateUserAvatarAsync(string userId, string avatarPath);

        // Notification methods
        Task<List<string>> GetUserIdsByRoleAsync(string role);
        Task<List<string>> GetAllActiveUserIdsAsync();

        // Home page methods
        Task<Account?> GetUserBasicInfoAsync(string userId);
        Task<Account?> GetUserWithPaymentPointAsync(string userId);        // Admin dashboard methods
        Task<List<Account>> GetRecentUsersAsync(int count = 5);

        // Admin user management methods
        Task<List<Account>> GetAllUsersAsync(string? search = null, string? roleFilter = null, int page = 1, int pageSize = 10);
        Task<int> GetUserCountAsync(string? search = null, string? roleFilter = null);
        Task<int> GetUserCountByRoleAsync(string role);
        Task<int> GetBannedUserCountAsync();
        Task<bool> UpdateUserBanStatusAsync(string userId, bool isBanned);
        Task<bool> DeleteUserAsync(string userId);

        // Chatbot context methods
        Task<Account?> GetUserWithEnrollmentsAndProgressAsync(string userId);
    }
}
