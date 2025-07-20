using BusinessLogicLayer.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra_MVC.Controllers
{
    /// <summary>
    /// Request models for SafeDelete operations
    /// </summary>
    public class ValidateDeleteRequest
    {
        public string EntityId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class DeleteRequest
    {
        public string EntityId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class RestoreRequest
    {
        public string EntityId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int TargetStatus { get; set; } = 1;
    }

    /// <summary>
    /// Controller for managing safe delete operations and deleted items
    /// </summary>
    [Authorize]
    public class SafeDeleteController : BaseController
    {
        private readonly SafeDeleteService _safeDeleteService;

        public SafeDeleteController(SafeDeleteService safeDeleteService)
        {
            _safeDeleteService = safeDeleteService;
        }                /// <summary>
                         /// Validate if an entity can be safely deleted
                         /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateDelete([FromBody] ValidateDeleteRequest request)
        {
            var result = await _safeDeleteService.ValidateEntityDeletionAsync<object>(request.EntityId, request.UserId);
            return Json(new { success = result.CanDelete, message = result.RecommendedAction, warnings = result.Warnings, blockingDependencies = result.BlockingDependencies });
        }

        /// <summary>
        /// Perform soft delete operation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete([FromBody] DeleteRequest request)
        {
            var result = await _safeDeleteService.SoftDeleteAsync<object>(request.EntityId, request.UserId, request.Reason);
            return Json(new { success = result.Success, message = result.Message, errorCode = result.ErrorCode });
        }                        /// <summary>
                                 /// Perform hard delete operation (instructor only for most entities)
                                 /// </summary>
        [HttpPost]
        [Authorize(Roles = "instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete([FromBody] DeleteRequest request)
        {
            var result = await _safeDeleteService.HardDeleteAsync<object>(request.EntityId, request.UserId, request.Reason);
            return Json(new { success = result.Success, message = result.Message, errorCode = result.ErrorCode });
        }

        /// <summary>
        /// Restore a soft-deleted entity
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore([FromBody] RestoreRequest request)
        {
            var result = await _safeDeleteService.RestoreAsync<object>(request.EntityId, request.UserId, request.TargetStatus);
            return Json(new { success = result.Success, message = result.Message, errorCode = result.ErrorCode });
        }

        /// <summary>
        /// Get deleted entities for management interface
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDeletedEntities(string entityType)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var (items, totalCount) = await _safeDeleteService.GetDeletedEntitiesPaginatedAsync(userId, "", entityType, 1, 100);
            return Json(new { success = true, items = items, totalCount = totalCount });
        }
    }
}

