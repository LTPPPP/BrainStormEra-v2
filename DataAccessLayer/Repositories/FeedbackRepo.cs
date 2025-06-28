using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataAccessLayer.Repositories
{
    public class FeedbackRepo : IFeedbackRepo
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<FeedbackRepo>? _logger;

        public FeedbackRepo(BrainStormEraContext context, ILogger<FeedbackRepo>? logger = null)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CreateFeedbackAsync(Feedback feedback)
        {
            try
            {
                _context.Feedbacks.Add(feedback);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating feedback for course {CourseId} by user {UserId}",
                    feedback.CourseId, feedback.UserId);
                throw;
            }
        }

        public async Task<bool> UpdateFeedbackAsync(Feedback feedback)
        {
            try
            {
                var existingFeedback = await _context.Feedbacks
                    .FirstOrDefaultAsync(f => f.FeedbackId == feedback.FeedbackId && f.UserId == feedback.UserId);

                if (existingFeedback == null)
                    return false;

                existingFeedback.StarRating = feedback.StarRating;
                existingFeedback.Comment = feedback.Comment;
                existingFeedback.FeedbackUpdatedAt = DateTime.UtcNow;

                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating feedback {FeedbackId}", feedback.FeedbackId);
                throw;
            }
        }

        public async Task<bool> DeleteFeedbackAsync(string feedbackId, string userId)
        {
            try
            {
                var feedback = await _context.Feedbacks
                    .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId && f.UserId == userId);

                if (feedback == null)
                    return false;

                // Soft delete: Set HiddenStatus to true instead of removing the record
                feedback.HiddenStatus = true;
                feedback.FeedbackUpdatedAt = DateTime.UtcNow;

                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error soft deleting feedback {FeedbackId}", feedbackId);
                throw;
            }
        }

        public async Task<Feedback?> GetFeedbackByIdAsync(string feedbackId)
        {
            try
            {
                return await _context.Feedbacks
                    .Include(f => f.User)
                    .Include(f => f.Course)
                    .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId && (f.HiddenStatus == null || f.HiddenStatus == false));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting feedback {FeedbackId}", feedbackId);
                throw;
            }
        }

        public async Task<Feedback?> GetUserFeedbackForCourseAsync(string userId, string courseId)
        {
            try
            {
                return await _context.Feedbacks
                    .Include(f => f.User)
                    .Include(f => f.Course)
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.CourseId == courseId && (f.HiddenStatus == null || f.HiddenStatus == false));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user feedback for course {CourseId} by user {UserId}",
                    courseId, userId);
                throw;
            }
        }

        public async Task<List<Feedback>> GetCourseFeedbacksAsync(string courseId, int page = 1, int pageSize = 10)
        {
            try
            {
                return await _context.Feedbacks
                    .Include(f => f.User)
                    .Where(f => f.CourseId == courseId && (f.HiddenStatus == null || f.HiddenStatus == false))
                    .OrderByDescending(f => f.FeedbackCreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting feedbacks for course {CourseId}", courseId);
                throw;
            }
        }

        public async Task<bool> HasUserReviewedCourseAsync(string userId, string courseId)
        {
            try
            {
                return await _context.Feedbacks
                    .AnyAsync(f => f.UserId == userId && f.CourseId == courseId && (f.HiddenStatus == null || f.HiddenStatus == false));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user {UserId} has reviewed course {CourseId}",
                    userId, courseId);
                throw;
            }
        }

        public async Task<bool> IsUserEnrolledInCourseAsync(string userId, string courseId)
        {
            try
            {
                return await _context.Enrollments
                    .AnyAsync(e => e.UserId == userId && e.CourseId == courseId && (e.EnrollmentStatus == 1 || e.EnrollmentStatus == 3));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user {UserId} is enrolled in course {CourseId}",
                    userId, courseId);
                throw;
            }
        }

        public async Task<int> GetCourseFeedbackCountAsync(string courseId)
        {
            try
            {
                return await _context.Feedbacks
                    .Where(f => f.CourseId == courseId && (f.HiddenStatus == null || f.HiddenStatus == false))
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting feedback count for course {CourseId}", courseId);
                throw;
            }
        }

        public async Task<double> GetCourseAverageRatingAsync(string courseId)
        {
            try
            {
                var ratings = await _context.Feedbacks
                    .Where(f => f.CourseId == courseId && f.StarRating.HasValue &&
                               (f.HiddenStatus == null || f.HiddenStatus == false))
                    .Select(f => (double)f.StarRating!.Value)
                    .ToListAsync();

                return ratings.Any() ? ratings.Average() : 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting average rating for course {CourseId}", courseId);
                throw;
            }
        }

        public async Task<bool> RestoreFeedbackAsync(string feedbackId, string userId)
        {
            try
            {
                var feedback = await _context.Feedbacks
                    .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId && f.UserId == userId && f.HiddenStatus == true);

                if (feedback == null)
                    return false;

                feedback.HiddenStatus = false;
                feedback.FeedbackUpdatedAt = DateTime.UtcNow;

                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error restoring feedback {FeedbackId}", feedbackId);
                throw;
            }
        }

        public async Task<Feedback?> GetFeedbackByIdIncludingDeletedAsync(string feedbackId)
        {
            try
            {
                return await _context.Feedbacks
                    .Include(f => f.User)
                    .Include(f => f.Course)
                    .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting feedback {FeedbackId} including deleted", feedbackId);
                throw;
            }
        }

        public async Task<Feedback?> GetUserFeedbackForCourseIncludingDeletedAsync(string userId, string courseId)
        {
            try
            {
                return await _context.Feedbacks
                    .Include(f => f.User)
                    .Include(f => f.Course)
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.CourseId == courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user feedback for course {CourseId} by user {UserId} including deleted",
                    courseId, userId);
                throw;
            }
        }
    }
}