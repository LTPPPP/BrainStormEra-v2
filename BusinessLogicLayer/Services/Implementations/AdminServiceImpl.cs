using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BusinessLogicLayer.Services.Implementations
{
    #region Result Classes

    /// <summary>
    /// Result class for admin dashboard operations
    /// </summary>
    public class AdminDashboardResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public AdminDashboardViewModel? Data { get; set; }
    }

    /// <summary>
    /// Result class for admin statistics operations
    /// </summary>
    public class AdminStatisticsResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
    }

    /// <summary>
    /// Result class for recent users operations
    /// </summary>
    public class RecentUsersResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<UserViewModel>? Data { get; set; }
    }

    /// <summary>
    /// Result class for recent courses operations
    /// </summary>
    public class RecentCoursesResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CourseViewModel>? Data { get; set; }
    }

    /// <summary>
    /// Result class for total revenue operations
    /// </summary>
    public class TotalRevenueResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal Data { get; set; }
    }

    /// <summary>
    /// Result class for admin user management operations
    /// </summary>
    public class AdminUsersResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public AdminUsersViewModel? Data { get; set; }
    }

    /// <summary>
    /// Result class for admin course management operations
    /// </summary>
    public class AdminCoursesResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public AdminCoursesViewModel? Data { get; set; }
    }

    /// <summary>
    /// Result class for admin operations
    /// </summary>
    public class AdminOperationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion

    /// <summary>
    /// Interface for AdminServiceImpl business logic operations
    /// </summary>
    public interface IAdminServiceImpl
    {
        Task<AdminDashboardResult> GetAdminDashboardAsync(ClaimsPrincipal user);
        Task<AdminStatisticsResult> GetAdminStatisticsAsync(ClaimsPrincipal user);
        Task<RecentUsersResult> GetRecentUsersAsync(ClaimsPrincipal user, int count = 5);
        Task<RecentCoursesResult> GetRecentCoursesAsync(ClaimsPrincipal user, int count = 5);
        Task<TotalRevenueResult> GetTotalRevenueAsync(ClaimsPrincipal user);
        Task<AdminUsersResult> GetAllUsersAsync(ClaimsPrincipal user, string? search = null, string? roleFilter = null, int page = 1, int pageSize = 10);
        Task<AdminCoursesResult> GetAllCoursesAsync(ClaimsPrincipal user, string? search = null, string? categoryFilter = null, int page = 1, int pageSize = 10);
        Task<AdminOperationResult> UpdateUserStatusAsync(ClaimsPrincipal user, string userId, bool isBanned);
        Task<AdminOperationResult> DeleteUserAsync(ClaimsPrincipal user, string userId);
        Task<AdminOperationResult> UpdateCourseStatusAsync(ClaimsPrincipal user, string courseId, bool isApproved);
        Task<AdminOperationResult> DeleteCourseAsync(ClaimsPrincipal user, string courseId);
    }

    /// <summary>
    /// Business logic implementation for Admin service operations.
    /// Handles authentication, authorization, validation, and error handling.
    /// </summary>
    public class AdminServiceImpl
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminServiceImpl> _logger;

        public AdminServiceImpl(IAdminService adminService, ILogger<AdminServiceImpl> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        /// <summary>
        /// Get admin dashboard data with authentication and authorization
        /// </summary>
        public async Task<AdminDashboardResult> GetAdminDashboardAsync(ClaimsPrincipal user)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminDashboardResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Get user ID from claims
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new AdminDashboardResult
                    {
                        IsSuccess = false,
                        Message = "User ID not found in claims.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access admin dashboard
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminDashboardResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Get admin dashboard data
                var dashboard = await _adminService.GetAdminDashboardAsync(userId);

                return new AdminDashboardResult
                {
                    IsSuccess = true,
                    Message = "Admin dashboard data retrieved successfully.",
                    Data = dashboard
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Admin not found for user {UserId}", user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new AdminDashboardResult
                {
                    IsSuccess = false,
                    Message = "Admin account not found.",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard data for user {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new AdminDashboardResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving admin dashboard data.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get admin statistics with authorization
        /// </summary>
        public async Task<AdminStatisticsResult> GetAdminStatisticsAsync(ClaimsPrincipal user)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminStatisticsResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access statistics
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminStatisticsResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Get admin statistics
                var statistics = await _adminService.GetAdminStatisticsAsync();

                return new AdminStatisticsResult
                {
                    IsSuccess = true,
                    Message = "Admin statistics retrieved successfully.",
                    Data = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin statistics for user {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new AdminStatisticsResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving admin statistics.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get recent users with authorization and validation
        /// </summary>
        public async Task<RecentUsersResult> GetRecentUsersAsync(ClaimsPrincipal user, int count = 5)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new RecentUsersResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access user data
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new RecentUsersResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Validate count parameter
                if (count <= 0 || count > 100)
                {
                    return new RecentUsersResult
                    {
                        IsSuccess = false,
                        Message = "Count must be between 1 and 100.",
                        Data = null
                    };
                }

                // Get recent users
                var recentUsers = await _adminService.GetRecentUsersAsync(count);

                return new RecentUsersResult
                {
                    IsSuccess = true,
                    Message = $"Retrieved {recentUsers.Count} recent users successfully.",
                    Data = recentUsers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent users for admin {UserId}, count: {Count}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value, count);
                return new RecentUsersResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving recent users.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get recent courses with authorization and validation
        /// </summary>
        public async Task<RecentCoursesResult> GetRecentCoursesAsync(ClaimsPrincipal user, int count = 5)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new RecentCoursesResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access course data
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new RecentCoursesResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Validate count parameter
                if (count <= 0 || count > 100)
                {
                    return new RecentCoursesResult
                    {
                        IsSuccess = false,
                        Message = "Count must be between 1 and 100.",
                        Data = null
                    };
                }

                // Get recent courses
                var recentCourses = await _adminService.GetRecentCoursesAsync(count);

                return new RecentCoursesResult
                {
                    IsSuccess = true,
                    Message = $"Retrieved {recentCourses.Count} recent courses successfully.",
                    Data = recentCourses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent courses for admin {UserId}, count: {Count}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value, count);
                return new RecentCoursesResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving recent courses.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get total revenue with authorization
        /// </summary>
        public async Task<TotalRevenueResult> GetTotalRevenueAsync(ClaimsPrincipal user)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new TotalRevenueResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = 0
                    };
                }

                // Authorization check - only Admin role can access revenue data
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new TotalRevenueResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = 0
                    };
                }

                // Get total revenue
                var totalRevenue = await _adminService.GetTotalRevenueAsync();

                return new TotalRevenueResult
                {
                    IsSuccess = true,
                    Message = "Total revenue retrieved successfully.",
                    Data = totalRevenue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total revenue for admin {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new TotalRevenueResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving total revenue.",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Get all users with authorization and pagination
        /// </summary>
        public async Task<AdminUsersResult> GetAllUsersAsync(ClaimsPrincipal user, string? search = null, string? roleFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminUsersResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access user management
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminUsersResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Get all users with filtering and pagination
                var usersData = await _adminService.GetAllUsersAsync(search, roleFilter, page, pageSize);

                return new AdminUsersResult
                {
                    IsSuccess = true,
                    Message = "Users retrieved successfully.",
                    Data = usersData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for admin {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new AdminUsersResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving users.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get all courses with authorization and pagination
        /// </summary>
        public async Task<AdminCoursesResult> GetAllCoursesAsync(ClaimsPrincipal user, string? search = null, string? categoryFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminCoursesResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated.",
                        Data = null
                    };
                }

                // Authorization check - only Admin role can access course management
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminCoursesResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required.",
                        Data = null
                    };
                }

                // Get all courses with filtering and pagination
                var coursesData = await _adminService.GetAllCoursesAsync(search, categoryFilter, page, pageSize);

                return new AdminCoursesResult
                {
                    IsSuccess = true,
                    Message = "Courses retrieved successfully.",
                    Data = coursesData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving courses for admin {UserId}",
                    user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return new AdminCoursesResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving courses.",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Update user status with authorization
        /// </summary>
        public async Task<AdminOperationResult> UpdateUserStatusAsync(ClaimsPrincipal user, string userId, bool isBanned)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                // Authorization check - only Admin role can update user status
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required."
                    };
                }

                // Update user status
                var success = await _adminService.UpdateUserStatusAsync(userId, isBanned);

                if (success)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = true,
                        Message = $"User status updated successfully."
                    };
                }
                else
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Failed to update user status."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for user {UserId}", userId);
                return new AdminOperationResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while updating user status."
                };
            }
        }

        /// <summary>
        /// Delete user with authorization
        /// </summary>
        public async Task<AdminOperationResult> DeleteUserAsync(ClaimsPrincipal user, string userId)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                // Authorization check - only Admin role can delete users
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required."
                    };
                }

                // Prevent admin from deleting themselves
                var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Cannot delete your own account."
                    };
                }

                // Delete user
                var success = await _adminService.DeleteUserAsync(userId);

                if (success)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = true,
                        Message = "User deleted successfully."
                    };
                }
                else
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Failed to delete user."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return new AdminOperationResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while deleting user."
                };
            }
        }

        /// <summary>
        /// Update course status with authorization
        /// </summary>
        public async Task<AdminOperationResult> UpdateCourseStatusAsync(ClaimsPrincipal user, string courseId, bool isApproved)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                // Authorization check - only Admin role can update course status
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required."
                    };
                }

                // Update course status
                var success = await _adminService.UpdateCourseStatusAsync(courseId, isApproved);

                if (success)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = true,
                        Message = "Course status updated successfully."
                    };
                }
                else
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Failed to update course status."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course status for course {CourseId}", courseId);
                return new AdminOperationResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while updating course status."
                };
            }
        }

        /// <summary>
        /// Delete course with authorization
        /// </summary>
        public async Task<AdminOperationResult> DeleteCourseAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                // Authentication check
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                // Authorization check - only Admin role can delete courses
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Access denied. Admin role required."
                    };
                }

                // Delete course
                var success = await _adminService.DeleteCourseAsync(courseId);

                if (success)
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = true,
                        Message = "Course deleted successfully."
                    };
                }
                else
                {
                    return new AdminOperationResult
                    {
                        IsSuccess = false,
                        Message = "Failed to delete course."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", courseId);
                return new AdminOperationResult
                {
                    IsSuccess = false,
                    Message = "An error occurred while deleting course."
                };
            }
        }
    }
}







