using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface ICourseService
    {
        Task<CourseListViewModel> GetCoursesAsync(string? search, string? category, int page, int pageSize);
        Task<CourseListViewModel> GetInstructorCoursesAsync(string authorId, string? search, string? category, int page, int pageSize); Task<CourseDetailViewModel?> GetCourseDetailAsync(string courseId);
        Task<CourseDetailViewModel?> GetCourseDetailAsync(string courseId, string? currentUserId = null); Task<List<CourseViewModel>> SearchCoursesAsync(string? search, string? category, int page, int pageSize, string? sortBy);
        Task<(List<CourseViewModel> courses, int totalCount)> SearchCoursesWithPaginationAsync(
            string? search,
            string? category,
            int page,
            int pageSize,
            string? sortBy,
            string? price = null,
            string? difficulty = null,
            string? duration = null);
        Task<bool> EnrollUserAsync(string userId, string courseId);
        Task<bool> IsUserEnrolledAsync(string userId, string courseId);
        Task<List<CourseCategoryViewModel>> GetCategoriesAsync();
        Task<List<CategoryAutocompleteItem>> SearchCategoriesAsync(string searchTerm);
        Task<string> CreateCourseAsync(CreateCourseViewModel model, string authorId);
        Task<bool> UpdateCourseImageAsync(string courseId, string imagePath);
        Task<CreateCourseViewModel?> GetCourseForEditAsync(string courseId, string authorId);
        Task<bool> UpdateCourseAsync(string courseId, CreateCourseViewModel model, string authorId);
        Task<bool> DeleteCourseAsync(string courseId, string authorId);
    }
}







