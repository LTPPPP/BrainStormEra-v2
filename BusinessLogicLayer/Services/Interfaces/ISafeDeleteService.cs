using DataAccessLayer.Data;
using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Interface for safe delete operations with dependency checking and audit trail
    /// </summary>
    public interface ISafeDeleteService
    {
        /// <summary>
        /// Check if an entity can be safely deleted (no critical dependencies)
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entityId">Entity ID</param>
        /// <param name="userId">User performing the operation</param>
        /// <returns>Validation result with blocking dependencies</returns>
        Task<SafeDeleteValidationResult> ValidateEntityDeletionAsync<T>(string entityId, string userId) where T : class;

        /// <summary>
        /// Perform soft delete by setting status to Archived
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entityId">Entity ID</param>
        /// <param name="userId">User performing the operation</param>
        /// <param name="reason">Reason for deletion</param>
        /// <returns>Success result</returns>
        Task<SafeDeleteResult> SoftDeleteAsync<T>(string entityId, string userId, string? reason = null) where T : class;

        /// <summary>
        /// Perform hard delete after validation
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entityId">Entity ID</param>
        /// <param name="userId">User performing the operation</param>
        /// <param name="reason">Reason for deletion</param>
        /// <returns>Success result</returns>
        Task<SafeDeleteResult> HardDeleteAsync<T>(string entityId, string userId, string? reason = null) where T : class;

        /// <summary>
        /// Restore a soft-deleted entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entityId">Entity ID</param>
        /// <param name="userId">User performing the operation</param>
        /// <param name="targetStatus">Target status after restore (default: Published)</param>
        /// <returns>Success result</returns>
        Task<SafeDeleteResult> RestoreAsync<T>(string entityId, string userId, int targetStatus = 1) where T : class;        /// <summary>
                                                                                                                             /// Get all deleted entities of a specific type
                                                                                                                             /// </summary>
                                                                                                                             /// <typeparam name="T">Entity type</typeparam>
                                                                                                                             /// <param name="userId">User ID (for authorization)</param>
                                                                                                                             /// <returns>List of deleted entities</returns>
        Task<IEnumerable<T>> GetDeletedEntitiesAsync<T>(string userId) where T : class;

        /// <summary>
        /// Get paginated deleted entities across all types
        /// </summary>
        /// <param name="userId">User ID (for authorization)</param>
        /// <param name="search">Search query</param>
        /// <param name="entityType">Filter by entity type (All, Course, Chapter, Lesson)</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated deleted entities</returns>
        Task<(IEnumerable<object> Items, int TotalCount)> GetDeletedEntitiesPaginatedAsync(
            string userId, string search = "", string entityType = "All", int page = 1, int pageSize = 10);

        /// <summary>
        /// Create audit trail for delete operations
        /// </summary>
        /// <param name="operation">Delete operation details</param>
        /// <returns>Success result</returns>
        Task<bool> CreateAuditTrailAsync(DeleteAuditTrail operation);
    }

    /// <summary>
    /// Result of safe delete validation
    /// </summary>
    public class SafeDeleteValidationResult
    {
        public bool CanDelete { get; set; }
        public List<string> BlockingDependencies { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public bool RequiresHardDelete { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of safe delete operation
    /// </summary>
    public class SafeDeleteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public List<string> AffectedEntities { get; set; } = new List<string>();
        public DateTime OperationTimestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Audit trail for delete operations
    /// </summary>
    public class DeleteAuditTrail
    {
        public string TrailId { get; set; } = Guid.NewGuid().ToString();
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty; // SoftDelete, HardDelete, Restore
        public string Reason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EntityState { get; set; } = string.Empty; // JSON snapshot of entity before deletion
        public string AffectedRelatedEntities { get; set; } = string.Empty; // JSON list of affected entities
    }
}







