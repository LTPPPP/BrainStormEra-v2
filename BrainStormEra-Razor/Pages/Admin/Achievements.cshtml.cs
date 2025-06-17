using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class AchievementsModel : PageModel
    {
        private readonly ILogger<AchievementsModel> _logger;
        private readonly IAdminService _adminService;

        public string? AdminName { get; set; }
        public string? UserId { get; set; }
        public AdminAchievementsViewModel AchievementsData { get; set; } = new AdminAchievementsViewModel();

        // Filter and pagination properties
        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }



        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 12;

        public AchievementsModel(ILogger<AchievementsModel> logger, IAdminService adminService)
        {
            _logger = logger;
            _adminService = adminService;
        }

        public async Task OnGetAsync()
        {
            try
            {
                AdminName = HttpContext.User?.Identity?.Name ?? "Admin";
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";

                // Load achievements data with pagination and filters
                if (!string.IsNullOrEmpty(UserId))
                {
                    AchievementsData = await _adminService.GetAllAchievementsAsync(
                        search: SearchQuery,
                        typeFilter: TypeFilter,
                        pointsFilter: null,
                        page: CurrentPage,
                        pageSize: PageSize
                    );
                }
                else
                {
                    _logger.LogWarning("User ID not found in claims");
                    AchievementsData = new AdminAchievementsViewModel();
                }

                _logger.LogInformation("Admin achievements page accessed by: {AdminName} at {AccessTime}", AdminName, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading achievements data for admin: {UserId}", UserId);
                AchievementsData = new AdminAchievementsViewModel();
            }
        }

        public async Task<IActionResult> OnPostCreateAchievementAsync([FromBody] CreateAchievementRequest request)
        {
            try
            {
                // Set UserId from claims
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";
                _logger.LogInformation("Create achievement request received from UserId: {UserId}, Request: {@Request}", UserId, request);

                if (string.IsNullOrEmpty(UserId))
                {
                    _logger.LogWarning("UserId not found in claims");
                    return new JsonResult(new { success = false, message = "User not authenticated" }) { StatusCode = 401 };
                }

                if (request == null)
                {
                    _logger.LogWarning("Request is null");
                    return BadRequest("Achievement data is required");
                }

                _logger.LogInformation("Request data: Name={Name}, Type={Type}, Icon={Icon}, Description={Description}",
    request.AchievementName, request.AchievementType, request.AchievementIcon, request.AchievementDescription);

                // Validate achievement type against allowed values
                var allowedTypes = new[] { "course_completion", "quiz_master", "streak", "first_course", "instructor", "student_engagement" };
                if (!allowedTypes.Contains(request.AchievementType?.ToLower()))
                {
                    _logger.LogWarning("Invalid achievement type: {Type}. Allowed types: {AllowedTypes}",
                        request.AchievementType, string.Join(", ", allowedTypes));
                    return new JsonResult(new { success = false, message = $"Invalid achievement type. Allowed types: {string.Join(", ", allowedTypes)}" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));

                    // Log detailed validation errors
                    foreach (var modelError in ModelState)
                    {
                        _logger.LogWarning("Field {Field}: {Errors}", modelError.Key,
                            string.Join(", ", modelError.Value.Errors.Select(e => e.ErrorMessage)));
                    }

                    return new JsonResult(new { success = false, message = "Invalid data", errors = errors });
                }

                // Normalize achievement type to lowercase
                request.AchievementType = request.AchievementType?.ToLower();

                var (success, achievementId) = await _adminService.CreateAchievementAsync(request, UserId);

                if (success)
                {
                    _logger.LogInformation("Achievement created successfully by admin {AdminName}: {AchievementName}, ID: {AchievementId}",
                        HttpContext.User?.Identity?.Name, request.AchievementName, achievementId);
                    return new JsonResult(new { success = true, message = "Achievement created successfully", achievementId = achievementId });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to create achievement" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating achievement: {AchievementName}", request?.AchievementName);
                return new JsonResult(new { success = false, message = "An error occurred while creating the achievement" });
            }
        }

        public async Task<IActionResult> OnPostUpdateAchievementAsync([FromBody] UpdateAchievementRequest request)
        {
            try
            {
                // Set UserId from claims
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";

                if (request == null)
                {
                    return BadRequest("Achievement data is required");
                }

                // Validate achievement type against allowed values
                var allowedTypes = new[] { "course_completion", "quiz_master", "streak", "first_course", "instructor", "student_engagement" };
                if (!allowedTypes.Contains(request.AchievementType?.ToLower()))
                {
                    _logger.LogWarning("Invalid achievement type: {Type}. Allowed types: {AllowedTypes}",
                        request.AchievementType, string.Join(", ", allowedTypes));
                    return new JsonResult(new { success = false, message = $"Invalid achievement type. Allowed types: {string.Join(", ", allowedTypes)}" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return new JsonResult(new { success = false, message = "Invalid data", errors = errors });
                }

                // Normalize achievement type to lowercase
                request.AchievementType = request.AchievementType?.ToLower();

                var result = await _adminService.UpdateAchievementAsync(request, UserId);

                if (result)
                {
                    _logger.LogInformation("Achievement updated successfully by admin {AdminName}: {AchievementId}",
                        HttpContext.User?.Identity?.Name, request.AchievementId);
                    return new JsonResult(new { success = true, message = "Achievement updated successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to update achievement" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating achievement: {AchievementId}", request?.AchievementId);
                return new JsonResult(new { success = false, message = "An error occurred while updating the achievement" });
            }
        }

        public async Task<IActionResult> OnPostDeleteAchievementAsync([FromBody] DeleteAchievementRequest request)
        {
            try
            {
                // Set UserId from claims
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";

                if (request == null || string.IsNullOrEmpty(request.AchievementId))
                {
                    return BadRequest("Achievement ID is required");
                }

                var result = await _adminService.DeleteAchievementAsync(request.AchievementId, UserId);

                if (result)
                {
                    _logger.LogInformation("Achievement deleted successfully by admin {AdminName}: {AchievementId}",
                        HttpContext.User?.Identity?.Name, request.AchievementId);
                    return new JsonResult(new { success = true, message = "Achievement deleted successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to delete achievement" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting achievement: {AchievementId}", request?.AchievementId);
                return new JsonResult(new { success = false, message = "An error occurred while deleting the achievement" });
            }
        }

        public async Task<IActionResult> OnGetAchievementDetailsAsync(string achievementId)
        {
            try
            {
                if (string.IsNullOrEmpty(achievementId))
                {
                    return BadRequest("Achievement ID is required");
                }

                var achievement = await _adminService.GetAchievementByIdAsync(achievementId);

                if (achievement == null)
                {
                    return NotFound("Achievement not found");
                }

                return new JsonResult(new { success = true, data = achievement });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement details: {AchievementId}", achievementId);
                return new JsonResult(new { success = false, message = "An error occurred while fetching achievement details" });
            }
        }

        public async Task<IActionResult> OnPostUploadAchievementIconAsync(IFormFile iconFile, string achievementId)
        {
            try
            {
                // Set UserId from claims
                UserId = HttpContext.User?.FindFirst("UserId")?.Value ?? "";

                if (iconFile == null || string.IsNullOrEmpty(achievementId))
                {
                    return BadRequest("Icon file and achievement ID are required");
                }

                var result = await _adminService.UploadAchievementIconAsync(iconFile, achievementId, UserId);

                if (result.Success)
                {
                    _logger.LogInformation("Achievement icon uploaded successfully by admin {AdminName}: {AchievementId}",
                        HttpContext.User?.Identity?.Name, achievementId);
                    return new JsonResult(new { success = true, iconPath = result.IconPath, message = "Icon uploaded successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading achievement icon: {AchievementId}", achievementId);
                return new JsonResult(new { success = false, message = "An error occurred while uploading the icon" });
            }
        }
    }

    // Helper request models for the page
    public class DeleteAchievementRequest
    {
        public string AchievementId { get; set; } = "";
    }
}