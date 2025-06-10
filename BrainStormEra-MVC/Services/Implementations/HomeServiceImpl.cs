using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BrainStormEra_MVC.Services.Implementations
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
                if (!string.Equals(userRole, "Learner", StringComparison.OrdinalIgnoreCase))
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
                if (!string.Equals(userRole, "Instructor", StringComparison.OrdinalIgnoreCase))
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
                if (!string.Equals(userRole, "Instructor", StringComparison.OrdinalIgnoreCase))
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

    #endregion
}
