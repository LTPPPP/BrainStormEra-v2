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
                if (request == null)
                {
                    return BadRequest("Achievement data is required");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return new JsonResult(new { success = false, message = "Invalid data", errors = errors });
                }

                var result = await _adminService.CreateAchievementAsync(request, UserId);

                if (result)
                {
                    _logger.LogInformation("Achievement created successfully by admin {AdminName}: {AchievementName}",
                        HttpContext.User?.Identity?.Name, request.AchievementName);
                    return new JsonResult(new { success = true, message = "Achievement created successfully" });
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
                if (request == null)
                {
                    return BadRequest("Achievement data is required");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return new JsonResult(new { success = false, message = "Invalid data", errors = errors });
                }

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
    }

    // Helper request models for the page
    public class DeleteAchievementRequest
    {
        public string AchievementId { get; set; } = "";
    }
}