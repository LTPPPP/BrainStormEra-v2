using DataAccessLayer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Helper service for managing course recommendations
    /// </summary>
    public class RecommendationHelper
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<RecommendationHelper> _logger;

        public RecommendationHelper(BrainStormEraContext context, ILogger<RecommendationHelper> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Ensures there are featured courses available for recommendations
        /// This method can be called during application startup or as needed
        /// </summary>
        public async Task<bool> EnsureFeaturedCoursesExistAsync()
        {
            try
            {
                // Check if there are any featured courses
                var featuredCount = await _context.Courses
                    .CountAsync(c => c.IsFeatured == true && c.CourseStatus == 1);

                if (featuredCount > 0)
                {
                    _logger.LogInformation("Found {Count} featured courses", featuredCount);
                    return true;
                }

                // If no featured courses, mark some popular courses as featured
                var coursesToFeature = await _context.Courses
                    .Where(c => c.CourseStatus == 1 && (c.IsFeatured != true || c.IsFeatured == null))
                    .Include(c => c.Enrollments)
                    .OrderByDescending(c => c.Enrollments.Count())
                    .ThenByDescending(c => c.CourseCreatedAt)
                    .Take(3) // Feature top 3 courses
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

        /// <summary>
        /// Gets statistics about recommendation data
        /// </summary>
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

    /// <summary>
    /// Statistics about recommendation system
    /// </summary>
    public class RecommendationStats
    {
        public int TotalActiveCourses { get; set; }
        public int FeaturedCourses { get; set; }
        public int CoursesWithEnrollments { get; set; }
    }
}
