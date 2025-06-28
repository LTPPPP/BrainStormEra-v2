using BusinessLogicLayer.Constants;
using BusinessLogicLayer.DTOs.Course;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BusinessLogicLayer.Services.Implementations
{
    public class FeedbackServiceImpl : IFeedbackService
    {
        private readonly IFeedbackRepo _feedbackRepo;
        private readonly ILogger<FeedbackServiceImpl> _logger;

        public FeedbackServiceImpl(IFeedbackRepo feedbackRepo, ILogger<FeedbackServiceImpl> logger)
        {
            _feedbackRepo = feedbackRepo;
            _logger = logger;
        }

        public async Task<CreateReviewResponse> CreateReviewAsync(ClaimsPrincipal user, CreateReviewRequest request)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return ReviewResponseExtensions.CreateFailure("User not authenticated");
                }

                // Check if user is enrolled in the course
                var isEnrolled = await _feedbackRepo.IsUserEnrolledInCourseAsync(userId, request.CourseId);
                if (!isEnrolled)
                {
                    return ReviewResponseExtensions.CreateFailure("You must be enrolled in this course to leave a review");
                }

                // Check if user has already reviewed this course
                var hasReviewed = await _feedbackRepo.HasUserReviewedCourseAsync(userId, request.CourseId);
                if (hasReviewed)
                {
                    return ReviewResponseExtensions.CreateFailure("You have already reviewed this course");
                }

                var feedback = new Feedback
                {
                    FeedbackId = Guid.NewGuid().ToString(),
                    CourseId = request.CourseId,
                    UserId = userId,
                    StarRating = (byte)request.StarRating,
                    Comment = request.Comment?.Trim(),
                    FeedbackDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    FeedbackCreatedAt = DateTime.UtcNow,
                    FeedbackUpdatedAt = DateTime.UtcNow,
                    HiddenStatus = false,
                    IsVerifiedPurchase = true, // Since they're enrolled
                    HelpfulCount = 0
                };

                var result = await _feedbackRepo.CreateFeedbackAsync(feedback);
                if (!result)
                {
                    return ReviewResponseExtensions.CreateFailure("Failed to create review");
                }

                return ReviewResponseExtensions.CreateSuccess("Review created successfully", reviewId: feedback.FeedbackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for course {CourseId}", request.CourseId);
                return ReviewResponseExtensions.CreateFailure("An error occurred while creating the review");
            }
        }

        public async Task<UpdateReviewResponse> UpdateReviewAsync(ClaimsPrincipal user, UpdateReviewRequest request)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return ReviewResponseExtensions.UpdateFailure("User not authenticated");
                }

                var existingFeedback = await _feedbackRepo.GetFeedbackByIdAsync(request.ReviewId);
                if (existingFeedback == null)
                {
                    return ReviewResponseExtensions.UpdateFailure("Review not found");
                }

                if (existingFeedback.UserId != userId)
                {
                    return ReviewResponseExtensions.UpdateFailure("You can only edit your own reviews");
                }

                existingFeedback.StarRating = (byte)request.StarRating;
                existingFeedback.Comment = request.Comment?.Trim();
                existingFeedback.FeedbackUpdatedAt = DateTime.UtcNow;

                var result = await _feedbackRepo.UpdateFeedbackAsync(existingFeedback);
                if (!result)
                {
                    return ReviewResponseExtensions.UpdateFailure("Failed to update review");
                }

                return ReviewResponseExtensions.UpdateSuccess("Review updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review {ReviewId}", request.ReviewId);
                return ReviewResponseExtensions.UpdateFailure("An error occurred while updating the review");
            }
        }

        public async Task<DeleteReviewResponse> DeleteReviewAsync(ClaimsPrincipal user, string reviewId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return ReviewResponseExtensions.DeleteFailure("User not authenticated");
                }

                var existingFeedback = await _feedbackRepo.GetFeedbackByIdAsync(reviewId);
                if (existingFeedback == null)
                {
                    return ReviewResponseExtensions.DeleteFailure("Review not found");
                }

                if (existingFeedback.UserId != userId)
                {
                    return ReviewResponseExtensions.DeleteFailure("You can only delete your own reviews");
                }

                var result = await _feedbackRepo.DeleteFeedbackAsync(reviewId, userId);
                if (!result)
                {
                    return ReviewResponseExtensions.DeleteFailure("Failed to delete review");
                }

                return ReviewResponseExtensions.DeleteSuccess("Review deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
                return ReviewResponseExtensions.DeleteFailure("An error occurred while deleting the review");
            }
        }

        public async Task<GetReviewsResponse> GetCourseReviewsAsync(string courseId, int page = 1, int pageSize = 10)
        {
            try
            {
                var feedbacks = await _feedbackRepo.GetCourseFeedbacksAsync(courseId, page, pageSize);
                var totalReviews = await _feedbackRepo.GetCourseFeedbackCountAsync(courseId);
                var averageRating = await _feedbackRepo.GetCourseAverageRatingAsync(courseId);

                var totalPages = (int)Math.Ceiling((double)totalReviews / pageSize);

                var reviews = feedbacks.Select(f => new ReviewViewModel
                {
                    ReviewId = f.FeedbackId,
                    UserName = f.User.FullName ?? f.User.Username,
                    UserImage = f.User.UserImage ?? MediaConstants.Defaults.DefaultAvatarPath,
                    StarRating = f.StarRating ?? 0,
                    ReviewComment = f.Comment ?? "",
                    ReviewDate = f.FeedbackCreatedAt,
                    IsVerifiedPurchase = f.IsVerifiedPurchase ?? false,
                    UserId = f.UserId
                }).ToList();

                var viewModel = new ReviewListViewModel
                {
                    CourseId = courseId,
                    Reviews = reviews,
                    AverageRating = averageRating,
                    TotalReviews = totalReviews,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                };

                return ReviewResponseExtensions.GetSuccess(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews for course {CourseId}", courseId);
                return ReviewResponseExtensions.GetFailure("An error occurred while loading reviews");
            }
        }

        public async Task<CheckReviewEligibilityResponse> CheckReviewEligibilityAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return ReviewResponseExtensions.CheckSuccess(new CheckReviewEligibilityResponse
                    {
                        CanCreateReview = false,
                        IsEnrolled = false,
                        HasExistingReview = false
                    });
                }

                var isEnrolled = await _feedbackRepo.IsUserEnrolledInCourseAsync(userId, courseId);
                var existingReview = await _feedbackRepo.GetUserFeedbackForCourseAsync(userId, courseId);

                var canCreate = isEnrolled && existingReview == null;

                return ReviewResponseExtensions.CheckSuccess(new CheckReviewEligibilityResponse
                {
                    CanCreateReview = canCreate,
                    IsEnrolled = isEnrolled,
                    HasExistingReview = existingReview != null,
                    ExistingReviewId = existingReview?.FeedbackId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking review eligibility for course {CourseId}", courseId);
                return ReviewResponseExtensions.CheckFailure("An error occurred while checking review eligibility");
            }
        }
    }

    // Extension methods for response types
    public static class ReviewResponseExtensions
    {
        // CreateReviewResponse methods
        public static CreateReviewResponse CreateSuccess(string message, string? reviewId = null)
        {
            return new CreateReviewResponse
            {
                ReviewId = reviewId,
                IsSuccess = true,
                Message = message
            };
        }

        public static CreateReviewResponse CreateFailure(string message)
        {
            return new CreateReviewResponse
            {
                IsSuccess = false,
                Message = message
            };
        }

        // UpdateReviewResponse methods
        public static UpdateReviewResponse UpdateSuccess(string message)
        {
            return new UpdateReviewResponse
            {
                IsSuccess = true,
                Message = message
            };
        }

        public static UpdateReviewResponse UpdateFailure(string message)
        {
            return new UpdateReviewResponse
            {
                IsSuccess = false,
                Message = message
            };
        }

        // DeleteReviewResponse methods
        public static DeleteReviewResponse DeleteSuccess(string message)
        {
            return new DeleteReviewResponse
            {
                IsSuccess = true,
                Message = message
            };
        }

        public static DeleteReviewResponse DeleteFailure(string message)
        {
            return new DeleteReviewResponse
            {
                IsSuccess = false,
                Message = message
            };
        }

        // GetReviewsResponse methods
        public static GetReviewsResponse GetSuccess(ReviewListViewModel viewModel)
        {
            return new GetReviewsResponse
            {
                ViewModel = viewModel,
                IsSuccess = true,
                Message = "Reviews loaded successfully"
            };
        }

        public static GetReviewsResponse GetFailure(string message)
        {
            return new GetReviewsResponse
            {
                IsSuccess = false,
                Message = message
            };
        }

        // CheckReviewEligibilityResponse methods
        public static CheckReviewEligibilityResponse CheckSuccess(CheckReviewEligibilityResponse eligibility)
        {
            eligibility.IsSuccess = true;
            eligibility.Message = "Eligibility checked successfully";
            return eligibility;
        }

        public static CheckReviewEligibilityResponse CheckFailure(string message)
        {
            return new CheckReviewEligibilityResponse
            {
                IsSuccess = false,
                Message = message,
                CanCreateReview = false,
                IsEnrolled = false,
                HasExistingReview = false
            };
        }
    }
}