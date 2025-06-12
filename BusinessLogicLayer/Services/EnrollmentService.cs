using Microsoft.Extensions.Logging;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BusinessLogicLayer.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly ICourseRepo _courseRepo;
        private readonly IUserRepo _userRepo;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EnrollmentService> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

        public EnrollmentService(
            ICourseRepo courseRepo,
            IUserRepo userRepo,
            IMemoryCache cache,
            ILogger<EnrollmentService> logger)
        {
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> EnrollAsync(string userId, string courseId)
        {
            try
            {
                var result = await _courseRepo.EnrollUserAsync(userId, courseId);

                if (result)
                {
                    _cache.Remove($"UserEnrollments_{userId}");
                    _cache.Remove($"CourseEnrollmentCount_{courseId}");
                    _cache.Remove($"IsEnrolled_{userId}_{courseId}");
                }

                return result;
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

            isEnrolled = await _courseRepo.IsUserEnrolledAsync(userId, courseId);

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

            var enrollments = await _courseRepo.GetUserEnrollmentsAsync(userId);

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

            count = await _courseRepo.GetCourseEnrollmentCountAsync(courseId);

            _cache.Set(cacheKey, count, CacheExpiration);
            return count;
        }
    }
}








