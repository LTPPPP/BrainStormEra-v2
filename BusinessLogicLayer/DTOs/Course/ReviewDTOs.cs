using BusinessLogicLayer.DTOs.Common;
using DataAccessLayer.Models.ViewModels;

namespace BusinessLogicLayer.DTOs.Course
{
    public class CreateReviewRequest
    {
        public string CourseId { get; set; } = string.Empty;
        public int StarRating { get; set; }
        public string? Comment { get; set; }
    }

    public class CreateReviewResponse : ServiceResult
    {
        public string? ReviewId { get; set; }
        public new bool Success => base.IsSuccess;
    }

    public class UpdateReviewRequest
    {
        public string ReviewId { get; set; } = string.Empty;
        public int StarRating { get; set; }
        public string? Comment { get; set; }
    }

    public class UpdateReviewResponse : ServiceResult
    {
        public new bool Success => base.IsSuccess;
    }

    public class DeleteReviewRequest
    {
        public string ReviewId { get; set; } = string.Empty;
    }

    public class DeleteReviewResponse : ServiceResult
    {
        public new bool Success => base.IsSuccess;
    }

    public class GetReviewsRequest
    {
        public string CourseId { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetReviewsResponse : ServiceResult
    {
        public ReviewListViewModel? ViewModel { get; set; }
        public new bool Success => base.IsSuccess;
    }

    public class CheckReviewEligibilityResponse : ServiceResult
    {
        public bool CanCreateReview { get; set; }
        public bool IsEnrolled { get; set; }
        public bool HasExistingReview { get; set; }
        public string? ExistingReviewId { get; set; }
    }
}