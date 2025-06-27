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
        public string? StatusFilter { get; set; }

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
                    );                    // No need to encode user IDs anymore

                    // Apply status filter in frontend since backend doesn't support it yet
                    if (!string.IsNullOrEmpty(StatusFilter))
                    {
                        var filteredUsers = StatusFilter.ToLower() switch
                        {
                            "active" => UsersData.Users.Where(u => !u.IsBanned).ToList(),
                            "banned" => UsersData.Users.Where(u => u.IsBanned).ToList(),
                            _ => UsersData.Users.ToList()
                        };

                        UsersData.Users = filteredUsers;
                        UsersData.TotalUsers = filteredUsers.Count;
                        UsersData.TotalPages = (int)Math.Ceiling((double)filteredUsers.Count / PageSize);
                    }
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

                // Use user ID directly
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

        public async Task<IActionResult> OnPostUpdateUserPointsAsync(string userId, decimal pointsChange)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required");
                }

                // Use user ID directly
                var result = await _adminService.UpdateUserPointsAsync(userId, pointsChange);

                if (result)
                {
                    var action = pointsChange > 0 ? "added" : "subtracted";
                    var amount = Math.Abs(pointsChange);

                    _logger.LogInformation("User points updated successfully by admin {AdminName} for user {UserId}. Points {Action}: {Amount}",
                        HttpContext.User?.Identity?.Name, userId, action, amount);

                    return new JsonResult(new { success = true, message = $"Points {action} successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to update user points" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user points for user: {UserId}, pointsChange: {PointsChange}", userId, pointsChange);
                return new JsonResult(new { success = false, message = "An error occurred while updating user points" });
            }
        }
    }
}