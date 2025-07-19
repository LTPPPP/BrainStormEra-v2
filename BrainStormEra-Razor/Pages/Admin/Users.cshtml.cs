using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

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

                _logger.LogInformation("Loading users page for admin: {AdminName}, UserId: {UserId}", AdminName, UserId);

                // Load users data with pagination and filters
                if (!string.IsNullOrEmpty(UserId))
                {
                    UsersData = await _adminService.GetAllUsersAsync(
                        search: SearchQuery,
                        roleFilter: RoleFilter,
                        page: CurrentPage,
                        pageSize: PageSize
                    );

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

                    _logger.LogInformation("Loaded {TotalUsers} users for admin: {AdminName}", UsersData.TotalUsers, AdminName);
                }
                else
                {
                    _logger.LogWarning("User ID not found in claims for admin: {AdminName}", AdminName);
                    UsersData = new AdminUsersViewModel();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users data for admin: {UserId}", UserId);
                UsersData = new AdminUsersViewModel();
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUpdateUserStatusAsync([FromBody] UpdateUserStatusRequest request)
        {
            try
            {
                _logger.LogInformation("Received ban/unban request for user: {UserId}, isBanned: {IsBanned} by admin: {AdminName}",
                    request?.UserId, request?.IsBanned, HttpContext.User?.Identity?.Name);

                // Validate request
                if (request == null)
                {
                    _logger.LogWarning("UpdateUserStatus request is null");
                    return new JsonResult(new { success = false, message = "Invalid request" });
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    _logger.LogWarning("UpdateUserStatus request UserId is null or empty");
                    return new JsonResult(new { success = false, message = "User ID is required" });
                }

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("UpdateUserStatus model validation failed: {Errors}", string.Join(", ", errors));
                    return new JsonResult(new { success = false, message = "Invalid request data" });
                }

                // Check if admin is trying to ban themselves
                var currentUserId = HttpContext.User?.FindFirst("UserId")?.Value;
                if (request.UserId == currentUserId && request.IsBanned)
                {
                    _logger.LogWarning("Admin {AdminName} attempted to ban themselves", HttpContext.User?.Identity?.Name);
                    return new JsonResult(new { success = false, message = "You cannot ban yourself" });
                }

                // Update user status
                var result = await _adminService.UpdateUserStatusAsync(request.UserId, request.IsBanned);

                if (result)
                {
                    var action = request.IsBanned ? "banned" : "unbanned";
                    _logger.LogInformation("User {UserId} {Action} successfully by admin {AdminName}",
                        request.UserId, action, HttpContext.User?.Identity?.Name);

                    return new JsonResult(new
                    {
                        success = true,
                        message = $"User {action} successfully"
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to update user status for user: {UserId}", request.UserId);
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Failed to update user status. Please try again."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for user: {UserId}", request?.UserId);
                return new JsonResult(new
                {
                    success = false,
                    message = "An error occurred while updating user status. Please try again."
                });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUpdateUserPointsAsync([FromBody] UpdateUserPointsRequest request)
        {
            try
            {
                _logger.LogInformation("Received points update request for user: {UserId}, pointsChange: {PointsChange} by admin: {AdminName}",
                    request?.UserId, request?.PointsChange, HttpContext.User?.Identity?.Name);

                // Validate request
                if (request == null)
                {
                    _logger.LogWarning("UpdateUserPoints request is null");
                    return new JsonResult(new { success = false, message = "Invalid request" });
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    _logger.LogWarning("UpdateUserPoints request UserId is null or empty");
                    return new JsonResult(new { success = false, message = "User ID is required" });
                }

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("UpdateUserPoints model validation failed: {Errors}", string.Join(", ", errors));
                    return new JsonResult(new { success = false, message = "Invalid request data" });
                }

                // Validate points change
                if (request.PointsChange == 0)
                {
                    _logger.LogWarning("UpdateUserPoints pointsChange is zero");
                    return new JsonResult(new { success = false, message = "Points change cannot be zero" });
                }

                // Update user points
                var result = await _adminService.UpdateUserPointsAsync(request.UserId, request.PointsChange);

                if (result)
                {
                    var action = request.PointsChange > 0 ? "added" : "subtracted";
                    var amount = Math.Abs(request.PointsChange);

                    _logger.LogInformation("User points updated successfully by admin {AdminName} for user {UserId}. Points {Action}: {Amount}",
                        HttpContext.User?.Identity?.Name, request.UserId, action, amount);

                    return new JsonResult(new
                    {
                        success = true,
                        message = $"{amount} points {action} successfully"
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to update user points for user: {UserId}", request.UserId);
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Failed to update user points. Please try again."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user points for user: {UserId}, pointsChange: {PointsChange}",
                    request?.UserId, request?.PointsChange);
                return new JsonResult(new
                {
                    success = false,
                    message = "An error occurred while updating user points. Please try again."
                });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostChangeUserRoleAsync([FromBody] ChangeUserRoleRequest request)
        {
            try
            {
                _logger.LogInformation("Received role change request for user: {UserId}, newRole: {NewRole} by admin: {AdminName}",
                    request?.UserId, request?.NewRole, HttpContext.User?.Identity?.Name);

                // Validate request
                if (request == null)
                {
                    _logger.LogWarning("ChangeUserRole request is null");
                    return new JsonResult(new { success = false, message = "Invalid request" });
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    _logger.LogWarning("ChangeUserRole request UserId is null or empty");
                    return new JsonResult(new { success = false, message = "User ID is required" });
                }

                if (string.IsNullOrEmpty(request.NewRole))
                {
                    _logger.LogWarning("ChangeUserRole request NewRole is null or empty");
                    return new JsonResult(new { success = false, message = "New role is required" });
                }

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("ChangeUserRole model validation failed: {Errors}", string.Join(", ", errors));
                    return new JsonResult(new { success = false, message = "Invalid request data" });
                }

                // Check if admin is trying to change their own role
                var currentUserId = HttpContext.User?.FindFirst("UserId")?.Value;
                if (request.UserId == currentUserId)
                {
                    _logger.LogWarning("Admin {AdminName} attempted to change their own role", HttpContext.User?.Identity?.Name);
                    return new JsonResult(new { success = false, message = "You cannot change your own role" });
                }

                // Validate the new role
                var validRoles = new[] { "learner", "instructor", "admin" };
                if (!validRoles.Contains(request.NewRole, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid role attempted: {NewRole} for user {UserId}", request.NewRole, request.UserId);
                    return new JsonResult(new { success = false, message = "Invalid role specified" });
                }

                // Change user role
                var result = await _adminService.ChangeUserRoleAsync(request.UserId, request.NewRole);

                if (result)
                {
                    _logger.LogInformation("User {UserId} role changed to {NewRole} successfully by admin {AdminName}",
                        request.UserId, request.NewRole, HttpContext.User?.Identity?.Name);

                    return new JsonResult(new
                    {
                        success = true,
                        message = $"User role changed to {request.NewRole} successfully"
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to change user role for user: {UserId} to {NewRole}", request.UserId, request.NewRole);
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Failed to change user role. User may have existing content that prevents role change."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user role for user: {UserId} to {NewRole}", request?.UserId, request?.NewRole);
                return new JsonResult(new
                {
                    success = false,
                    message = "An error occurred while changing user role. Please try again."
                });
            }
        }

        // Request models
        public class UpdateUserStatusRequest
        {
            [Required]
            public string UserId { get; set; } = string.Empty;

            [Required]
            public bool IsBanned { get; set; }
        }

        public class UpdateUserPointsRequest
        {
            [Required]
            public string UserId { get; set; } = string.Empty;

            [Required]
            [Range(-999999, 999999, ErrorMessage = "Points change must be between -999999 and 999999")]
            public decimal PointsChange { get; set; }
        }

        public class ChangeUserRoleRequest
        {
            [Required]
            public string UserId { get; set; } = string.Empty;

            [Required]
            [StringLength(20)]
            public string NewRole { get; set; } = string.Empty;
        }
    }
}