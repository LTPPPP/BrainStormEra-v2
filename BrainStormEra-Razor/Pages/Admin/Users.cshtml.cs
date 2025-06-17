using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class UsersModel : PageModel
    {
        private readonly ILogger<UsersModel> _logger;
        private readonly IAdminService _adminService;

        public string? AdminName { get; set; }
        public string? UserId { get; set; }
        public AdminUsersViewModel UsersData { get; set; } = new AdminUsersViewModel();

        // Filter and pagination properties
        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RoleFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public UsersModel(ILogger<UsersModel> logger, IAdminService adminService)
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

                // Load users data with pagination and filters
                if (!string.IsNullOrEmpty(UserId))
                {
                    UsersData = await _adminService.GetAllUsersAsync(
                        search: SearchQuery,
                        roleFilter: RoleFilter,
                        page: CurrentPage,
                        pageSize: PageSize
                    );
                }
                else
                {
                    _logger.LogWarning("User ID not found in claims");
                    UsersData = new AdminUsersViewModel();
                }

                _logger.LogInformation("Admin users page accessed by: {AdminName} at {AccessTime}", AdminName, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users data for admin: {UserId}", UserId);
                UsersData = new AdminUsersViewModel();
            }
        }

        public async Task<IActionResult> OnPostUpdateUserStatusAsync(string userId, bool isBanned)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required");
                }

                var result = await _adminService.UpdateUserStatusAsync(userId, isBanned);

                if (result)
                {
                    _logger.LogInformation("User status updated successfully by admin {AdminName} for user {UserId}",
                        HttpContext.User?.Identity?.Name, userId);
                    return new JsonResult(new { success = true, message = "User status updated successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to update user status" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for user: {UserId}", userId);
                return new JsonResult(new { success = false, message = "An error occurred while updating user status" });
            }
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required");
                }

                var result = await _adminService.DeleteUserAsync(userId);

                if (result)
                {
                    _logger.LogInformation("User deleted successfully by admin {AdminName} for user {UserId}",
                        HttpContext.User?.Identity?.Name, userId);
                    return new JsonResult(new { success = true, message = "User deleted successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to delete user" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return new JsonResult(new { success = false, message = "An error occurred while deleting user" });
            }
        }
    }
}