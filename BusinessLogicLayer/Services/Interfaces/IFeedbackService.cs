using BusinessLogicLayer.DTOs.Course;
using System.Security.Claims;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IFeedbackService
    {
        Task<CreateReviewResponse> CreateReviewAsync(ClaimsPrincipal user, CreateReviewRequest request);
        Task<UpdateReviewResponse> UpdateReviewAsync(ClaimsPrincipal user, UpdateReviewRequest request);
        Task<DeleteReviewResponse> DeleteReviewAsync(ClaimsPrincipal user, string reviewId);
        Task<GetReviewsResponse> GetCourseReviewsAsync(string courseId, int page = 1, int pageSize = 10);
        Task<CheckReviewEligibilityResponse> CheckReviewEligibilityAsync(ClaimsPrincipal user, string courseId);
    }
}