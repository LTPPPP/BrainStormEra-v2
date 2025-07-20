using DataAccessLayer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessLogicLayer.Services.Interfaces;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Implementations
{
    public class RecommendationHelper : IRecommendationHelper
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<RecommendationHelper> _logger;

        public RecommendationHelper(BrainStormEraContext context, ILogger<RecommendationHelper> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> EnsureFeaturedCoursesExistAsync()
        {
            try
            {
                var featuredCount = await _context.Courses
                    .CountAsync(c => c.IsFeatured == true && c.CourseStatus == 1);

                if (featuredCount > 0)
                {
                    _logger.LogInformation("Found {Count} featured courses", featuredCount);
                    return true;
                }

                var coursesToFeature = await _context.Courses
                    .Where(c => c.CourseStatus == 1 && (c.IsFeatured != true || c.IsFeatured == null))
                    .Include(c => c.Enrollments)
                    .OrderByDescending(c => c.Enrollments.Count())
                    .ThenByDescending(c => c.CourseCreatedAt)
                    .Take(3)
                    .ToListAsync();

                if (coursesToFeature.Any())
                {
                    foreach (var course in coursesToFeature)
                    {
                        course.IsFeatured = true;
                        course.CourseUpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Automatically featured {Count} courses for recommendations", coursesToFeature.Count);
                    return true;
                }

                _logger.LogWarning("No courses available to feature for recommendations");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring featured courses exist");
                return false;
            }
        }

        public async Task<RecommendationStats> GetRecommendationStatsAsync()
        {
            try
            {
                var totalCourses = await _context.Courses.CountAsync(c => c.CourseStatus == 1);
                var featuredCourses = await _context.Courses.CountAsync(c => c.IsFeatured == true && c.CourseStatus == 1);
                var coursesWithEnrollments = await _context.Courses.CountAsync(c => c.CourseStatus == 1 && c.Enrollments.Any());

                return new RecommendationStats
                {
                    TotalActiveCourses = totalCourses,
                    FeaturedCourses = featuredCourses,
                    CoursesWithEnrollments = coursesWithEnrollments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendation stats");
                return new RecommendationStats();
            }
        }
    }
}