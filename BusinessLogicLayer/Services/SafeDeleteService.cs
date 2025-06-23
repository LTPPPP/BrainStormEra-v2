using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using BrainStormEra_MVC.Models;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Reflection;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Service implementation for safe delete operations with comprehensive dependency checking
    /// </summary>
    public class SafeDeleteService : ISafeDeleteService
    {
        private readonly ISafeDeleteRepo _safeDeleteRepo;
        private readonly BrainStormEraContext _context;
        private readonly ILogger<SafeDeleteService> _logger;
        private readonly Dictionary<string, EntityDeletionPolicy> _deletionPolicies;

        public SafeDeleteService(
            ISafeDeleteRepo safeDeleteRepo,
            BrainStormEraContext context,
            ILogger<SafeDeleteService> logger)
        {
            _safeDeleteRepo = safeDeleteRepo;
            _context = context;
            _logger = logger;
            _deletionPolicies = InitializeDeletionPolicies();
        }

        /// <summary>
        /// Initialize deletion policies for different entity types
        /// </summary>
        private Dictionary<string, EntityDeletionPolicy> InitializeDeletionPolicies()
        {
            return new Dictionary<string, EntityDeletionPolicy>
            {
                ["Course"] = new EntityDeletionPolicy
                {
                    EntityType = "Course",
                    AllowSoftDelete = true,
                    AllowHardDelete = false, // Only when no enrollments
                    BlockingRelationships = new List<string> { "Enrollments" },
                    CascadeDeleteRelationships = new List<string> { "Chapters", "Lessons", "Feedbacks" },
                    RetentionDays = 90
                },
                ["Chapter"] = new EntityDeletionPolicy
                {
                    EntityType = "Chapter",
                    AllowSoftDelete = true,
                    AllowHardDelete = true,
                    CascadeDeleteRelationships = new List<string> { "Lessons" },
                    RetentionDays = 30
                },
                ["Lesson"] = new EntityDeletionPolicy
                {
                    EntityType = "Lesson",
                    AllowSoftDelete = true,
                    AllowHardDelete = true,
                    BlockingRelationships = new List<string> { "UserProgresses" },
                    RetentionDays = 30
                },
                ["Account"] = new EntityDeletionPolicy
                {
                    EntityType = "Account",
                    AllowSoftDelete = true,
                    AllowHardDelete = false,
                    RequiresAdminApproval = true,
                    BlockingRelationships = new List<string> { "CourseAuthors", "Enrollments", "PaymentTransactions" },
                    RetentionDays = 365
                },
                ["Enrollment"] = new EntityDeletionPolicy
                {
                    EntityType = "Enrollment",
                    AllowSoftDelete = true,
                    AllowHardDelete = false,
                    BlockingRelationships = new List<string> { "Certificates", "PaymentTransactions" },
                    RetentionDays = 180
                }
            };
        }

        public async Task<SafeDeleteValidationResult> ValidateEntityDeletionAsync<T>(string entityId, string userId) where T : class
        {
            var result = new SafeDeleteValidationResult();
            var entityType = typeof(T).Name;

            try
            {
                if (!_deletionPolicies.TryGetValue(entityType, out var policy))
                {
                    result.CanDelete = false;
                    result.BlockingDependencies.Add($"No deletion policy defined for {entityType}");
                    return result;
                }

                // Check entity-specific dependencies
                switch (entityType)
                {
                    case "Course":
                        return await ValidateCourseDeletionAsync(entityId, userId, policy);
                    case "Chapter":
                        return await ValidateChapterDeletionAsync(entityId, userId, policy);
                    case "Lesson":
                        return await ValidateLessonDeletionAsync(entityId, userId, policy);
                    case "Account":
                        return await ValidateAccountDeletionAsync(entityId, userId, policy);
                    case "Enrollment":
                        return await ValidateEnrollmentDeletionAsync(entityId, userId, policy);
                    default:
                        result.CanDelete = false;
                        result.BlockingDependencies.Add($"Validation not implemented for {entityType}");
                        return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating deletion for {EntityType} {EntityId}", entityType, entityId);
                result.CanDelete = false;
                result.BlockingDependencies.Add("Internal error during validation");
                return result;
            }
        }

        private async Task<SafeDeleteValidationResult> ValidateCourseDeletionAsync(string courseId, string userId, EntityDeletionPolicy policy)
        {
            var result = new SafeDeleteValidationResult();

            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Chapters)
                    .ThenInclude(ch => ch.Lessons)
                .Include(c => c.Feedbacks)
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.AuthorId == userId);

            if (course == null)
            {
                result.CanDelete = false;
                result.BlockingDependencies.Add("Course not found or access denied");
                return result;
            }

            // Check for active enrollments
            var activeEnrollments = course.Enrollments.Where(e => e.EnrollmentStatus != 4).Count(); // Not Archived
            if (activeEnrollments > 0)
            {
                result.CanDelete = false;
                result.BlockingDependencies.Add($"Course has {activeEnrollments} active enrollment(s)");
                result.RecommendedAction = "Archive course instead of deleting, or contact enrolled students";
            }

            // Note: Payment transactions are now independent of courses
            // No need to check payment transactions for course deletion

            // Check course status
            if (course.CourseStatus == 1 || course.CourseStatus == 2) // Published or Active
            {
                result.Warnings.Add("Course is published/active. Consider archiving instead");
            }

            // If no blocking dependencies, allow soft delete
            if (result.BlockingDependencies.Count == 0)
            {
                result.CanDelete = true;
                result.RecommendedAction = activeEnrollments > 0 ? "Soft delete (Archive)" : "Soft delete recommended";
            }

            return result;
        }

        private async Task<SafeDeleteValidationResult> ValidateChapterDeletionAsync(string chapterId, string userId, EntityDeletionPolicy policy)
        {
            var result = new SafeDeleteValidationResult();

            var chapter = await _context.Chapters
                .Include(c => c.Course)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.UserProgresses)
                .FirstOrDefaultAsync(c => c.ChapterId == chapterId && c.Course.AuthorId == userId);

            if (chapter == null)
            {
                result.CanDelete = false;
                result.BlockingDependencies.Add("Chapter not found or access denied");
                return result;
            }

            // Check for user progress in lessons
            var totalProgress = chapter.Lessons.SelectMany(l => l.UserProgresses).Count();
            if (totalProgress > 0)
            {
                result.Warnings.Add($"Chapter has {totalProgress} user progress record(s) in its lessons");
                result.RecommendedAction = "Soft delete to preserve learning history";
            }

            // Check if chapter has prerequisite dependencies
            var dependentChapters = await _context.Chapters
                .Where(c => c.UnlockAfterChapterId == chapterId)
                .CountAsync();

            if (dependentChapters > 0)
            {
                result.BlockingDependencies.Add($"Chapter is a prerequisite for {dependentChapters} other chapter(s)");
            }

            if (result.BlockingDependencies.Count == 0)
            {
                result.CanDelete = true;
                result.RecommendedAction = totalProgress > 0 ? "Soft delete recommended" : "Hard delete allowed";
            }

            return result;
        }

        private async Task<SafeDeleteValidationResult> ValidateLessonDeletionAsync(string lessonId, string userId, EntityDeletionPolicy policy)
        {
            var result = new SafeDeleteValidationResult();

            var lesson = await _context.Lessons
                .Include(l => l.Chapter)
                    .ThenInclude(c => c.Course)
                .Include(l => l.UserProgresses)
                .Include(l => l.Quizzes)
                    .ThenInclude(q => q.QuizAttempts)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId && l.Chapter.Course.AuthorId == userId);

            if (lesson == null)
            {
                result.CanDelete = false;
                result.BlockingDependencies.Add("Lesson not found or access denied");
                return result;
            }

            // Check for user progress
            var progressCount = lesson.UserProgresses.Count;
            if (progressCount > 0)
            {
                result.Warnings.Add($"Lesson has {progressCount} user progress record(s)");
                result.RecommendedAction = "Soft delete to preserve learning history";
            }

            // Check for quiz attempts
            var quizAttempts = lesson.Quizzes.SelectMany(q => q.QuizAttempts).Count();
            if (quizAttempts > 0)
            {
                result.Warnings.Add($"Lesson has {quizAttempts} quiz attempt(s)");
            }

            // Check if lesson is a prerequisite for other lessons
            var dependentLessons = await _context.Lessons
                .Where(l => l.UnlockAfterLessonId == lessonId)
                .CountAsync();

            if (dependentLessons > 0)
            {
                result.BlockingDependencies.Add($"Lesson is a prerequisite for {dependentLessons} other lesson(s)");
            }

            if (result.BlockingDependencies.Count == 0)
            {
                result.CanDelete = true;
                result.RecommendedAction = progressCount > 0 || quizAttempts > 0 ? "Soft delete recommended" : "Hard delete allowed";
            }

            return result;
        }

        private async Task<SafeDeleteValidationResult> ValidateAccountDeletionAsync(string accountId, string userId, EntityDeletionPolicy policy)
        {
            var result = new SafeDeleteValidationResult();

            // Only admin or the account owner can delete
            var currentUser = await _context.Accounts.FindAsync(userId);
            if (currentUser?.UserRole != "Admin" && userId != accountId)
            {
                result.CanDelete = false;
                result.BlockingDependencies.Add("Insufficient permissions to delete account");
                return result;
            }

            var account = await _context.Accounts
                .Include(a => a.CourseAuthors)
                .Include(a => a.Enrollments)
                .Include(a => a.PaymentTransactionUsers)
                .FirstOrDefaultAsync(a => a.UserId == accountId);

            if (account == null)
            {
                result.CanDelete = false;
                result.BlockingDependencies.Add("Account not found");
                return result;
            }

            // Check for authored courses
            var activeCourses = account.CourseAuthors.Where(c => c.CourseStatus != 4).Count(); // Not Archived
            if (activeCourses > 0)
            {
                result.BlockingDependencies.Add($"Account has {activeCourses} active course(s) as author");
            }

            // Check for active enrollments
            var activeEnrollments = account.Enrollments.Where(e => e.EnrollmentStatus != 4).Count(); // Not Archived
            if (activeEnrollments > 0)
            {
                result.BlockingDependencies.Add($"Account has {activeEnrollments} active enrollment(s)");
            }

            // Check for payment history
            var hasPayments = account.PaymentTransactionUsers.Any();
            if (hasPayments)
            {
                result.BlockingDependencies.Add("Account has payment transaction history");
            }

            // Account deletion always requires soft delete for compliance
            result.RequiresHardDelete = false;
            result.RecommendedAction = "Soft delete only (data retention compliance)";

            if (result.BlockingDependencies.Count == 0)
            {
                result.CanDelete = true;
            }

            return result;
        }

        private async Task<SafeDeleteValidationResult> ValidateEnrollmentDeletionAsync(string enrollmentId, string userId, EntityDeletionPolicy policy)
        {
            var result = new SafeDeleteValidationResult(); var enrollment = await _context.Enrollments
                .Include(e => e.Certificates)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId && e.UserId == userId);

            if (enrollment == null)
            {
                result.CanDelete = false;
                result.BlockingDependencies.Add("Enrollment not found or access denied");
                return result;
            }

            // Check for certificates
            if (enrollment.Certificates.Any())
            {
                result.BlockingDependencies.Add("Enrollment has issued certificates");
            }

            // Note: Payment transactions are now independent of specific enrollments
            // We don't block enrollment deletion based on payment history

            // Check completion status
            if (enrollment.EnrollmentStatus == 6) // Completed
            {
                result.Warnings.Add("Enrollment is completed - consider soft delete for record keeping");
            }

            if (result.BlockingDependencies.Count == 0)
            {
                result.CanDelete = true;
                result.RecommendedAction = "Soft delete recommended for audit trail";
            }

            return result;
        }

        public async Task<SafeDeleteResult> SoftDeleteAsync<T>(string entityId, string userId, string? reason = null) where T : class
        {
            var result = new SafeDeleteResult();
            var entityType = typeof(T).Name;

            try
            {
                // Validate first
                var validation = await ValidateEntityDeletionAsync<T>(entityId, userId);
                if (!validation.CanDelete)
                {
                    result.Success = false;
                    result.Message = "Entity cannot be deleted: " + string.Join(", ", validation.BlockingDependencies);
                    result.ErrorCode = "VALIDATION_FAILED";
                    return result;
                }

                // Perform soft delete by updating status to Archived (4)
                var success = await UpdateEntityStatusAsync<T>(entityId, 4); // Archived status

                if (success)
                {
                    // Create audit trail
                    await CreateAuditTrailAsync(new DeleteAuditTrail
                    {
                        EntityType = entityType,
                        EntityId = entityId,
                        UserId = userId,
                        Operation = "SoftDelete",
                        Reason = reason ?? "Soft delete operation",
                        EntityState = await GetEntitySnapshotAsync<T>(entityId)
                    });

                    result.Success = true;
                    result.Message = $"{entityType} soft deleted successfully";
                    result.AffectedEntities.Add($"{entityType}:{entityId}");
                }
                else
                {
                    result.Success = false;
                    result.Message = "Failed to update entity status";
                    result.ErrorCode = "UPDATE_FAILED";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing soft delete for {EntityType} {EntityId}", entityType, entityId);
                result.Success = false;
                result.Message = "Internal error during soft delete";
                result.ErrorCode = "INTERNAL_ERROR";
            }

            return result;
        }

        public async Task<SafeDeleteResult> HardDeleteAsync<T>(string entityId, string userId, string? reason = null) where T : class
        {
            var result = new SafeDeleteResult();
            var entityType = typeof(T).Name;

            try
            {
                // Validate first
                var validation = await ValidateEntityDeletionAsync<T>(entityId, userId);
                if (!validation.CanDelete || validation.RequiresHardDelete == false)
                {
                    result.Success = false;
                    result.Message = validation.RequiresHardDelete == false
                        ? "Hard delete not allowed for this entity type"
                        : "Entity cannot be deleted: " + string.Join(", ", validation.BlockingDependencies);
                    result.ErrorCode = "HARD_DELETE_BLOCKED";
                    return result;
                }

                // Get entity snapshot before deletion
                var entitySnapshot = await GetEntitySnapshotAsync<T>(entityId);

                // Perform hard delete based on entity type
                var success = await PerformHardDeleteAsync<T>(entityId, userId);

                if (success)
                {
                    // Create audit trail
                    await CreateAuditTrailAsync(new DeleteAuditTrail
                    {
                        EntityType = entityType,
                        EntityId = entityId,
                        UserId = userId,
                        Operation = "HardDelete",
                        Reason = reason ?? "Hard delete operation",
                        EntityState = entitySnapshot
                    });

                    result.Success = true;
                    result.Message = $"{entityType} permanently deleted";
                    result.AffectedEntities.Add($"{entityType}:{entityId}");
                }
                else
                {
                    result.Success = false;
                    result.Message = "Failed to delete entity";
                    result.ErrorCode = "DELETE_FAILED";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing hard delete for {EntityType} {EntityId}", entityType, entityId);
                result.Success = false;
                result.Message = "Internal error during hard delete";
                result.ErrorCode = "INTERNAL_ERROR";
            }

            return result;
        }

        public async Task<SafeDeleteResult> RestoreAsync<T>(string entityId, string userId, int targetStatus = 1) where T : class
        {
            var result = new SafeDeleteResult();
            var entityType = typeof(T).Name;

            try
            {
                // Check if entity exists and is archived
                var isArchived = await IsEntityArchivedAsync<T>(entityId);
                if (!isArchived)
                {
                    result.Success = false;
                    result.Message = "Entity is not archived or does not exist";
                    result.ErrorCode = "NOT_ARCHIVED";
                    return result;
                }

                // Restore by updating status
                var success = await UpdateEntityStatusAsync<T>(entityId, targetStatus);

                if (success)
                {
                    // Create audit trail
                    await CreateAuditTrailAsync(new DeleteAuditTrail
                    {
                        EntityType = entityType,
                        EntityId = entityId,
                        UserId = userId,
                        Operation = "Restore",
                        Reason = $"Restored to status {targetStatus}"
                    });

                    result.Success = true;
                    result.Message = $"{entityType} restored successfully";
                    result.AffectedEntities.Add($"{entityType}:{entityId}");
                }
                else
                {
                    result.Success = false;
                    result.Message = "Failed to restore entity";
                    result.ErrorCode = "RESTORE_FAILED";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring {EntityType} {EntityId}", entityType, entityId);
                result.Success = false;
                result.Message = "Internal error during restore";
                result.ErrorCode = "INTERNAL_ERROR";
            }

            return result;
        }

        public async Task<IEnumerable<T>> GetDeletedEntitiesAsync<T>(string userId) where T : class
        {
            var entityType = typeof(T).Name;

            return entityType switch
            {
                "Course" => (IEnumerable<T>)await _context.Courses
                    .Where(c => c.CourseStatus == 4 && c.AuthorId == userId)
                    .ToListAsync(),
                "Chapter" => (IEnumerable<T>)await _context.Chapters
                    .Include(c => c.Course)
                    .Where(c => c.ChapterStatus == 4 && c.Course.AuthorId == userId)
                    .ToListAsync(),
                "Lesson" => (IEnumerable<T>)await _context.Lessons
                    .Include(l => l.Chapter.Course)
                    .Where(l => l.LessonStatus == 4 && l.Chapter.Course.AuthorId == userId)
                    .ToListAsync(),
                _ => new List<T>()
            };
        }

        public async Task<(IEnumerable<object> Items, int TotalCount)> GetDeletedEntitiesPaginatedAsync(
            string userId, string search = "", string entityType = "All", int page = 1, int pageSize = 10)
        {
            try
            {
                var allDeletedItems = new List<object>();

                // Get deleted courses
                if (entityType == "All" || entityType == "Course")
                {
                    var deletedCourses = await _context.Courses
                    .Where(c => c.CourseStatus == 4 && c.AuthorId == userId)
                    .Where(c => string.IsNullOrEmpty(search) ||
                               c.CourseName.ToLower().Contains(search.ToLower()) ||
                               (c.CourseDescription != null && c.CourseDescription.ToLower().Contains(search.ToLower())))
                        .Select(c => new
                        {
                            EntityId = c.CourseId,
                            EntityType = "Course",
                            EntityName = c.CourseName,
                            DeletedDate = c.CourseUpdatedAt,
                            DeletedByUserId = c.AuthorId,
                            DeleteReason = "Archived by author"
                        })
                        .ToListAsync();

                    allDeletedItems.AddRange(deletedCourses);
                }

                // Get deleted chapters
                if (entityType == "All" || entityType == "Chapter")
                {
                    var deletedChapters = await _context.Chapters
                        .Include(c => c.Course)
                        .Where(c => c.ChapterStatus == 4 && c.Course.AuthorId == userId)
                        .Where(c => string.IsNullOrEmpty(search) ||
                                   (c.ChapterName != null && c.ChapterName.ToLower().Contains(search.ToLower())) ||
                                   (c.ChapterDescription != null && c.ChapterDescription.ToLower().Contains(search.ToLower())))
                        .Select(c => new
                        {
                            EntityId = c.ChapterId,
                            EntityType = "Chapter",
                            EntityName = c.ChapterName,
                            DeletedDate = c.ChapterUpdatedAt,
                            DeletedByUserId = c.Course.AuthorId,
                            DeleteReason = "Archived with course"
                        })
                        .ToListAsync();

                    allDeletedItems.AddRange(deletedChapters);
                }

                // Get deleted lessons
                if (entityType == "All" || entityType == "Lesson")
                {
                    var deletedLessons = await _context.Lessons
                        .Include(l => l.Chapter.Course)
                        .Where(l => l.LessonStatus == 4 && l.Chapter.Course.AuthorId == userId)
                        .Where(l => string.IsNullOrEmpty(search) ||
                                   (l.LessonName != null && l.LessonName.ToLower().Contains(search.ToLower())) ||
                                   (l.LessonDescription != null && l.LessonDescription.ToLower().Contains(search.ToLower())))
                        .Select(l => new
                        {
                            EntityId = l.LessonId,
                            EntityType = "Lesson",
                            EntityName = l.LessonName,
                            DeletedDate = l.LessonUpdatedAt,
                            DeletedByUserId = l.Chapter.Course.AuthorId,
                            DeleteReason = "Archived with chapter"
                        })
                        .ToListAsync();

                    allDeletedItems.AddRange(deletedLessons);
                }

                // Sort by deleted date (most recent first)
                var sortedItems = allDeletedItems
                    .OrderByDescending(item => GetDeletedDate(item))
                    .ToList();

                var totalCount = sortedItems.Count;
                var paginatedItems = sortedItems
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (paginatedItems, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated deleted entities for user {UserId}", userId);
                return (new List<object>(), 0);
            }
        }

        private DateTime? GetDeletedDate(object item)
        {
            var type = item.GetType();
            var deletedDateProperty = type.GetProperty("DeletedDate");
            return deletedDateProperty?.GetValue(item) as DateTime?;
        }
        public Task<bool> CreateAuditTrailAsync(DeleteAuditTrail operation)
        {
            try
            {
                // TODO: Implement audit logging when DeleteAuditLog is added to DataAccessLayer
                // For now, just log the operation
                _logger.LogInformation("Audit Trail: {Operation} performed on {EntityType} {EntityId} by user {UserId}",
                    operation.Operation, operation.EntityType, operation.EntityId, operation.UserId);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit trail for {Operation} on {EntityType} {EntityId}",
                    operation.Operation, operation.EntityType, operation.EntityId);
                return Task.FromResult(false);
            }
        }

        // Helper methods
        private async Task<bool> UpdateEntityStatusAsync<T>(string entityId, int status) where T : class
        {
            var entityType = typeof(T).Name;

            switch (entityType)
            {
                case "Course":
                    var course = await _context.Courses.FindAsync(entityId);
                    if (course != null)
                    {
                        course.CourseStatus = status;
                        course.CourseUpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    break;

                case "Chapter":
                    var chapter = await _context.Chapters.FindAsync(entityId);
                    if (chapter != null)
                    {
                        chapter.ChapterStatus = status;
                        chapter.ChapterUpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    break;

                case "Lesson":
                    var lesson = await _context.Lessons.FindAsync(entityId);
                    if (lesson != null)
                    {
                        lesson.LessonStatus = status;
                        lesson.LessonUpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    break;
            }

            return false;
        }

        private async Task<bool> IsEntityArchivedAsync<T>(string entityId) where T : class
        {
            var entityType = typeof(T).Name;

            return entityType switch
            {
                "Course" => await _context.Courses.AnyAsync(c => c.CourseId == entityId && c.CourseStatus == 4),
                "Chapter" => await _context.Chapters.AnyAsync(c => c.ChapterId == entityId && c.ChapterStatus == 4),
                "Lesson" => await _context.Lessons.AnyAsync(l => l.LessonId == entityId && l.LessonStatus == 4),
                _ => false
            };
        }

        private async Task<string> GetEntitySnapshotAsync<T>(string entityId) where T : class
        {
            try
            {
                var entityType = typeof(T).Name;
                object? entity = entityType switch
                {
                    "Course" => await _context.Courses.FindAsync(entityId),
                    "Chapter" => await _context.Chapters.FindAsync(entityId),
                    "Lesson" => await _context.Lessons.FindAsync(entityId),
                    _ => null
                };

                return entity != null ? JsonSerializer.Serialize(entity) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<bool> PerformHardDeleteAsync<T>(string entityId, string userId) where T : class
        {
            var entityType = typeof(T).Name;

            switch (entityType)
            {
                case "Chapter":
                    var chapter = await _context.Chapters
                        .Include(c => c.Lessons)
                        .Include(c => c.Course)
                        .FirstOrDefaultAsync(c => c.ChapterId == entityId && c.Course.AuthorId == userId);

                    if (chapter != null)
                    {
                        _context.Lessons.RemoveRange(chapter.Lessons);
                        _context.Chapters.Remove(chapter);
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    break;

                case "Lesson":
                    var lesson = await _context.Lessons
                        .Include(l => l.Chapter.Course)
                        .FirstOrDefaultAsync(l => l.LessonId == entityId && l.Chapter.Course.AuthorId == userId);

                    if (lesson != null)
                    {
                        _context.Lessons.Remove(lesson);
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}








