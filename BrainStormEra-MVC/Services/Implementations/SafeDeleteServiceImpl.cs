using System.Security.Claims;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BrainStormEra_MVC.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles controller-specific business logic for SafeDelete operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// validation, error handling, and response formatting.
    /// </summary>
    public class SafeDeleteServiceImpl
    {
        private readonly ISafeDeleteService _safeDeleteService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SafeDeleteServiceImpl> _logger;

        public SafeDeleteServiceImpl(
            ISafeDeleteService safeDeleteService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SafeDeleteServiceImpl> logger)
        {
            _safeDeleteService = safeDeleteService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        #region Controller Business Logic Methods        /// <summary>
        /// Handle ValidateDelete action with authentication and response formatting
        /// </summary>
        public async Task<SafeDeleteControllerResult> HandleValidateDeleteAsync(ValidateDeleteRequest request)
        {
            var result = new SafeDeleteControllerResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.JsonResponse = new { success = false, message = "User not authenticated" };
                    return result;
                }                // Validate request
                var validationResult = ValidateDeleteRequestData(request);
                if (!validationResult.IsValid)
                {
                    result.JsonResponse = new
                    {
                        success = false,
                        message = "Invalid request: " + string.Join(", ", validationResult.Errors)
                    };
                    return result;
                }

                // Perform validation
                var validation = request.EntityType switch
                {
                    "Course" => await _safeDeleteService.ValidateEntityDeletionAsync<Course>(request.EntityId, userId),
                    "Chapter" => await _safeDeleteService.ValidateEntityDeletionAsync<Chapter>(request.EntityId, userId),
                    "Lesson" => await _safeDeleteService.ValidateEntityDeletionAsync<Lesson>(request.EntityId, userId),
                    "Account" => await _safeDeleteService.ValidateEntityDeletionAsync<Account>(request.EntityId, userId),
                    "Enrollment" => await _safeDeleteService.ValidateEntityDeletionAsync<Enrollment>(request.EntityId, userId),
                    _ => new SafeDeleteValidationResult { CanDelete = false, BlockingDependencies = { "Unsupported entity type" } }
                };

                result.IsSuccess = true;
                result.JsonResponse = new
                {
                    success = true,
                    canDelete = validation.CanDelete,
                    blockingDependencies = validation.BlockingDependencies,
                    warnings = validation.Warnings,
                    recommendedAction = validation.RecommendedAction,
                    requiresHardDelete = validation.RequiresHardDelete
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating delete for {EntityType} {EntityId}", request.EntityType, request.EntityId);
                result.JsonResponse = new { success = false, message = "Error during validation" };
                return result;
            }
        }        /// <summary>
                 /// Handle SoftDelete action with authentication and response formatting
                 /// </summary>
        public async Task<SafeDeleteControllerResult> HandleSoftDeleteAsync(DeleteRequest request)
        {
            var result = new SafeDeleteControllerResult(); try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.JsonResponse = new { success = false, message = "User not authenticated" };
                    return result;
                }

                // Validate request
                var validationResult = ValidateDeleteRequestData(request);
                if (!validationResult.IsValid)
                {
                    result.JsonResponse = new
                    {
                        success = false,
                        message = "Invalid request: " + string.Join(", ", validationResult.Errors)
                    };
                    return result;
                }

                // Perform soft delete
                var deleteResult = request.EntityType switch
                {
                    "Course" => await _safeDeleteService.SoftDeleteAsync<Course>(request.EntityId, userId, request.Reason),
                    "Chapter" => await _safeDeleteService.SoftDeleteAsync<Chapter>(request.EntityId, userId, request.Reason),
                    "Lesson" => await _safeDeleteService.SoftDeleteAsync<Lesson>(request.EntityId, userId, request.Reason),
                    "Account" => await _safeDeleteService.SoftDeleteAsync<Account>(request.EntityId, userId, request.Reason),
                    "Enrollment" => await _safeDeleteService.SoftDeleteAsync<Enrollment>(request.EntityId, userId, request.Reason),
                    _ => new SafeDeleteResult { Success = false, Message = "Unsupported entity type", ErrorCode = "UNSUPPORTED_TYPE" }
                };

                result.IsSuccess = true;
                result.JsonResponse = new
                {
                    success = deleteResult.Success,
                    message = deleteResult.Message,
                    errorCode = deleteResult.ErrorCode,
                    affectedEntities = deleteResult.AffectedEntities
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing soft delete for {EntityType} {EntityId}", request.EntityType, request.EntityId);
                result.JsonResponse = new { success = false, message = "Error during soft delete", errorCode = "INTERNAL_ERROR" };
                return result;
            }
        }        /// <summary>
                 /// Handle HardDelete action with authentication and response formatting
                 /// </summary>
        public async Task<SafeDeleteControllerResult> HandleHardDeleteAsync(DeleteRequest request)
        {
            var result = new SafeDeleteControllerResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId(); if (string.IsNullOrEmpty(userId))
                {
                    result.JsonResponse = new { success = false, message = "User not authenticated" };
                    return result;
                }

                // Validate request
                var validationResult = ValidateDeleteRequestData(request);
                if (!validationResult.IsValid)
                {
                    result.JsonResponse = new
                    {
                        success = false,
                        message = "Invalid request: " + string.Join(", ", validationResult.Errors)
                    };
                    return result;
                }

                // Perform hard delete
                var deleteResult = request.EntityType switch
                {
                    "Chapter" => await _safeDeleteService.HardDeleteAsync<Chapter>(request.EntityId, userId, request.Reason),
                    "Lesson" => await _safeDeleteService.HardDeleteAsync<Lesson>(request.EntityId, userId, request.Reason),
                    _ => new SafeDeleteResult { Success = false, Message = "Hard delete not supported for this entity type", ErrorCode = "HARD_DELETE_NOT_SUPPORTED" }
                };

                result.IsSuccess = true;
                result.JsonResponse = new
                {
                    success = deleteResult.Success,
                    message = deleteResult.Message,
                    errorCode = deleteResult.ErrorCode,
                    affectedEntities = deleteResult.AffectedEntities
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing hard delete for {EntityType} {EntityId}", request.EntityType, request.EntityId);
                result.JsonResponse = new { success = false, message = "Error during hard delete", errorCode = "INTERNAL_ERROR" };
                return result;
            }
        }        /// <summary>
                 /// Handle Restore action with authentication and response formatting
                 /// </summary>
        public async Task<SafeDeleteControllerResult> HandleRestoreAsync(RestoreRequest request)
        {
            var result = new SafeDeleteControllerResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.JsonResponse = new { success = false, message = "User not authenticated" };
                    return result;
                }

                // Validate request
                var validationResult = ValidateRestoreRequest(request);
                if (!validationResult.IsValid)
                {
                    result.JsonResponse = new
                    {
                        success = false,
                        message = "Invalid request: " + string.Join(", ", validationResult.Errors)
                    };
                    return result;
                }

                // Perform restore
                var restoreResult = request.EntityType switch
                {
                    "Course" => await _safeDeleteService.RestoreAsync<Course>(request.EntityId, userId, request.TargetStatus),
                    "Chapter" => await _safeDeleteService.RestoreAsync<Chapter>(request.EntityId, userId, request.TargetStatus),
                    "Lesson" => await _safeDeleteService.RestoreAsync<Lesson>(request.EntityId, userId, request.TargetStatus),
                    "Account" => await _safeDeleteService.RestoreAsync<Account>(request.EntityId, userId, request.TargetStatus),
                    "Enrollment" => await _safeDeleteService.RestoreAsync<Enrollment>(request.EntityId, userId, request.TargetStatus),
                    _ => new SafeDeleteResult { Success = false, Message = "Unsupported entity type", ErrorCode = "UNSUPPORTED_TYPE" }
                };

                result.IsSuccess = true;
                result.JsonResponse = new
                {
                    success = restoreResult.Success,
                    message = restoreResult.Message,
                    errorCode = restoreResult.ErrorCode,
                    affectedEntities = restoreResult.AffectedEntities
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring {EntityType} {EntityId}", request.EntityType, request.EntityId);
                result.JsonResponse = new { success = false, message = "Error during restore", errorCode = "INTERNAL_ERROR" };
                return result;
            }
        }

        /// <summary>
        /// Handle GetDeletedEntities action with authentication and response formatting
        /// </summary>
        public async Task<SafeDeleteControllerResult> HandleGetDeletedEntitiesAsync(string entityType)
        {
            var result = new SafeDeleteControllerResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.JsonResponse = new { success = false, message = "User not authenticated" };
                    return result;
                }

                // Validate entity type
                if (!IsValidEntityType(entityType))
                {
                    result.JsonResponse = new { success = false, message = "Unsupported entity type" };
                    return result;
                }

                // Get deleted entities
                object? entities = entityType switch
                {
                    "Course" => await _safeDeleteService.GetDeletedEntitiesAsync<Course>(userId),
                    "Chapter" => await _safeDeleteService.GetDeletedEntitiesAsync<Chapter>(userId),
                    "Lesson" => await _safeDeleteService.GetDeletedEntitiesAsync<Lesson>(userId),
                    _ => null
                };

                if (entities == null)
                {
                    result.JsonResponse = new { success = false, message = "Unsupported entity type" };
                    return result;
                }

                // Format entities for response
                var formattedEntities = FormatEntitiesForResponse(entities, entityType);

                result.IsSuccess = true;
                result.JsonResponse = new
                {
                    success = true,
                    entities = formattedEntities
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deleted entities of type {EntityType}", entityType);
                result.JsonResponse = new { success = false, message = "Error retrieving deleted entities" };
                return result;
            }
        }

        /// <summary>
        /// Handle AdminDeletedItems action with authentication and pagination
        /// </summary>
        public async Task<AdminDeletedItemsControllerResult> HandleAdminDeletedItemsAsync(
            ClaimsPrincipal user, string search = "", string entityType = "All", int page = 1, int pageSize = 10)
        {
            var result = new AdminDeletedItemsControllerResult();

            try
            {
                // Check authentication
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    result.TempDataErrorMessage = "User not authenticated";
                    result.ShouldRedirect = true;
                    result.RedirectAction = "Index";
                    result.RedirectController = "Home";
                    return result;
                }

                // Validate parameters
                var validationResult = ValidateAdminDeletedItemsParameters(search, entityType, page, pageSize);
                if (!validationResult.IsValid)
                {
                    result.TempDataErrorMessage = "Invalid parameters: " + string.Join(", ", validationResult.Errors);
                    result.ShouldRedirect = true;
                    result.RedirectAction = "Index";
                    result.RedirectController = "Home";
                    return result;
                }

                // Normalize parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                // Get paginated deleted items
                var (deletedItems, totalCount) = await _safeDeleteService.GetDeletedEntitiesPaginatedAsync(
                    userId, search, entityType, page, pageSize);

                // Create view models
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

                result.IsSuccess = true;
                result.ViewModel = viewModel;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin deleted items page");
                result.TempDataErrorMessage = "Error loading deleted items";
                result.ShouldRedirect = true;
                result.RedirectAction = "Index";
                result.RedirectController = "Home";
                return result;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get current user ID from HTTP context
        /// </summary>
        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
        }        /// <summary>
                 /// Validate delete request structure
                 /// </summary>
        private ValidationResult ValidateDeleteRequestData(ValidateDeleteRequest request)
        {
            var result = new ValidationResult { IsValid = true };

            if (request == null)
            {
                result.IsValid = false;
                result.Errors.Add("Request cannot be null");
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.EntityType))
            {
                result.IsValid = false;
                result.Errors.Add("Entity type is required");
            }

            if (string.IsNullOrWhiteSpace(request.EntityId))
            {
                result.IsValid = false;
                result.Errors.Add("Entity ID is required");
            }

            if (!IsValidEntityType(request.EntityType))
            {
                result.IsValid = false;
                result.Errors.Add("Invalid entity type");
            }

            return result;
        }

        /// <summary>
        /// Validate delete request data
        /// </summary>
        private ValidationResult ValidateDeleteRequestData(DeleteRequest request)
        {
            var result = new ValidationResult { IsValid = true };

            if (request == null)
            {
                result.IsValid = false;
                result.Errors.Add("Request cannot be null");
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.EntityType))
            {
                result.IsValid = false;
                result.Errors.Add("Entity type is required");
            }

            if (string.IsNullOrWhiteSpace(request.EntityId))
            {
                result.IsValid = false;
                result.Errors.Add("Entity ID is required");
            }

            if (!IsValidEntityType(request.EntityType))
            {
                result.IsValid = false;
                result.Errors.Add("Invalid entity type");
            }

            return result;
        }

        /// <summary>
        /// Validate restore request
        /// </summary>
        private ValidationResult ValidateRestoreRequest(RestoreRequest request)
        {
            var result = new ValidationResult { IsValid = true };

            if (request == null)
            {
                result.IsValid = false;
                result.Errors.Add("Request cannot be null");
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.EntityType))
            {
                result.IsValid = false;
                result.Errors.Add("Entity type is required");
            }

            if (string.IsNullOrWhiteSpace(request.EntityId))
            {
                result.IsValid = false;
                result.Errors.Add("Entity ID is required");
            }

            if (!IsValidEntityType(request.EntityType))
            {
                result.IsValid = false;
                result.Errors.Add("Invalid entity type");
            }

            if (request.TargetStatus < 1 || request.TargetStatus > 6)
            {
                result.IsValid = false;
                result.Errors.Add("Invalid target status");
            }

            return result;
        }

        /// <summary>
        /// Validate admin deleted items parameters
        /// </summary>
        private ValidationResult ValidateAdminDeletedItemsParameters(string search, string entityType, int page, int pageSize)
        {
            var result = new ValidationResult { IsValid = true };

            if (!string.IsNullOrEmpty(entityType) && !IsValidEntityTypeOrAll(entityType))
            {
                result.IsValid = false;
                result.Errors.Add("Invalid entity type");
            }

            if (page < 1)
            {
                result.Errors.Add("Page must be at least 1");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                result.Errors.Add("Page size must be between 1 and 100");
            }

            return result;
        }

        /// <summary>
        /// Check if entity type is valid
        /// </summary>
        private bool IsValidEntityType(string entityType)
        {
            var validTypes = new[] { "Course", "Chapter", "Lesson", "Account", "Enrollment" };
            return validTypes.Contains(entityType);
        }

        /// <summary>
        /// Check if entity type is valid or "All"
        /// </summary>
        private bool IsValidEntityTypeOrAll(string entityType)
        {
            return entityType == "All" || IsValidEntityType(entityType);
        }

        /// <summary>
        /// Format entities for JSON response
        /// </summary>
        private List<object> FormatEntitiesForResponse(object entities, string entityType)
        {
            var results = new List<object>();

            if (entities is System.Collections.IEnumerable enumerable)
            {
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
            }

            return results;
        }

        /// <summary>
        /// Create DeletedItemViewModel from anonymous object
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

        /// <summary>
        /// Get entity ID from entity object
        /// </summary>
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

        /// <summary>
        /// Get entity name from entity object
        /// </summary>
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

        /// <summary>
        /// Get entity deleted date from entity object
        /// </summary>
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

        #endregion

        #region Result Classes

        /// <summary>
        /// Result class for most controller actions
        /// </summary>
        public class SafeDeleteControllerResult
        {
            public bool IsSuccess { get; set; }
            public object? JsonResponse { get; set; }
        }

        /// <summary>
        /// Result class for AdminDeletedItems action
        /// </summary>
        public class AdminDeletedItemsControllerResult
        {
            public bool IsSuccess { get; set; }
            public bool ShouldRedirect { get; set; }
            public string? RedirectAction { get; set; }
            public string? RedirectController { get; set; }
            public string? TempDataErrorMessage { get; set; }
            public AdminDeletedItemsViewModel? ViewModel { get; set; }
        }

        /// <summary>
        /// Request/Response models
        /// </summary>
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

        /// <summary>
        /// Validation result class
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }

        #endregion
    }
}
