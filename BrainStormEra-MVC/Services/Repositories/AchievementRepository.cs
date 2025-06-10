using DataAccessLayer.Data;
using DataAccessLayer.Models;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BrainStormEra_MVC.Services.Repositories
{
    public class AchievementRepository : IAchievementRepository
    {
        private readonly BrainStormEraContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AchievementRepository> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

        public AchievementRepository(BrainStormEraContext context, IMemoryCache cache, ILogger<AchievementRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Achievement>> GetAllAchievementsAsync()
        {
            const string cacheKey = "AllAchievements";

            if (_cache.TryGetValue(cacheKey, out List<Achievement>? cached))
                return cached!;

            var achievements = await _context.Achievements
                .AsNoTracking()
                .ToListAsync();

            _cache.Set(cacheKey, achievements, CacheExpiration);
            return achievements;
        }

        public async Task<Achievement?> GetAchievementByIdAsync(string achievementId)
        {
            var cacheKey = $"Achievement_{achievementId}";

            if (_cache.TryGetValue(cacheKey, out Achievement? cached))
                return cached;

            var achievement = await _context.Achievements
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AchievementId == achievementId);

            if (achievement != null)
                _cache.Set(cacheKey, achievement, CacheExpiration);

            return achievement;
        }

        public async Task<List<UserAchievement>> GetUserAchievementsAsync(string userId)
        {
            var cacheKey = $"UserAchievements_{userId}";

            if (_cache.TryGetValue(cacheKey, out List<UserAchievement>? cached))
                return cached!;

            var userAchievements = await _context.UserAchievements
                .AsNoTracking()
                .Where(ua => ua.UserId == userId)
                .Include(ua => ua.Achievement)
                .ToListAsync();

            _cache.Set(cacheKey, userAchievements, TimeSpan.FromMinutes(5));
            return userAchievements;
        }

        public async Task<UserAchievement?> GetUserAchievementAsync(string userId, string achievementId)
        {
            return await _context.UserAchievements
                .AsNoTracking()
                .Include(ua => ua.Achievement)
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);
        }

        public async Task<bool> HasUserAchievementAsync(string userId, string achievementId)
        {
            return await _context.UserAchievements
                .AsNoTracking()
                .AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);
        }

        public async Task AddUserAchievementAsync(UserAchievement userAchievement)
        {
            _context.UserAchievements.Add(userAchievement);
            await _context.SaveChangesAsync();

            _cache.Remove($"UserAchievements_{userAchievement.UserId}");
        }

        public async Task<Dictionary<string, int>> GetUserCompletedCoursesCountAsync()
        {
            const string cacheKey = "UserCompletedCoursesCount";

            if (_cache.TryGetValue(cacheKey, out Dictionary<string, int>? cached))
                return cached!;

            var userCompletedCourses = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.EnrollmentStatus == 5)
                .GroupBy(e => e.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            _cache.Set(cacheKey, userCompletedCourses, TimeSpan.FromMinutes(5));
            return userCompletedCourses;
        }

        public async Task<string?> GetCourseNameAsync(string courseId)
        {
            var cacheKey = $"CourseName_{courseId}";

            if (_cache.TryGetValue(cacheKey, out string? cached))
                return cached;

            var courseName = await _context.Courses
                .AsNoTracking()
                .Where(c => c.CourseId == courseId)
                .Select(c => c.CourseName)
                .FirstOrDefaultAsync();

            if (courseName != null)
                _cache.Set(cacheKey, courseName, CacheExpiration);

            return courseName;
        }

        public async Task<List<UserAchievement>> GetUserAchievementsAsync(string userId, string? search, int page, int pageSize)
        {
            var cacheKey = $"UserAchievementsPaginated_{userId}_{search}_{page}_{pageSize}"; if (_cache.TryGetValue(cacheKey, out List<UserAchievement>? cached))
                return cached!;

            IQueryable<UserAchievement> query = _context.UserAchievements
                .AsNoTracking()
                .Where(ua => ua.UserId == userId)
                .Include(ua => ua.Achievement);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(ua =>
                    ua.Achievement.AchievementName.Contains(search) ||
                    ua.Achievement.AchievementDescription!.Contains(search) ||
                    ua.Achievement.AchievementType!.Contains(search));
            }

            var userAchievements = await query
                .OrderByDescending(ua => ua.ReceivedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _cache.Set(cacheKey, userAchievements, TimeSpan.FromMinutes(5));
            return userAchievements;
        }

        public async Task<int> GetUserAchievementsCountAsync(string userId, string? search)
        {
            var cacheKey = $"UserAchievementsCount_{userId}_{search}";

            if (_cache.TryGetValue(cacheKey, out int cached))
                return cached;

            IQueryable<UserAchievement> query = _context.UserAchievements
                .AsNoTracking()
                .Where(ua => ua.UserId == userId)
                .Include(ua => ua.Achievement);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(ua =>
                    ua.Achievement.AchievementName.Contains(search) ||
                    ua.Achievement.AchievementDescription!.Contains(search) ||
                    ua.Achievement.AchievementType!.Contains(search));
            }

            var count = await query.CountAsync();

            _cache.Set(cacheKey, count, TimeSpan.FromMinutes(5));
            return count;
        }
    }
}
