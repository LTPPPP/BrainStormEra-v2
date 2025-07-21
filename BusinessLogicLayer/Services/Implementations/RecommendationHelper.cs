using DataAccessLayer.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using BusinessLogicLayer.Services.Interfaces;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Implementations
{
    public class RecommendationHelper : IRecommendationHelper
    {
        private readonly ICourseRepo _courseRepo;
        private readonly ILogger<RecommendationHelper> _logger;

        public RecommendationHelper(ICourseRepo courseRepo, ILogger<RecommendationHelper> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<bool> EnsureFeaturedCoursesExistAsync()
        {
            try
            {
                // Get featured courses count - this would need to be implemented in the repository
                var featuredCourses = await _courseRepo.GetFeaturedCoursesAsync(10);
                var featuredCount = featuredCourses.Count;

                if (featuredCount > 0)
                {
                    _logger.LogInformation("Found {Count} featured courses", featuredCount);
                    return true;
                }

                // Get courses to feature - this would need to be implemented in the repository
                // For now, we'll return false as the repository method doesn't exist yet
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
                // Get course statistics - these would need to be implemented in the repository
                var totalCourses = await _courseRepo.GetApprovedCourseCountAsync();
                var featuredCourses = 0; // This would need a repository method
                var coursesWithEnrollments = 0; // This would need a repository method

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