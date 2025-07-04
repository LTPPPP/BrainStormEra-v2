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
        public AdminUserViewModel? UserDetail { get; set; }
        public string UserId { get; set; } = string.Empty;

        public UserDetailModel(ILogger<UserDetailModel> logger, IAdminService adminService)
        {
            _logger = logger;
            _adminService = adminService;
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
                }

                // Use user ID directly to get user details
                _logger.LogInformation("Getting user details for userId: {UserId}", userId);

                UserDetail = await _adminService.GetUserDetailAsync(userId);

                if (UserDetail == null)
                {
                    _logger.LogWarning("User not found with userId: {UserId}", userId);
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToPage("/Admin/Users");
                }

                // Store user ID for UI
                UserId = userId;

                _logger.LogInformation("Admin {AdminName} viewed details for user {UserId} - {UserName}",
                    HttpContext.User?.Identity?.Name, userId, UserDetail.FullName);

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
                _logger.LogError(ex, "Error updating user points for user: {UserId}, pointsChange: {PointsChange}", userId, pointsChange);
                return new JsonResult(new { success = false, message = "An error occurred while updating user points" });
            }
        }
    }
}