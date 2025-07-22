using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface ICourseService
    {
        Task<CourseListViewModel> GetCoursesAsync(string? search, string? category, int page, int pageSize);
        Task<CourseListViewModel> GetInstructorCoursesAsync(string authorId, string? search, string? category, int page, int pageSize); Task<CourseDetailViewModel?> GetCourseDetailAsync(string courseId);
        Task<CourseDetailViewModel?> GetCourseDetailAsync(string courseId, string? currentUserId = null); Task<List<CourseViewModel>> SearchCoursesAsync(string? search, string? category, int page, int pageSize, string? sortBy);
        Task<(List<CourseViewModel> courses, int totalCount)> SearchCoursesWithPaginationAsync(
            string? courseSearch,
            string? categorySearch,
            int page,
            int pageSize,
            string? sortBy,
            string? price = null,
            string? difficulty = null,
            string? duration = null,
            string? userRole = null,
            string? userId = null);
        Task<bool> EnrollUserAsync(string userId, string courseId);
        Task<bool> IsUserEnrolledAsync(string userId, string courseId);
        Task<List<CourseCategoryViewModel>> GetCategoriesAsync();
        Task<List<CategoryAutocompleteItem>> SearchCategoriesAsync(string searchTerm);
        Task<string> CreateCourseAsync(CreateCourseViewModel model, string authorId);
        Task<bool> UpdateCourseImageAsync(string courseId, string imagePath);
        Task<CreateCourseViewModel?> GetCourseForEditAsync(string courseId, string authorId);
        Task<bool> UpdateCourseAsync(string courseId, CreateCourseViewModel model, string authorId);
        Task<bool> DeleteCourseAsync(string courseId, string authorId);
        Task<bool> UpdateCourseApprovalStatusAsync(string courseId, string approvalStatus);
        Task<Course?> GetCourseByIdAsync(string courseId);
        Task<List<Chapter>> GetChaptersByCourseIdAsync(string courseId);
        Task<List<Lesson>> GetLessonsByChapterIdAsync(string chapterId);
        // Controller-facing methods
        Task<CourseIndexResult> GetCoursesAsync(System.Security.Claims.ClaimsPrincipal user, string? search, string? category, int page = 1, int pageSize = 50);
        Task<CourseSearchResult> SearchCoursesAsync(System.Security.Claims.ClaimsPrincipal user, string? courseSearch, string? categorySearch, int page = 1, int pageSize = 50, string? sortBy = "newest", string? price = null, string? difficulty = null, string? duration = null);
        Task<CourseDetailResult> GetCourseDetailsAsync(System.Security.Claims.ClaimsPrincipal user, string courseId, string? tab = null);
        Task<EnrollmentResult> EnrollInCourseAsync(System.Security.Claims.ClaimsPrincipal user, string courseId);
        Task<CreateCourseResult> GetCreateCourseViewModelAsync();
        Task<CreateCourseResult> CreateCourseAsync(System.Security.Claims.ClaimsPrincipal user, CreateCourseViewModel model);
        Task<EditCourseResult> GetCourseForEditAsync(System.Security.Claims.ClaimsPrincipal user, string courseId);
        Task<EditCourseResult> UpdateCourseAsync(System.Security.Claims.ClaimsPrincipal user, string courseId, CreateCourseViewModel model);
        Task<DeleteCourseResult> DeleteCourseAsync(System.Security.Claims.ClaimsPrincipal user, string courseId);
        Task<InstructorCoursesResult> GetUserCoursesAsync(System.Security.Claims.ClaimsPrincipal user);
        Task<CourseApprovalResult> RequestCourseApprovalAsync(System.Security.Claims.ClaimsPrincipal user, string courseId);
        Task<LearnManagementResult> GetLearnManagementDataAsync(System.Security.Claims.ClaimsPrincipal user, string courseId);
    }
}







