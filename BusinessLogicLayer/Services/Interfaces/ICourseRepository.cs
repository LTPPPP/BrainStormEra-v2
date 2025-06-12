using DataAccessLayer.Data;
using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface ICourseRepository
    {
        IQueryable<Course> GetActiveCourses();
        Task<Course?> GetCourseByIdAsync(string courseId);
        Task<Course?> GetCourseDetailAsync(string courseId);
        Task<Course?> GetCourseDetailAsync(string courseId, string? currentUserId = null);
        Task<List<Course>> GetFeaturedCoursesAsync(int take = 4);
        Task<bool> CourseExistsAsync(string courseId);
        Task<int> GetTotalCoursesCountAsync(string? search, string? category);
    }
}







