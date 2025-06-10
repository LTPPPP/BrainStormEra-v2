using DataAccessLayer.Data;
using DataAccessLayer.Models;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BrainStormEra_MVC.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly BrainStormEraContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EnrollmentService> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

        public EnrollmentService(BrainStormEraContext context, IMemoryCache cache, ILogger<EnrollmentService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> EnrollAsync(string userId, string courseId)
        {
            try
            {
                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (existingEnrollment != null)
                {
                    return false;
                }

                var enrollment = new Enrollment
                {
                    EnrollmentId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    CourseId = courseId,
                    EnrollmentCreatedAt = DateTime.UtcNow,
                    EnrollmentUpdatedAt = DateTime.UtcNow,
                    EnrollmentStatus = 1,
                    ProgressPercentage = 0
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                _cache.Remove($"UserEnrollments_{userId}");
                _cache.Remove($"CourseEnrollmentCount_{courseId}");
                _cache.Remove($"IsEnrolled_{userId}_{courseId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling user {UserId} in course {CourseId}", userId, courseId);
                return false;
            }
        }

        public async Task<bool> IsEnrolledAsync(string userId, string courseId)
        {
            var cacheKey = $"IsEnrolled_{userId}_{courseId}";

            if (_cache.TryGetValue(cacheKey, out bool isEnrolled))
            {
                return isEnrolled;
            }

            isEnrolled = await _context.Enrollments
                .AsNoTracking()
                .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);

            _cache.Set(cacheKey, isEnrolled, CacheExpiration);
            return isEnrolled;
        }

        public async Task<List<Enrollment>> GetUserEnrollmentsAsync(string userId)
        {
            var cacheKey = $"UserEnrollments_{userId}";

            if (_cache.TryGetValue(cacheKey, out List<Enrollment>? cachedEnrollments))
            {
                return cachedEnrollments!;
            }

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Author)
                .ToListAsync();

            _cache.Set(cacheKey, enrollments, TimeSpan.FromMinutes(10));
            return enrollments;
        }

        public async Task<int> GetCourseEnrollmentCountAsync(string courseId)
        {
            var cacheKey = $"CourseEnrollmentCount_{courseId}";

            if (_cache.TryGetValue(cacheKey, out int count))
            {
                return count;
            }

            count = await _context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.CourseId == courseId);

            _cache.Set(cacheKey, count, CacheExpiration);
            return count;
        }
    }
}
