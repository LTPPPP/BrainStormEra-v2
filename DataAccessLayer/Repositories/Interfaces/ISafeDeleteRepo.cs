using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface ISafeDeleteRepo<T> : IBaseRepo<T> where T : class
    {
        // Soft delete operations
        Task<bool> SoftDeleteAsync(string id, string deletedBy, string? reason = null);
        Task<bool> SoftDeleteAsync(T entity, string deletedBy, string? reason = null);
        Task<bool> SoftDeleteRangeAsync(List<string> ids, string deletedBy, string? reason = null);
        Task<bool> SoftDeleteRangeAsync(List<T> entities, string deletedBy, string? reason = null);

        // Restore operations
        Task<bool> RestoreAsync(string id, string restoredBy);
        Task<bool> RestoreAsync(T entity, string restoredBy);
        Task<bool> RestoreRangeAsync(List<string> ids, string restoredBy);
        Task<bool> RestoreRangeAsync(List<T> entities, string restoredBy);

        // Query operations for soft deleted items
        Task<List<T>> GetDeletedItemsAsync(int page = 1, int pageSize = 20);
        Task<List<T>> GetDeletedItemsByUserAsync(string deletedBy, int page = 1, int pageSize = 20);
        Task<List<T>> GetDeletedItemsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<T?> GetDeletedItemByIdAsync(string id);
        Task<bool> IsDeletedAsync(string id);

        // Query operations excluding deleted items (default behavior)
        Task<List<T>> GetActiveItemsAsync(int page = 1, int pageSize = 20);
        Task<T?> GetActiveItemByIdAsync(string id);
        Task<int> GetActiveItemsCountAsync();
        Task<int> GetDeletedItemsCountAsync();

        // Include deleted items in queries
        Task<List<T>> GetAllItemsIncludingDeletedAsync(int page = 1, int pageSize = 20);
        Task<T?> GetItemByIdIncludingDeletedAsync(string id);
        Task<int> GetTotalItemsCountIncludingDeletedAsync();

        // Permanent delete operations (hard delete)
        Task<bool> PermanentDeleteAsync(string id, string deletedBy);
        Task<bool> PermanentDeleteAsync(T entity, string deletedBy);
        Task<bool> PermanentDeleteRangeAsync(List<string> ids, string deletedBy);
        Task<bool> PermanentDeleteRangeAsync(List<T> entities, string deletedBy);

        // Cleanup operations
        Task<bool> PermanentDeleteOldItemsAsync(int daysOld, string deletedBy);
        Task<List<T>> GetItemsOlderThanAsync(int daysOld);
        Task<bool> CleanupDeletedItemsAsync(int daysToKeep, string deletedBy);

        // Audit and tracking
        Task<List<DeleteAuditLog>> GetDeleteAuditLogsAsync(string? entityId = null, int page = 1, int pageSize = 20);
        Task<DeleteAuditLog?> GetDeleteAuditLogAsync(string entityId);
        Task<List<DeleteAuditLog>> GetDeleteLogsByUserAsync(string userId, int page = 1, int pageSize = 20);
        Task<List<DeleteAuditLog>> GetDeleteLogsByDateRangeAsync(DateTime startDate, DateTime endDate);

        // Statistics and reporting
        Task<DeleteStatistics> GetDeleteStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, int>> GetDeletedItemsByReasonAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<UserDeleteActivity>> GetTopDeletersAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<T>> GetFrequentlyDeletedItemsAsync(int count = 10);

        // Bulk operations
        Task<bool> BulkSoftDeleteByConditionAsync(Func<T, bool> condition, string deletedBy, string? reason = null);
        Task<bool> BulkRestoreByConditionAsync(Func<T, bool> condition, string restoredBy);
        Task<bool> BulkPermanentDeleteByConditionAsync(Func<T, bool> condition, string deletedBy);

        // Validation and safety
        Task<bool> CanDeleteAsync(string id);
        Task<bool> CanRestoreAsync(string id);
        Task<List<string>> GetDependentEntitiesAsync(string id);
        Task<bool> HasDependenciesAsync(string id);

        // Configuration and settings
        Task<bool> SetAutoCleanupAsync(bool enabled, int daysToKeep);
        Task<(bool enabled, int daysToKeep)> GetAutoCleanupSettingsAsync();
        Task<bool> EnableSoftDeleteForEntityAsync(string entityType);
        Task<bool> DisableSoftDeleteForEntityAsync(string entityType);
    }

    // Specialized safe delete repository for specific entities
    public interface ISafeDeleteRepo : ISafeDeleteRepo<object>
    {
        // User safe delete operations
        Task<bool> SoftDeleteUserAsync(string userId, string deletedBy, string? reason = null);
        Task<bool> RestoreUserAsync(string userId, string restoredBy);
        Task<List<Account>> GetDeletedUsersAsync(int page = 1, int pageSize = 20);

        // Course safe delete operations
        Task<bool> SoftDeleteCourseAsync(string courseId, string deletedBy, string? reason = null);
        Task<bool> RestoreCourseAsync(string courseId, string restoredBy);
        Task<List<Course>> GetDeletedCoursesAsync(int page = 1, int pageSize = 20);
        Task<bool> SoftDeleteCourseWithDependenciesAsync(string courseId, string deletedBy, string? reason = null);

        // Lesson safe delete operations
        Task<bool> SoftDeleteLessonAsync(string lessonId, string deletedBy, string? reason = null);
        Task<bool> RestoreLessonAsync(string lessonId, string restoredBy);
        Task<List<Lesson>> GetDeletedLessonsAsync(int page = 1, int pageSize = 20);

        // Chapter safe delete operations
        Task<bool> SoftDeleteChapterAsync(string chapterId, string deletedBy, string? reason = null);
        Task<bool> RestoreChapterAsync(string chapterId, string restoredBy);
        Task<List<Chapter>> GetDeletedChaptersAsync(int page = 1, int pageSize = 20);
        Task<bool> SoftDeleteChapterWithLessonsAsync(string chapterId, string deletedBy, string? reason = null);

        // Quiz safe delete operations
        Task<bool> SoftDeleteQuizAsync(string quizId, string deletedBy, string? reason = null);
        Task<bool> RestoreQuizAsync(string quizId, string restoredBy);
        Task<List<Quiz>> GetDeletedQuizzesAsync(int page = 1, int pageSize = 20);

        // Cross-entity operations
        Task<bool> SoftDeleteUserDataAsync(string userId, string deletedBy, string? reason = null);
        Task<bool> RestoreUserDataAsync(string userId, string restoredBy);
        Task<DeleteSummary> GetUserDeleteSummaryAsync(string userId);

        // Batch operations with dependencies
        Task<bool> SafeDeleteWithDependencyCheckAsync(string entityType, string entityId, string deletedBy, string? reason = null);
        Task<List<DependencyInfo>> GetDependencyInfoAsync(string entityType, string entityId);
        Task<bool> CascadeDeleteAsync(string entityType, string entityId, string deletedBy, string? reason = null);

        // Admin operations
        Task<AdminDeleteOverview> GetAdminDeleteOverviewAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<bool> AdminPermanentDeleteAsync(string entityType, string entityId, string adminId, string reason);
        Task<List<PendingDeleteRequest>> GetPendingDeleteRequestsAsync();
        Task<bool> ProcessDeleteRequestAsync(string requestId, string adminId, bool approved, string? notes = null);
    }

    // Supporting models for safe delete operations
    public class DeleteAuditLog
    {
        public string Id { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // SoftDelete, Restore, PermanentDelete
        public string DeletedBy { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }
        public string? Reason { get; set; }
        public string? RestoredBy { get; set; }
        public DateTime? RestoredAt { get; set; }
        public bool IsPermanent { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class DeleteStatistics
    {
        public int TotalDeleted { get; set; }
        public int TotalRestored { get; set; }
        public int TotalPermanentDeleted { get; set; }
        public Dictionary<string, int> DeletedByEntityType { get; set; } = new();
        public Dictionary<string, int> DeletedByUser { get; set; } = new();
        public Dictionary<string, int> DeletedByReason { get; set; } = new();
        public DateTime? LastDeletedAt { get; set; }
        public DateTime? LastRestoredAt { get; set; }
    }

    public class UserDeleteActivity
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int DeleteCount { get; set; }
        public int RestoreCount { get; set; }
        public int PermanentDeleteCount { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class DeleteSummary
    {
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, int> DeletedEntities { get; set; } = new();
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public bool CanRestore { get; set; }
        public List<string> Dependencies { get; set; } = new();
    }

    public class DependencyInfo
    {
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string DependencyType { get; set; } = string.Empty;
        public bool BlocksDeletion { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class AdminDeleteOverview
    {
        public int TotalDeletedItems { get; set; }
        public int PendingDeleteRequests { get; set; }
        public int ItemsScheduledForCleanup { get; set; }
        public Dictionary<string, int> DeletedByEntityType { get; set; } = new();
        public List<DeleteAuditLog> RecentDeletions { get; set; } = new();
        public List<UserDeleteActivity> TopDeleters { get; set; } = new();
    }

    public class PendingDeleteRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<DependencyInfo> Dependencies { get; set; } = new();
    }
}
