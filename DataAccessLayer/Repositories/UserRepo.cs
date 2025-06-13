using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataAccessLayer.Repositories
{
    public class UserRepo : BaseRepo<Account>, IUserRepo
    {
        private readonly ILogger<UserRepo>? _logger;

        public UserRepo(BrainStormEraContext context, ILogger<UserRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // User-specific methods
        public async Task<Account?> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _dbSet
                    .FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user by username: {Username}", username);
                throw;
            }
        }

        public async Task<Account?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _dbSet
                    .FirstOrDefaultAsync(u => u.UserEmail == email);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user by email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                return await _dbSet.AnyAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking username existence: {Username}", username);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                return await _dbSet.AnyAsync(u => u.UserEmail == email);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking email existence: {Email}", email);
                throw;
            }
        }

        public async Task<bool> CreateUserAsync(Account user)
        {
            try
            {
                await AddAsync(user);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(Account user)
        {
            try
            {
                await UpdateAsync(user);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user");
                throw;
            }
        }

        public async Task UpdateLastLoginAsync(string userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user != null)
                {
                    user.LastLogin = DateTime.UtcNow;
                    await SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating last login for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> BanUserAsync(string userId, bool isBanned)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsBanned = isBanned;
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating ban status for user: {UserId}", userId);
                throw;
            }
        }

        // User management methods
        public async Task<List<Account>> GetUsersWithEnrollmentsAsync(string instructorId, string? courseId = null, string? search = null, string? statusFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
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
                    .Select(e => e.User)
                    .ToListAsync();

                return enrollments;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting users with enrollments for instructor: {InstructorId}", instructorId);
                throw;
            }
        }

        public async Task<Account?> GetUserWithDetailsAsync(string userId)
        {
            try
            {
                return await _dbSet
                    .Include(u => u.Enrollments)
                        .ThenInclude(e => e.Course)
                    .Include(u => u.UserAchievements)
                        .ThenInclude(ua => ua.Achievement)
                    .FirstOrDefaultAsync(u => u.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user details: {UserId}", userId);
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
                if (!string.IsNullOrEmpty(currentLessonId))
                {
                    enrollment.CurrentLessonId = currentLessonId;
                }
                enrollment.EnrollmentUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user enrollment progress");
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
                enrollment.EnrollmentUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user enrollment status");
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
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error unenrolling user from course");
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
                    enrollment.EnrollmentUpdatedAt = DateTime.UtcNow;
                }

                var result = await SaveChangesAsync();
                return enrollments.Count;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error bulk updating user status");
                throw;
            }
        }

        // Profile methods
        public async Task<bool> UpdateUserProfileAsync(Account user)
        {
            try
            {
                var existingUser = await GetByIdAsync(user.UserId);
                if (existingUser == null)
                    return false;

                // Update only profile-related fields
                existingUser.FullName = user.FullName;
                existingUser.UserEmail = user.UserEmail;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.DateOfBirth = user.DateOfBirth;
                existingUser.Gender = user.Gender;
                existingUser.UserAddress = user.UserAddress;
                existingUser.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user profile");
                throw;
            }
        }

        public async Task<bool> UpdateUserPasswordAsync(string userId, string newPasswordHash)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.PasswordHash = newPasswordHash;
                user.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user password");
                throw;
            }
        }
        public async Task<bool> UpdateUserAvatarAsync(string userId, string avatarPath)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.UserImage = avatarPath;
                user.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user avatar");
                throw;
            }
        }

        // Notification methods
        public async Task<List<string>> GetUserIdsByRoleAsync(string role)
        {
            try
            {
                return await _dbSet
                    .Where(a => a.UserRole == role)
                    .Select(a => a.UserId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user IDs by role: {Role}", role);
                throw;
            }
        }
        public async Task<List<string>> GetAllActiveUserIdsAsync()
        {
            try
            {
                return await _dbSet
                    .Where(a => a.IsBanned != true)
                    .Select(a => a.UserId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all active user IDs");
                throw;
            }
        }

        public async Task<Account?> GetUserBasicInfoAsync(string userId)
        {
            try
            {
                return await _dbSet
                    .Where(a => a.UserId == userId)
                    .Select(a => new Account
                    {
                        UserId = a.UserId,
                        Username = a.Username,
                        FullName = a.FullName,
                        UserImage = a.UserImage
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user basic info for user {UserId}", userId);
                throw;
            }
        }
        public async Task<Account?> GetUserWithPaymentPointAsync(string userId)
        {
            try
            {
                return await _dbSet
                    .Where(a => a.UserId == userId)
                    .Select(a => new Account
                    {
                        UserId = a.UserId,
                        Username = a.Username,
                        FullName = a.FullName,
                        UserImage = a.UserImage,
                        PaymentPoint = a.PaymentPoint
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user with payment point for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Account>> GetRecentUsersAsync(int count = 5)
        {
            try
            {
                return await _dbSet.OrderByDescending(a => a.AccountCreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting recent users");
                throw;
            }
        }
        public async Task<Account?> GetUserWithEnrollmentsAndProgressAsync(string userId)
        {
            try
            {
                return await _dbSet
                    .Include(a => a.Enrollments)
                        .ThenInclude(e => e.Course)
                    .Include(a => a.UserProgresses)
                        .ThenInclude(up => up.Lesson)
                    .FirstOrDefaultAsync(a => a.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user context for user {UserId}", userId);
                throw;
            }
        }

        // Admin user management methods
        public async Task<List<Account>> GetAllUsersAsync(string? search = null, string? roleFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                IQueryable<Account> query = _dbSet;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(a => a.Username.Contains(search) ||
                                           a.UserEmail.Contains(search) ||
                                           (a.FullName != null && a.FullName.Contains(search)));
                }

                if (!string.IsNullOrWhiteSpace(roleFilter))
                {
                    query = query.Where(a => a.UserRole == roleFilter);
                }

                return await query
                    .OrderByDescending(a => a.AccountCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all users for admin");
                throw;
            }
        }

        public async Task<int> GetUserCountAsync(string? search = null, string? roleFilter = null)
        {
            try
            {
                IQueryable<Account> query = _dbSet;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(a => a.Username.Contains(search) ||
                                           a.UserEmail.Contains(search) ||
                                           (a.FullName != null && a.FullName.Contains(search)));
                }

                if (!string.IsNullOrWhiteSpace(roleFilter))
                {
                    query = query.Where(a => a.UserRole == roleFilter);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user count");
                throw;
            }
        }

        public async Task<int> GetUserCountByRoleAsync(string role)
        {
            try
            {
                return await _dbSet.CountAsync(a => a.UserRole == role);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user count by role: {Role}", role);
                throw;
            }
        }

        public async Task<int> GetBannedUserCountAsync()
        {
            try
            {
                return await _dbSet.CountAsync(a => a.IsBanned == true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting banned user count");
                throw;
            }
        }

        public async Task<bool> UpdateUserBanStatusAsync(string userId, bool isBanned)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsBanned = isBanned;
                user.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user ban status");
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                // Soft delete by banning the user and marking as deleted
                user.IsBanned = true;
                user.AccountUpdatedAt = DateTime.UtcNow;

                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting user");
                throw;
            }
        }
    }
}
