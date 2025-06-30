using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    // Course view models have been moved to HomePageGuestViewModel.cs to avoid duplication
    // This file is kept for future course-specific view models if needed

    public class CreateReviewViewModel
    {
        [Required(ErrorMessage = "Course ID is required")]
        public string CourseId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int StarRating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string? Comment { get; set; }
    }

    public class ReviewListViewModel
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public List<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool CanCreateReview { get; set; }
        public bool HasUserReviewed { get; set; }
        public string? UserExistingReviewId { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class EditReviewViewModel
    {
        public string ReviewId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int StarRating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string? Comment { get; set; }
    }
}
