using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra_MVC.Controllers
{
    /// <summary>
    /// Controller for managing safe delete operations and deleted items
    /// </summary>
    [Authorize]
    public class SafeDeleteController : BaseController
    {
        private readonly ISafeDeleteService _safeDeleteService;
        private readonly ILogger<SafeDeleteController> _logger;

        public SafeDeleteController(ISafeDeleteService safeDeleteService, ILogger<SafeDeleteController> logger)
        {
            _safeDeleteService = safeDeleteService;
            _logger = logger;
        }

        /// <summary>
        /// Validate if an entity can be safely deleted
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateDelete([FromBody] ValidateDeleteRequest request)
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var validation = request.EntityType switch
                {
                    "Course" => await _safeDeleteService.ValidateEntityDeletionAsync<Course>(request.EntityId, userId),
                    "Chapter" => await _safeDeleteService.ValidateEntityDeletionAsync<Chapter>(request.EntityId, userId),
                    "Lesson" => await _safeDeleteService.ValidateEntityDeletionAsync<Lesson>(request.EntityId, userId),
                    "Account" => await _safeDeleteService.ValidateEntityDeletionAsync<Account>(request.EntityId, userId),
                    "Enrollment" => await _safeDeleteService.ValidateEntityDeletionAsync<Enrollment>(request.EntityId, userId),
                    _ => new SafeDeleteValidationResult { CanDelete = false, BlockingDependencies = { "Unsupported entity type" } }
                };

                return Json(new
                {
                    success = true,
                    canDelete = validation.CanDelete,
                    blockingDependencies = validation.BlockingDependencies,
                    warnings = validation.Warnings,
                    recommendedAction = validation.RecommendedAction,
                    requiresHardDelete = validation.RequiresHardDelete
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating delete for {EntityType} {EntityId}", request.EntityType, request.EntityId);
                return Json(new { success = false, message = "Error during validation" });
            }
        }

        /// <summary>
        /// Perform soft delete operation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete([FromBody] DeleteRequest request)
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = request.EntityType switch
                {
                    "Course" => await _safeDeleteService.SoftDeleteAsync<Course>(request.EntityId, userId, request.Reason),
                    "Chapter" => await _safeDeleteService.SoftDeleteAsync<Chapter>(request.EntityId, userId, request.Reason),
                    "Lesson" => await _safeDeleteService.SoftDeleteAsync<Lesson>(request.EntityId, userId, request.Reason),
                    "Account" => await _safeDeleteService.SoftDeleteAsync<Account>(request.EntityId, userId, request.Reason),
                    "Enrollment" => await _safeDeleteService.SoftDeleteAsync<Enrollment>(request.EntityId, userId, request.Reason),
                    _ => new SafeDeleteResult { Success = false, Message = "Unsupported entity type", ErrorCode = "UNSUPPORTED_TYPE" }
                };

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    errorCode = result.ErrorCode,
                    affectedEntities = result.AffectedEntities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing soft delete for {EntityType} {EntityId}", request.EntityType, request.EntityId);
                return Json(new { success = false, message = "Error during soft delete", errorCode = "INTERNAL_ERROR" });
            }
        }

        /// <summary>
        /// Perform hard delete operation (admin only for most entities)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete([FromBody] DeleteRequest request)
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = request.EntityType switch
                {
                    "Chapter" => await _safeDeleteService.HardDeleteAsync<Chapter>(request.EntityId, userId, request.Reason),
                    "Lesson" => await _safeDeleteService.HardDeleteAsync<Lesson>(request.EntityId, userId, request.Reason),
                    _ => new SafeDeleteResult { Success = false, Message = "Hard delete not supported for this entity type", ErrorCode = "HARD_DELETE_NOT_SUPPORTED" }
                };

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    errorCode = result.ErrorCode,
                    affectedEntities = result.AffectedEntities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing hard delete for {EntityType} {EntityId}", request.EntityType, request.EntityId);
                return Json(new { success = false, message = "Error during hard delete", errorCode = "INTERNAL_ERROR" });
            }
        }

        /// <summary>
        /// Restore a soft-deleted entity
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore([FromBody] RestoreRequest request)
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = request.EntityType switch
                {
                    "Course" => await _safeDeleteService.RestoreAsync<Course>(request.EntityId, userId, request.TargetStatus),
                    "Chapter" => await _safeDeleteService.RestoreAsync<Chapter>(request.EntityId, userId, request.TargetStatus),
                    "Lesson" => await _safeDeleteService.RestoreAsync<Lesson>(request.EntityId, userId, request.TargetStatus),
                    "Account" => await _safeDeleteService.RestoreAsync<Account>(request.EntityId, userId, request.TargetStatus),
                    "Enrollment" => await _safeDeleteService.RestoreAsync<Enrollment>(request.EntityId, userId, request.TargetStatus),
                    _ => new SafeDeleteResult { Success = false, Message = "Unsupported entity type", ErrorCode = "UNSUPPORTED_TYPE" }
                };

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    errorCode = result.ErrorCode,
                    affectedEntities = result.AffectedEntities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring {EntityType} {EntityId}", request.EntityType, request.EntityId);
                return Json(new { success = false, message = "Error during restore", errorCode = "INTERNAL_ERROR" });
            }
        }

        /// <summary>
        /// Get deleted entities for management interface
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDeletedEntities(string entityType)
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                object? entities = entityType switch
                {
                    "Course" => await _safeDeleteService.GetDeletedEntitiesAsync<Course>(userId),
                    "Chapter" => await _safeDeleteService.GetDeletedEntitiesAsync<Chapter>(userId),
                    "Lesson" => await _safeDeleteService.GetDeletedEntitiesAsync<Lesson>(userId),
                    _ => null
                };

                if (entities == null)
                {
                    return Json(new { success = false, message = "Unsupported entity type" });
                }

                // Cast to IEnumerable for LINQ operations
                var enumerable = entities as System.Collections.IEnumerable;
                if (enumerable == null)
                {
                    return Json(new { success = false, message = "Invalid entity collection" });
                }

                var results = new List<object>();
                foreach (var entity in enumerable)
                {
                    results.Add(new
                    {
                        id = GetEntityId(entity),
                        name = GetEntityName(entity),
                        type = entityType,
                        deletedDate = GetEntityDeletedDate(entity)
                    });
                }

                return Json(new
                {
                    success = true,
                    entities = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deleted entities of type {EntityType}", entityType);
                return Json(new { success = false, message = "Error retrieving deleted entities" });
            }
        }        /// <summary>
                 /// Admin interface for managing deleted items
                 /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDeletedItems(string search = "", string entityType = "All", int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated";
                    return RedirectToAction("Index", "Home");
                }

                // Validate parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                var (deletedItems, totalCount) = await _safeDeleteService.GetDeletedEntitiesPaginatedAsync(
                    userId, search, entityType, page, pageSize);

                var deletedItemViewModels = deletedItems.Select(item => CreateDeletedItemViewModel(item)).ToList();

                var viewModel = new AdminDeletedItemsViewModel
                {
                    DeletedItems = deletedItemViewModels,
                    SearchQuery = search,
                    EntityType = entityType,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    TotalItems = totalCount,
                    PageSize = pageSize
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin deleted items page");
                TempData["ErrorMessage"] = "Error loading deleted items";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Helper method to create DeletedItemViewModel from anonymous object
        /// </summary>
        private DeletedItemViewModel CreateDeletedItemViewModel(object item)
        {
            var type = item.GetType();
            var entityId = type.GetProperty("EntityId")?.GetValue(item)?.ToString() ?? "";
            var entityType = type.GetProperty("EntityType")?.GetValue(item)?.ToString() ?? "";
            var entityName = type.GetProperty("EntityName")?.GetValue(item)?.ToString() ?? "";
            var deletedDate = type.GetProperty("DeletedDate")?.GetValue(item) as DateTime?;
            var deletedByUserId = type.GetProperty("DeletedByUserId")?.GetValue(item)?.ToString() ?? "";
            var deleteReason = type.GetProperty("DeleteReason")?.GetValue(item)?.ToString() ?? "";

            return new DeletedItemViewModel
            {
                EntityId = entityId,
                EntityType = entityType,
                EntityName = entityName,
                DeletedDate = deletedDate,
                DeletedByUserId = deletedByUserId,
                DeleteReason = deleteReason,
                CanRestore = true
            };
        }

        // Helper methods
        private string GetEntityId(object entity)
        {
            return entity switch
            {
                Course course => course.CourseId,
                Chapter chapter => chapter.ChapterId,
                Lesson lesson => lesson.LessonId,
                _ => string.Empty
            };
        }

        private string GetEntityName(object entity)
        {
            return entity switch
            {
                Course course => course.CourseName,
                Chapter chapter => chapter.ChapterName,
                Lesson lesson => lesson.LessonName,
                _ => "Unknown"
            };
        }

        private DateTime? GetEntityDeletedDate(object entity)
        {
            return entity switch
            {
                Course course => course.CourseUpdatedAt,
                Chapter chapter => chapter.ChapterUpdatedAt,
                Lesson lesson => lesson.LessonUpdatedAt,
                _ => null
            };
        }
    }    // Request/Response models
    public class ValidateDeleteRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
    }

    public class DeleteRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RestoreRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public int TargetStatus { get; set; } = 1; // Published by default
    }
}
