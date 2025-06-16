using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles complex business logic for Home operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class HomeServiceImpl
    {
        private readonly IHomeService _homeService;
        private readonly ILogger<HomeServiceImpl> _logger;

        public HomeServiceImpl(
            IHomeService homeService,
            ILogger<HomeServiceImpl> logger)
        {
            _homeService = homeService;
            _logger = logger;
        }

        #region Home Page Operations

        /// <summary>
        /// Get guest home page with error handling
        /// </summary>
        public async Task<GuestHomePageResult> GetGuestHomePageAsync()
        {
            try
            {
                var viewModel = await _homeService.GetGuestHomePageAsync();

                return new GuestHomePageResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading guest home page");
                return new GuestHomePageResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the home page. Please try again later.",
                    ViewModel = new HomePageGuestViewModel()
                };
            }
        }

        #endregion

        #region Learner Dashboard Operations

        /// <summary>
        /// Get learner dashboard with authentication and authorization
        /// </summary>
        public async Task<LearnerDashboardResult> GetLearnerDashboardAsync(ClaimsPrincipal user)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new LearnerDashboardResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var userRole = user.FindFirst("UserRole")?.Value;
                if (!string.Equals(userRole, "learner", StringComparison.OrdinalIgnoreCase))
                {
                    return new LearnerDashboardResult
                    {
                        Success = false,
                        ErrorMessage = "Access denied. You don't have permission to access the learner dashboard.",
                        RedirectToUserDashboard = true
                    };
                }

                var viewModel = await _homeService.GetLearnerDashboardAsync(userId);

                return new LearnerDashboardResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "User not found while loading learner dashboard");
                return new LearnerDashboardResult
                {
                    Success = false,
                    ErrorMessage = "User account not found. Please log in again.",
                    RedirectAction = "Index",
                    RedirectController = "Login"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading learner dashboard for user: {UserId}", user.FindFirst("UserId")?.Value);
                return new LearnerDashboardResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the dashboard. Please try again.",
                    RedirectAction = "Index",
                    RedirectController = "Home"
                };
            }
        }

        #endregion

        #region Instructor Dashboard Operations

        /// <summary>
        /// Get instructor dashboard with authentication and authorization
        /// </summary>
        public async Task<InstructorDashboardResult> GetInstructorDashboardAsync(ClaimsPrincipal user)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new InstructorDashboardResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }
                var userRole = user.FindFirst("UserRole")?.Value;
                if (!string.Equals(userRole, "instructor", StringComparison.OrdinalIgnoreCase))
                {
                    return new InstructorDashboardResult
                    {
                        Success = false,
                        ErrorMessage = "Access denied. You don't have permission to access the instructor dashboard.",
                        RedirectToUserDashboard = true
                    };
                }

                var viewModel = await _homeService.GetInstructorDashboardAsync(userId);

                return new InstructorDashboardResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "User not found while loading instructor dashboard");
                return new InstructorDashboardResult
                {
                    Success = false,
                    ErrorMessage = "Instructor account not found. Please log in again.",
                    RedirectAction = "Index",
                    RedirectController = "Login"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading instructor dashboard for user: {UserId}", user.FindFirst("UserId")?.Value);
                return new InstructorDashboardResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the dashboard. Please try again.",
                    RedirectAction = "Index",
                    RedirectController = "Home"
                };
            }
        }

        /// <summary>
        /// Get income data for instructor with authorization
        /// </summary>
        public async Task<IncomeDataResult> GetIncomeDataAsync(ClaimsPrincipal user, int days = 30)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new IncomeDataResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }
                var userRole = user.FindFirst("UserRole")?.Value;
                if (!string.Equals(userRole, "instructor", StringComparison.OrdinalIgnoreCase))
                {
                    return new IncomeDataResult
                    {
                        Success = false,
                        ErrorMessage = "Access denied. Only instructors can view income data."
                    };
                }

                if (days <= 0 || days > 365)
                {
                    return new IncomeDataResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid date range. Days must be between 1 and 365."
                    };
                }

                var incomeData = await _homeService.GetIncomeDataAsync(userId, days);

                // Fill in missing dates with 0 income
                var result = new List<dynamic>();
                for (int i = days - 1; i >= 0; i--)
                {
                    var date = DateTime.Now.AddDays(-i).Date;
                    var income = incomeData.FirstOrDefault(x => ((dynamic)x).Date == date);
                    result.Add(new
                    {
                        Date = date.ToString(days <= 7 ? "MMM dd" : days <= 30 ? "MMM dd" : "MMM yyyy"),
                        Amount = income != null ? ((dynamic)income).Amount : 0
                    });
                }

                return new IncomeDataResult
                {
                    Success = true,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting income data for instructor: {UserId}", user.FindFirst("UserId")?.Value);
                return new IncomeDataResult
                {
                    Success = false,
                    ErrorMessage = "Error loading income data"
                };
            }
        }

        #endregion

        #region Utility Operations

        /// <summary>
        /// Get recommended courses with error handling
        /// </summary>
        public async Task<RecommendedCoursesResult> GetRecommendedCoursesAsync()
        {
            try
            {
                var recommendedCourses = await _homeService.GetRecommendedCoursesAsync();

                return new RecommendedCoursesResult
                {
                    Success = true,
                    Courses = recommendedCourses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recommended courses");
                return new RecommendedCoursesResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading recommended courses.",
                    Courses = new List<dynamic>()
                };
            }
        }

        /// <summary>
        /// Check database connection with error handling
        /// </summary>
        public async Task<DatabaseConnectionResult> CheckDatabaseConnectionAsync()
        {
            try
            {
                var isConnected = await _homeService.IsDatabaseConnectedAsync();

                return new DatabaseConnectionResult
                {
                    Success = true,
                    IsConnected = isConnected,
                    Message = isConnected ? "Database is connected" : "Database is not connected"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connection");
                return new DatabaseConnectionResult
                {
                    Success = false,
                    IsConnected = false,
                    Message = "Error checking database connection"
                };
            }
        }

        #endregion

        #region Controller Business Logic Methods

        /// <summary>
        /// Handle Index action with authentication check and redirection logic
        /// </summary>
        public async Task<IndexControllerResult> HandleIndexAsync(ClaimsPrincipal user)
        {
            var result = new IndexControllerResult();

            try
            {
                // Check authentication and determine redirect
                var authResult = CheckAuthenticationAndGetRedirect(user);
                if (authResult.ShouldRedirect)
                {
                    result.ShouldRedirect = true;
                    result.RedirectAction = authResult.RedirectAction;
                    result.RedirectController = authResult.RedirectController;
                    result.TempDataMessage = authResult.TempDataMessage;
                    return result;
                }

                // Get guest home page
                var guestResult = await GetGuestHomePageAsync();
                if (!guestResult.Success)
                {
                    result.ViewBagError = guestResult.ErrorMessage;
                    result.ViewModel = new HomePageGuestViewModel();
                }
                else
                {
                    result.ViewModel = guestResult.ViewModel;
                }

                result.ViewBagIsAuthenticated = false;
                result.IsSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in HandleIndexAsync");
                result.ViewBagError = "An unexpected error occurred. Please try again later.";
                result.ViewModel = new HomePageGuestViewModel();
                result.IsSuccess = true; // Still show view with error message
                return result;
            }
        }

        /// <summary>
        /// Handle LearnerDashboard action with error handling and redirection
        /// </summary>
        public async Task<DashboardControllerResult> HandleLearnerDashboardAsync(ClaimsPrincipal user)
        {
            var result = new DashboardControllerResult();

            try
            {
                var dashboardResult = await GetLearnerDashboardAsync(user);

                if (!dashboardResult.Success)
                {
                    result.TempDataErrorMessage = dashboardResult.ErrorMessage;

                    if (!string.IsNullOrEmpty(dashboardResult.RedirectAction) && !string.IsNullOrEmpty(dashboardResult.RedirectController))
                    {
                        result.ShouldRedirect = true;
                        result.RedirectAction = dashboardResult.RedirectAction;
                        result.RedirectController = dashboardResult.RedirectController;
                    }
                    else if (dashboardResult.RedirectToUserDashboard)
                    {
                        result.ShouldRedirectToUserDashboard = true;
                    }
                    else
                    {
                        result.ShouldRedirect = true;
                        result.RedirectAction = "Index";
                        result.RedirectController = "Home";
                    }
                    return result;
                }

                result.IsSuccess = true;
                result.ViewModel = dashboardResult.ViewModel;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in HandleLearnerDashboardAsync");
                result.TempDataErrorMessage = "An unexpected error occurred while loading the dashboard.";
                result.ShouldRedirect = true;
                result.RedirectAction = "Index";
                result.RedirectController = "Home";
                return result;
            }
        }

        /// <summary>
        /// Handle InstructorDashboard action with error handling and redirection
        /// </summary>
        public async Task<DashboardControllerResult> HandleInstructorDashboardAsync(ClaimsPrincipal user)
        {
            var result = new DashboardControllerResult();

            try
            {
                var dashboardResult = await GetInstructorDashboardAsync(user);

                if (!dashboardResult.Success)
                {
                    result.TempDataErrorMessage = dashboardResult.ErrorMessage;

                    if (!string.IsNullOrEmpty(dashboardResult.RedirectAction) && !string.IsNullOrEmpty(dashboardResult.RedirectController))
                    {
                        result.ShouldRedirect = true;
                        result.RedirectAction = dashboardResult.RedirectAction;
                        result.RedirectController = dashboardResult.RedirectController;
                    }
                    else if (dashboardResult.RedirectToUserDashboard)
                    {
                        result.ShouldRedirectToUserDashboard = true;
                    }
                    else
                    {
                        result.ShouldRedirect = true;
                        result.RedirectAction = "Index";
                        result.RedirectController = "Home";
                    }
                    return result;
                }

                result.IsSuccess = true;
                result.ViewModel = dashboardResult.ViewModel;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in HandleInstructorDashboardAsync");
                result.TempDataErrorMessage = "An unexpected error occurred while loading the dashboard.";
                result.ShouldRedirect = true;
                result.RedirectAction = "Index";
                result.RedirectController = "Home";
                return result;
            }
        }

        /// <summary>
        /// Handle GetIncomeData action with JSON response formatting
        /// </summary>
        public async Task<IncomeDataControllerResult> HandleGetIncomeDataAsync(ClaimsPrincipal user, int days = 30)
        {
            var result = new IncomeDataControllerResult();

            try
            {
                var incomeResult = await GetIncomeDataAsync(user, days);

                if (!incomeResult.Success)
                {
                    result.JsonResponse = new { success = false, message = incomeResult.ErrorMessage };
                    return result;
                }

                result.JsonResponse = new { success = true, data = incomeResult.Data };
                result.IsSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in HandleGetIncomeDataAsync");
                result.JsonResponse = new { success = false, message = "An unexpected error occurred." };
                return result;
            }
        }

        /// <summary>
        /// Get user role-based dashboard redirect information
        /// </summary>
        public UserDashboardRedirectResult GetUserDashboardRedirect(ClaimsPrincipal user)
        {
            var result = new UserDashboardRedirectResult();

            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                var userRole = user.FindFirst("UserRole")?.Value;
                var username = user.Identity?.Name; _logger.LogInformation("GetUserDashboardRedirect - CurrentUserRole: '{Role}', UserId: {UserId}",
                    userRole, userId);
                if (string.Equals(userRole, "instructor", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Redirecting instructor user to Instructor Dashboard");
                    result.RedirectAction = "InstructorDashboard";
                    result.RedirectController = "Home";
                }
                else if (string.Equals(userRole, "learner", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Redirecting learner user to Learner Dashboard");
                    result.RedirectAction = "LearnerDashboard";
                    result.RedirectController = "Home";
                }
                else
                {
                    _logger.LogWarning("Invalid user role detected: '{Role}' for user: {UserId}", userRole, userId);
                    result.HasError = true;
                    result.ErrorMessage = "Invalid login attempt.";
                    result.RedirectAction = "Index";
                    result.RedirectController = "Login";
                }

                result.IsSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserDashboardRedirect");
                result.HasError = true;
                result.ErrorMessage = "An error occurred while determining dashboard redirect.";
                result.RedirectAction = "Index";
                result.RedirectController = "Login";
                return result;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Check authentication and determine redirect logic
        /// </summary>
        private AuthenticationCheckResult CheckAuthenticationAndGetRedirect(ClaimsPrincipal user)
        {
            var result = new AuthenticationCheckResult();

            if (user?.Identity?.IsAuthenticated != true)
            {
                return result; // Not authenticated, continue with guest view
            }

            var username = user.Identity?.Name;
            var userRole = user.FindFirst("UserRole")?.Value;

            _logger.LogInformation("Authenticated user accessing page: {Username} (Role: {Role}), redirecting to dashboard",
                username, userRole);

            try
            {
                // Get redirect information based on user role
                var redirectResult = GetUserDashboardRedirect(user);
                if (redirectResult.IsSuccess && !redirectResult.HasError)
                {
                    result.ShouldRedirect = true;
                    result.RedirectAction = redirectResult.RedirectAction;
                    result.RedirectController = redirectResult.RedirectController;
                }
                else
                {
                    result.ShouldRedirect = true;
                    result.RedirectAction = "Index";
                    result.RedirectController = "Login";
                    result.TempDataMessage = redirectResult.ErrorMessage ?? "Your session has expired or is invalid. Please log in again.";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redirecting authenticated user: {Username}, {Role}", username, userRole);
                result.ShouldRedirect = true;
                result.RedirectAction = "Index";
                result.RedirectController = "Login";
                result.TempDataMessage = "Your session has expired or is invalid. Please log in again.";
                return result;
            }
        }

        #endregion

    }

    #region Result Classes

    /// <summary>
    /// Result class for guest home page operations
    /// </summary>
    public class GuestHomePageResult
    {
        public bool Success { get; set; }
        public HomePageGuestViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result class for learner dashboard operations
    /// </summary>
    public class LearnerDashboardResult
    {
        public bool Success { get; set; }
        public LearnerDashboardViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool RedirectToUserDashboard { get; set; }
    }

    /// <summary>
    /// Result class for instructor dashboard operations
    /// </summary>
    public class InstructorDashboardResult
    {
        public bool Success { get; set; }
        public InstructorDashboardViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool RedirectToUserDashboard { get; set; }
    }

    /// <summary>
    /// Result class for income data operations
    /// </summary>
    public class IncomeDataResult
    {
        public bool Success { get; set; }
        public List<dynamic>? Data { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result class for recommended courses operations
    /// </summary>
    public class RecommendedCoursesResult
    {
        public bool Success { get; set; }
        public List<dynamic> Courses { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result class for database connection operations
    /// </summary>
    public class DatabaseConnectionResult
    {
        public bool Success { get; set; }
        public bool IsConnected { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Result class for Index controller action
    /// </summary>
    public class IndexControllerResult
    {
        public bool IsSuccess { get; set; }
        public bool ShouldRedirect { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public string? TempDataMessage { get; set; }
        public HomePageGuestViewModel? ViewModel { get; set; }
        public string? ViewBagError { get; set; }
        public bool ViewBagIsAuthenticated { get; set; }
    }

    /// <summary>
    /// Result class for Dashboard controller actions
    /// </summary>
    public class DashboardControllerResult
    {
        public bool IsSuccess { get; set; }
        public bool ShouldRedirect { get; set; }
        public bool ShouldRedirectToUserDashboard { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public string? TempDataErrorMessage { get; set; }
        public object? ViewModel { get; set; }
    }

    /// <summary>
    /// Result class for GetIncomeData controller action
    /// </summary>
    public class IncomeDataControllerResult
    {
        public bool IsSuccess { get; set; }
        public object? JsonResponse { get; set; }
    }

    /// <summary>
    /// Result class for user dashboard redirect operations
    /// </summary>
    public class UserDashboardRedirectResult
    {
        public bool IsSuccess { get; set; }
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
    }

    /// <summary>
    /// Result class for authentication check operations
    /// </summary>
    public class AuthenticationCheckResult
    {
        public bool ShouldRedirect { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public string? TempDataMessage { get; set; }
    }

    #endregion
}
