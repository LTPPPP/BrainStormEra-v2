namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IRecommendationHelper
    {
        Task<bool> EnsureFeaturedCoursesExistAsync();
        Task<RecommendationStats> GetRecommendationStatsAsync();
    }

    public class RecommendationStats
    {
        public int TotalActiveCourses { get; set; }
        public int FeaturedCourses { get; set; }
        public int CoursesWithEnrollments { get; set; }
    }
}