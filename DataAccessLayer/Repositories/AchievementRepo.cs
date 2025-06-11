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
    public class AchievementRepo : BaseRepo<Achievement>, IAchievementRepo
    {
        private readonly ILogger<AchievementRepo>? _logger;

        public AchievementRepo(BrainStormEraContext context, ILogger<AchievementRepo>? logger = null)
            : base(context)
        {
            _logger = logger;
        }

        // Achievement query methods
        public async Task<List<Achievement>> GetAllAchievementsAsync()
        {
            try
            {
                return await _dbSet
                    .OrderBy(a => a.AchievementType)
                    .ThenBy(a => a.PointsReward)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all achievements");
                throw;
            }
        }

        public async Task<List<Achievement>> GetAchievementsByTypeAsync(string achievementType)
        {
            try
            {
                return await _dbSet
                    .Where(a => a.AchievementType == achievementType)
                    .OrderBy(a => a.PointsReward)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting achievements by type: {Type}", achievementType);
                throw;
            }
        }

        public async Task<Achievement?> GetAchievementWithUsersAsync(string achievementId)
        {
            try
            {
                return await _dbSet
                    .Include(a => a.UserAchievements)
                        .ThenInclude(ua => ua.User)
                    .FirstOrDefaultAsync(a => a.AchievementId == achievementId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting achievement with users: {AchievementId}", achievementId);
                throw;
            }
        }

        public async Task<List<Achievement>> GetAchievementsByPointsRangeAsync(int minPoints, int maxPoints)
        {
            try
            {
                return await _dbSet
                    .Where(a => a.PointsReward >= minPoints && a.PointsReward <= maxPoints)
                    .OrderBy(a => a.PointsReward)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting achievements by points range: {Min}-{Max}", minPoints, maxPoints);
                throw;
            }
        }

        // User achievement methods
        public async Task<List<UserAchievement>> GetUserAchievementsAsync(string userId)
        {
            try
            {
                return await _context.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.UserId == userId)
                    .OrderByDescending(ua => ua.ReceivedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user achievements: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Achievement>> GetUnlockedAchievementsAsync(string userId)
        {
            try
            {
                var userAchievements = await _context.UserAchievements
                    .Where(ua => ua.UserId == userId)
                    .Select(ua => ua.AchievementId)
                    .ToListAsync();

                return await _dbSet
                    .Where(a => userAchievements.Contains(a.AchievementId))
                    .OrderBy(a => a.AchievementType)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting unlocked achievements: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Achievement>> GetLockedAchievementsAsync(string userId)
        {
            try
            {
                var userAchievements = await _context.UserAchievements
                    .Where(ua => ua.UserId == userId)
                    .Select(ua => ua.AchievementId)
                    .ToListAsync();

                return await _dbSet
                    .Where(a => !userAchievements.Contains(a.AchievementId))
                    .OrderBy(a => a.AchievementType)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting locked achievements: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserAchievement?> GetUserAchievementAsync(string userId, string achievementId)
        {
            try
            {
                return await _context.UserAchievements
                    .Include(ua => ua.Achievement)
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user achievement: {UserId}, {AchievementId}", userId, achievementId);
                throw;
            }
        }

        // Achievement management methods
        public async Task<string> CreateAchievementAsync(Achievement achievement)
        {
            try
            {
                await AddAsync(achievement);
                await SaveChangesAsync();
                return achievement.AchievementId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating achievement");
                throw;
            }
        }

        public async Task<bool> UpdateAchievementAsync(Achievement achievement)
        {
            try
            {
                await UpdateAsync(achievement);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating achievement");
                throw;
            }
        }

        public async Task<bool> DeleteAchievementAsync(string achievementId)
        {
            try
            {
                var achievement = await GetByIdAsync(achievementId);
                if (achievement == null)
                    return false;

                await DeleteAsync(achievement);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting achievement");
                throw;
            }
        }

        public async Task<bool> UpdateAchievementIconAsync(string achievementId, string iconPath)
        {
            try
            {
                var achievement = await GetByIdAsync(achievementId);
                if (achievement == null)
                    return false;

                achievement.AchievementIcon = iconPath;
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating achievement icon");
                throw;
            }
        }

        // Achievement unlocking methods
        public async Task<bool> UnlockAchievementAsync(string userId, string achievementId)
        {
            try
            {
                var existingUserAchievement = await GetUserAchievementAsync(userId, achievementId);
                if (existingUserAchievement != null)
                    return false; // Already unlocked

                var userAchievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementId = achievementId,
                    ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                _context.UserAchievements.Add(userAchievement);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error unlocking achievement");
                throw;
            }
        }

        public async Task<bool> CheckAndUnlockAchievementsAsync(string userId)
        {
            // This would contain logic to check various conditions and unlock achievements
            // For now, returning a basic implementation
            return true;
        }

        public async Task<List<Achievement>> GetNewlyUnlockedAchievementsAsync(string userId)
        {
            try
            {
                var recentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-1)); // Last hour
                var newlyUnlocked = await _context.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.UserId == userId && ua.ReceivedDate >= recentDate)
                    .Select(ua => ua.Achievement)
                    .ToListAsync();

                return newlyUnlocked;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting newly unlocked achievements");
                throw;
            }
        }

        public async Task<bool> IsAchievementUnlockedAsync(string userId, string achievementId)
        {
            try
            {
                return await _context.UserAchievements
                    .AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if achievement is unlocked");
                throw;
            }
        }

        // Achievement statistics
        public async Task<int> GetUserAchievementCountAsync(string userId)
        {
            try
            {
                return await _context.UserAchievements
                    .CountAsync(ua => ua.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user achievement count");
                throw;
            }
        }

        public async Task<int> GetTotalPointsEarnedAsync(string userId)
        {
            try
            {
                return await _context.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.UserId == userId)
                    .SumAsync(ua => ua.Achievement.PointsReward ?? 0);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting total points earned");
                throw;
            }
        }

        public async Task<decimal> GetAchievementCompletionPercentageAsync(string userId)
        {
            try
            {
                var totalAchievements = await CountAsync();
                if (totalAchievements == 0) return 0;

                var userAchievements = await GetUserAchievementCountAsync(userId);
                return Math.Round((decimal)userAchievements / totalAchievements * 100, 2);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting achievement completion percentage");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetAchievementTypeStatsAsync(string userId)
        {
            try
            {
                return await _context.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.UserId == userId)
                    .GroupBy(ua => ua.Achievement.AchievementType)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting achievement type stats");
                throw;
            }
        }

        // Basic implementations for remaining interface methods
        public async Task<List<(string userId, string userName, int points, int achievementCount)>> GetAchievementLeaderboardAsync(int top = 10)
        {
            try
            {
                return await _context.UserAchievements
                    .Include(ua => ua.User)
                    .Include(ua => ua.Achievement)
                    .GroupBy(ua => new { ua.UserId, ua.User.Username })
                    .Select(g => new
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.Username,
                        Points = g.Sum(ua => ua.Achievement.PointsReward ?? 0),
                        AchievementCount = g.Count()
                    })
                    .OrderByDescending(x => x.Points)
                    .Take(top)
                    .Select(x => ValueTuple.Create(x.UserId, x.UserName, x.Points, x.AchievementCount))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting achievement leaderboard");
                throw;
            }
        }

        public async Task<int> GetUserRankingAsync(string userId) => 1; // Basic implementation
        public async Task<List<(string userId, string userName, int points)>> GetTopUsersByPointsAsync(int top = 10) => new(); // Basic implementation
        public async Task<bool> ValidateAchievementRequirementsAsync(string userId, string achievementId) => true;
        public async Task<List<Achievement>> GetEligibleAchievementsAsync(string userId) => new();
        public async Task<bool> HasPrerequisiteAchievementsAsync(string userId, string achievementId) => true;

        // Achievement categories and types
        public async Task<List<string>> GetAchievementTypesAsync() =>
            await _dbSet.Select(a => a.AchievementType).Distinct().ToListAsync();

        public async Task<List<Achievement>> GetCourseCompletionAchievementsAsync() =>
            await GetAchievementsByTypeAsync("course_completion");

        public async Task<List<Achievement>> GetQuizMasteryAchievementsAsync() =>
            await GetAchievementsByTypeAsync("quiz_mastery");

        public async Task<List<Achievement>> GetStreakAchievementsAsync() =>
            await GetAchievementsByTypeAsync("streak");

        public async Task<List<Achievement>> GetSocialAchievementsAsync() =>
            await GetAchievementsByTypeAsync("social");

        // Achievement progress tracking - basic implementations
        public async Task<bool> UpdateAchievementProgressAsync(string userId, string achievementType, int progress) => true;
        public async Task<Dictionary<string, int>> GetAchievementProgressAsync(string userId) => new();
        public async Task<bool> IncrementAchievementProgressAsync(string userId, string achievementType, int increment = 1) => true;

        // Missing methods implementation
        public async Task<List<UserAchievement>> GetUserAchievementsAsync(string userId, string? search, int page, int pageSize)
        {
            try
            {
                var query = _context.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.UserId == userId);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(ua => ua.Achievement.AchievementName.Contains(search) ||
                                             ua.Achievement.AchievementDescription!.Contains(search));
                }

                return await query
                    .OrderByDescending(ua => ua.ReceivedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user achievements with pagination");
                throw;
            }
        }

        public async Task<int> GetUserAchievementsCountAsync(string userId, string? search)
        {
            try
            {
                var query = _context.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.UserId == userId);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(ua => ua.Achievement.AchievementName.Contains(search) ||
                                             ua.Achievement.AchievementDescription!.Contains(search));
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user achievements count");
                throw;
            }
        }

        public async Task<string?> GetCourseNameAsync(string courseId)
        {
            try
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);
                return course?.CourseName;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting course name");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetUserCompletedCoursesCountAsync()
        {
            try
            {
                return await _context.Enrollments
                    .Where(e => e.CertificateIssuedDate.HasValue)
                    .GroupBy(e => e.UserId)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user completed courses count");
                throw;
            }
        }

        public async Task<bool> HasUserAchievementAsync(string userId, string achievementId)
        {
            try
            {
                return await _context.UserAchievements
                    .AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user has achievement");
                throw;
            }
        }

        public async Task<bool> AddUserAchievementAsync(UserAchievement userAchievement)
        {
            try
            {
                _context.UserAchievements.Add(userAchievement);
                var result = await SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding user achievement");
                throw;
            }
        }
    }
}
