using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IFeedbackRepo
    {
        Task<bool> CreateFeedbackAsync(Feedback feedback);
        Task<bool> UpdateFeedbackAsync(Feedback feedback);
        Task<bool> DeleteFeedbackAsync(string feedbackId, string userId);
        Task<Feedback?> GetFeedbackByIdAsync(string feedbackId);
        Task<Feedback?> GetUserFeedbackForCourseAsync(string userId, string courseId);
        Task<List<Feedback>> GetCourseFeedbacksAsync(string courseId, int page = 1, int pageSize = 10);
        Task<bool> HasUserReviewedCourseAsync(string userId, string courseId);
        Task<bool> IsUserEnrolledInCourseAsync(string userId, string courseId);
        Task<int> GetCourseFeedbackCountAsync(string courseId);
        Task<double> GetCourseAverageRatingAsync(string courseId);
    }
}