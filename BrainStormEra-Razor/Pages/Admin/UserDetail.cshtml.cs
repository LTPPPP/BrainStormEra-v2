using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class UserDetailModel : PageModel
    {
        private readonly ILogger<UserDetailModel> _logger;
        private readonly IAdminService _adminService;
        private readonly IUrlHashService _urlHashService;

        public AdminUserViewModel? UserDetail { get; set; }
        public string EncodedUserId { get; set; } = string.Empty;

        public UserDetailModel(ILogger<UserDetailModel> logger, IAdminService adminService, IUrlHashService urlHashService)
        {
            _logger = logger;
            _adminService = adminService;
            _urlHashService = urlHashService;
        }

        public async Task<IActionResult> OnGetAsync(string userId)
        {
            try
            {
                _logger.LogInformation("UserDetail OnGet called with userId: {UserId}", userId);

                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User ID is required.";
                    return RedirectToPage("/Admin/Users");
                }                // Decode hash ID to real ID
                var realUserId = _urlHashService.GetRealId(userId);
                _logger.LogInformation("Decoded userId {EncodedId} to realUserId: {RealUserId}", userId, realUserId);

                // Get user details
                var allUsers = await _adminService.GetAllUsersAsync();
                _logger.LogInformation("Retrieved {UserCount} users from service", allUsers.Users.Count);

                UserDetail = allUsers.Users.FirstOrDefault(u => u.UserId == realUserId);

                if (UserDetail == null)
                {
                    _logger.LogWarning("User not found with realUserId: {RealUserId}. Available user IDs: {UserIds}",
                        realUserId, string.Join(", ", allUsers.Users.Select(u => u.UserId).Take(5)));
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToPage("/Admin/Users");
                }

                // Store encoded user ID for UI
                EncodedUserId = userId;

                _logger.LogInformation("Admin {AdminName} viewed details for user {UserId}",
                    HttpContext.User?.Identity?.Name, realUserId);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for userId: {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while loading user details.";
                return RedirectToPage("/Admin/Users");
            }
        }
        public async Task<IActionResult> OnPostUpdateUserStatusAsync(string userId, bool isBanned)
        {
            var realUserId = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required");
                }                // Decode hash ID to real ID
                realUserId = _urlHashService.GetRealId(userId);
                var result = await _adminService.UpdateUserStatusAsync(realUserId, isBanned);

                if (result)
                {
                    _logger.LogInformation("User status updated successfully by admin {AdminName} for user {UserId}",
                        HttpContext.User?.Identity?.Name, realUserId);

                    TempData["SuccessMessage"] = $"User has been {(isBanned ? "banned" : "unbanned")} successfully.";
                    return new JsonResult(new { success = true, message = "User status updated successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to update user status" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for user: {UserId}", realUserId);
                return new JsonResult(new { success = false, message = "An error occurred while updating user status" });
            }
        }

        public async Task<IActionResult> OnPostUpdateUserPointsAsync(string userId, decimal pointsChange)
        {
            var realUserId = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required");
                }                // Decode hash ID to real ID
                realUserId = _urlHashService.GetRealId(userId);
                var result = await _adminService.UpdateUserPointsAsync(realUserId, pointsChange);

                if (result)
                {
                    var action = pointsChange > 0 ? "added" : "subtracted";
                    var amount = Math.Abs(pointsChange);

                    _logger.LogInformation("User points updated successfully by admin {AdminName} for user {UserId}. Points {Action}: {Amount}",
                        HttpContext.User?.Identity?.Name, realUserId, action, amount);

                    TempData["SuccessMessage"] = $"{amount:N0} points have been {action} successfully.";
                    return new JsonResult(new { success = true, message = $"Points {action} successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to update user points" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user points for user: {UserId}, pointsChange: {PointsChange}", realUserId, pointsChange);
                return new JsonResult(new { success = false, message = "An error occurred while updating user points" });
            }
        }
    }
}