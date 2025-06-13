using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface ICourseRepo : IBaseRepo<Course>
    {        // Course query methods
        IQueryable<Course> GetActiveCourses();
        Task<Course?> GetCourseDetailAsync(string courseId);
        Task<Course?> GetCourseDetailAsync(string courseId, string? currentUserId);
        Task<Course?> GetCourseWithChaptersAsync(string courseId);
        Task<Course?> GetCourseWithChaptersAsync(string courseId, string authorId);
        Task<Course?> GetCourseWithEnrollmentsAsync(string courseId);
        Task<List<Course>> SearchCoursesAsync(string? search, string? category, int page, int pageSize, string? sortBy);
        Task<Course?> GetCourseByIdAsync(string courseId);

        // Course management methods
        Task<string> CreateCourseAsync(Course course);
        Task<bool> UpdateCourseAsync(Course course);
        Task<bool> UpdateCourseImageAsync(string courseId, string imagePath);
        Task<bool> DeleteCourseAsync(string courseId, string authorId);
        Task<Course?> GetCourseForEditAsync(string courseId, string authorId);

        // Instructor-specific methods
        Task<List<Course>> GetInstructorCoursesAsync(string authorId, string? search, string? category, int page, int pageSize);
        Task<List<Course>> GetCoursesForFilterAsync(string instructorId);        // Enrollment methods
        Task<bool> EnrollUserAsync(string userId, string courseId);
        Task<bool> IsUserEnrolledAsync(string userId, string courseId);
        Task<List<Enrollment>> GetUserEnrollmentsAsync(string userId);
        Task<int> GetCourseEnrollmentCountAsync(string courseId);

        // Categories methods
        Task<List<CourseCategory>> GetCategoriesAsync();
        Task<List<CourseCategory>> SearchCategoriesAsync(string searchTerm);

        // Course notification methods
        Task<List<string>> GetEnrolledUserIdsAsync(string courseId);

        // Home page methods
        Task<List<Course>> GetFeaturedCoursesAsync(int count = 6);
        Task<List<Course>> GetRecommendedCoursesForUserAsync(string userId, List<string> excludeCourseIds, int count = 6);
        Task<List<CourseCategory>> GetCategoriesWithCourseCountAsync(int count = 8);

        // Admin dashboard methods
        Task<List<Course>> GetRecentCoursesAsync(int count = 5);        // Statistics methods
        Task<int> GetTotalStudentsForCourseAsync(string courseId);
        Task<decimal> GetAverageRatingForCourseAsync(string courseId);
        Task<int> GetTotalReviewsForCourseAsync(string courseId);        // Admin course management methods
        Task<List<Course>> GetAllCoursesAsync(string? search = null, string? categoryFilter = null, int page = 1, int pageSize = 10);
        Task<int> GetCourseCountAsync(string? search = null, string? categoryFilter = null);
        Task<int> GetApprovedCourseCountAsync();
        Task<int> GetPendingCourseCountAsync();
        Task<int> GetRejectedCourseCountAsync();
        Task<int> GetFreeCourseCountAsync();
        Task<int> GetPaidCourseCountAsync();
        Task<bool> UpdateCourseApprovalAsync(string courseId, bool isApproved);
        Task<bool> DeleteCourseAsync(string courseId);
    }
}
