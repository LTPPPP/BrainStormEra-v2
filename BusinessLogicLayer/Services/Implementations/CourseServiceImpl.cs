using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles complex business logic for Course operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class CourseServiceImpl
    {
        private readonly ICourseService _courseService;
        private readonly ICourseImageService _courseImageService;
        private readonly ILogger<CourseServiceImpl> _logger;
        private readonly IServiceProvider _serviceProvider;

        public CourseServiceImpl(
            ICourseService courseService,
            ICourseImageService courseImageService,
            ILogger<CourseServiceImpl> logger,
            IServiceProvider serviceProvider)
        {
            _courseService = courseService;
            _courseImageService = courseImageService;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        #region Course Listing Operations

        /// <summary>
        /// Get courses based on user role and permissions
        /// </summary>
        public async Task<CourseIndexResult> GetCoursesAsync(
            ClaimsPrincipal user,
            string? search,
            string? category,
            int page = 1,
            int pageSize = 12)
        {
            try
            {
                CourseListViewModel viewModel;
                var currentUserId = user.FindFirst("UserId")?.Value;
                var currentUserRole = user.FindFirst(ClaimTypes.Role)?.Value;

                // Check if user is instructor and should see only their courses
                if (currentUserRole?.Equals("Instructor", StringComparison.OrdinalIgnoreCase) == true
                    && !string.IsNullOrEmpty(currentUserId))
                {
                    viewModel = await _courseService.GetInstructorCoursesAsync(currentUserId, search, category, page, pageSize);
                }
                else
                {
                    viewModel = await _courseService.GetCoursesAsync(search, category, page, pageSize);
                }

                return new CourseIndexResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading courses");
                return new CourseIndexResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading courses. Please try again later.",
                    ViewModel = new CourseListViewModel()
                };
            }
        }                        /// <summary>
                                 /// Search courses with advanced filtering and sorting (separated course and category search)
                                 /// Role-based: Instructors see denied/pending courses, Admins see deleted courses
                                 /// </summary>
        public async Task<CourseSearchResult> SearchCoursesAsync(
            ClaimsPrincipal user,
            string? courseSearch,
            string? categorySearch,
            int page = 1,
            int pageSize = 12,
            string? sortBy = "newest",
            string? price = null,
            string? difficulty = null,
            string? duration = null)
        {
            try
            {
                // Get user role and ID for role-based filtering
                string? userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                string? userId = user.FindFirst("UserId")?.Value;

                var (courses, totalCount) = await _courseService.SearchCoursesWithPaginationAsync(
                    courseSearch, categorySearch, page, pageSize, sortBy, price, difficulty, duration, userRole, userId);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return new CourseSearchResult
                {
                    Success = true,
                    Courses = courses,
                    TotalCourses = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching courses");
                return new CourseSearchResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while searching courses",
                    Courses = new List<CourseViewModel>()
                };
            }
        }

        #endregion

        #region Course Detail Operations

        /// <summary>
        /// Get course details with user-specific information
        /// </summary>
        public async Task<CourseDetailResult> GetCourseDetailsAsync(ClaimsPrincipal user, string courseId, string? tab = null)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                return new CourseDetailResult
                {
                    Success = false,
                    IsNotFound = true
                };
            }

            try
            {
                // Get current user ID if authenticated
                string? currentUserId = null;
                if (user.Identity?.IsAuthenticated == true)
                {
                    currentUserId = user.FindFirst("UserId")?.Value;
                }

                var viewModel = await _courseService.GetCourseDetailAsync(courseId, currentUserId);
                if (viewModel == null)
                {
                    return new CourseDetailResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                // Set user-specific properties
                if (user.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(currentUserId))
                {
                    viewModel.IsEnrolled = await _courseService.IsUserEnrolledAsync(currentUserId, courseId);
                    viewModel.CanEnroll = !viewModel.IsEnrolled;
                }

                return new CourseDetailResult
                {
                    Success = true,
                    ViewModel = viewModel,
                    IsAuthor = viewModel.AuthorId == currentUserId,
                    CurrentUserId = currentUserId,
                    ActiveTab = tab
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading course details for course {CourseId}", courseId);
                return new CourseDetailResult
                {
                    Success = false,
                    IsNotFound = true
                };
            }
        }

        #endregion

        #region Course Enrollment Operations

        /// <summary>
        /// Handle course enrollment with validation
        /// </summary>
        public async Task<EnrollmentResult> EnrollInCourseAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                var isAlreadyEnrolled = await _courseService.IsUserEnrolledAsync(userId, courseId);
                if (isAlreadyEnrolled)
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = "Already enrolled in this course"
                    };
                }

                var success = await _courseService.EnrollUserAsync(userId, courseId);
                if (success)
                {
                    return new EnrollmentResult
                    {
                        Success = true,
                        Message = "Successfully enrolled in course!"
                    };
                }
                else
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = "Course requires payment or enrollment failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during enrollment for course {CourseId}", courseId);
                return new EnrollmentResult
                {
                    Success = false,
                    Message = "An error occurred during enrollment"
                };
            }
        }

        #endregion

        #region Course Management Operations (Instructor)

        /// <summary>
        /// Get create course view model with available categories
        /// </summary>
        public async Task<CreateCourseResult> GetCreateCourseViewModelAsync()
        {
            try
            {
                var model = new CreateCourseViewModel
                {
                    AvailableCategories = await _courseService.GetCategoriesAsync()
                };

                return new CreateCourseResult
                {
                    Success = true,
                    ViewModel = model
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create course page");
                return new CreateCourseResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the create course page.",
                    RedirectAction = "InstructorDashboard",
                    RedirectController = "Home"
                };
            }
        }

        /// <summary>
        /// Create a new course with image upload handling
        /// </summary>
        public async Task<CreateCourseResult> CreateCourseAsync(
            ClaimsPrincipal user,
            CreateCourseViewModel model)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CreateCourseResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var courseId = await _courseService.CreateCourseAsync(model, userId);

                // Handle course image upload if provided
                string? warningMessage = null;
                if (model.CourseImage != null)
                {
                    var uploadResult = await _courseImageService.UploadCourseImageAsync(model.CourseImage, courseId);
                    if (uploadResult.Success && !string.IsNullOrEmpty(uploadResult.ImagePath))
                    {
                        // Update course with image path
                        await _courseService.UpdateCourseImageAsync(courseId, uploadResult.ImagePath);
                    }
                    else
                    {
                        warningMessage = uploadResult.ErrorMessage ?? "Failed to upload course image.";
                    }
                }

                return new CreateCourseResult
                {
                    Success = true,
                    SuccessMessage = "Course created successfully! Your course is saved as draft. Visit the course details page to request approval when you're ready to publish.",
                    WarningMessage = warningMessage,
                    RedirectAction = "InstructorDashboard",
                    RedirectController = "Home"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating course");

                // Reload categories for the view
                try
                {
                    model.AvailableCategories = await _courseService.GetCategoriesAsync();
                }
                catch (Exception)
                {
                    model.AvailableCategories = new List<CourseCategoryViewModel>();
                }

                return new CreateCourseResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while creating the course. Please try again.",
                    ViewModel = model,
                    ReturnView = true
                };
            }
        }

        /// <summary>
        /// Get course for editing with authorization check
        /// </summary>
        public async Task<EditCourseResult> GetCourseForEditAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditCourseResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var model = await _courseService.GetCourseForEditAsync(courseId, userId);
                if (model == null)
                {
                    return new EditCourseResult
                    {
                        Success = false,
                        ErrorMessage = "Course not found or you are not authorized to edit this course.",
                        RedirectAction = "InstructorDashboard",
                        RedirectController = "Home"
                    };
                }

                return new EditCourseResult
                {
                    Success = true,
                    ViewModel = model,
                    CourseId = courseId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading edit course page for course {CourseId}", courseId);
                return new EditCourseResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the edit course page.",
                    RedirectAction = "InstructorDashboard",
                    RedirectController = "Home"
                };
            }
        }

        /// <summary>
        /// Update course with image upload handling
        /// </summary>
        public async Task<EditCourseResult> UpdateCourseAsync(
            ClaimsPrincipal user,
            string courseId,
            CreateCourseViewModel model)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditCourseResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var success = await _courseService.UpdateCourseAsync(courseId, model, userId);
                if (!success)
                {
                    return new EditCourseResult
                    {
                        Success = false,
                        ErrorMessage = "Course not found or you are not authorized to edit this course.",
                        RedirectAction = "InstructorDashboard",
                        RedirectController = "Home"
                    };
                }

                // Handle course image upload if provided
                string? warningMessage = null;
                if (model.CourseImage != null)
                {
                    var uploadResult = await _courseImageService.UploadCourseImageAsync(model.CourseImage, courseId);
                    if (uploadResult.Success && !string.IsNullOrEmpty(uploadResult.ImagePath))
                    {
                        await _courseService.UpdateCourseImageAsync(courseId, uploadResult.ImagePath);
                    }
                    else
                    {
                        warningMessage = uploadResult.ErrorMessage ?? "Failed to upload course image.";
                    }
                }

                return new EditCourseResult
                {
                    Success = true,
                    SuccessMessage = "Course updated successfully!",
                    WarningMessage = warningMessage,
                    RedirectAction = "InstructorDashboard",
                    RedirectController = "Home"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating course {CourseId}", courseId);

                // Reload categories for the view
                try
                {
                    model.AvailableCategories = await _courseService.GetCategoriesAsync();
                }
                catch (Exception)
                {
                    model.AvailableCategories = new List<CourseCategoryViewModel>();
                }

                return new EditCourseResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while updating the course. Please try again.",
                    ViewModel = model,
                    CourseId = courseId,
                    ReturnView = true
                };
            }
        }

        /// <summary>
        /// Delete course with authorization check
        /// </summary>
        public async Task<DeleteCourseResult> DeleteCourseAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new DeleteCourseResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                var success = await _courseService.DeleteCourseAsync(courseId, userId);
                if (success)
                {
                    return new DeleteCourseResult
                    {
                        Success = true,
                        Message = "Course deleted successfully!"
                    };
                }
                else
                {
                    return new DeleteCourseResult
                    {
                        Success = false,
                        Message = "Course not found, you are not authorized to delete this course, or the course has enrolled students."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting course {CourseId}", courseId);
                return new DeleteCourseResult
                {
                    Success = false,
                    Message = "An error occurred while deleting the course."
                };
            }
        }

        #endregion

        #region Category Operations

        /// <summary>
        /// Search categories for autocomplete
        /// </summary>
        public async Task<List<CategoryAutocompleteItem>> SearchCategoriesAsync(string? term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return new List<CategoryAutocompleteItem>();
                }

                return await _courseService.SearchCategoriesAsync(term);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching categories with term: {Term}", term);
                return new List<CategoryAutocompleteItem>();
            }
        }

        #endregion

        #region Instructor Dashboard Operations

        /// <summary>
        /// Get user courses for notifications (Instructor only)
        /// </summary>
        public async Task<InstructorCoursesResult> GetUserCoursesAsync(ClaimsPrincipal user)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new InstructorCoursesResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                var courses = await _courseService.GetInstructorCoursesAsync(userId, null, null, 1, int.MaxValue);

                var courseList = courses.Courses.Select(c => new
                {
                    courseId = c.CourseId,
                    courseName = c.CourseName,
                    enrollmentCount = c.EnrollmentCount
                }).ToList();

                return new InstructorCoursesResult
                {
                    Success = true,
                    Courses = courseList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user courses for notifications");
                return new InstructorCoursesResult
                {
                    Success = false,
                    Message = "An error occurred while loading courses."
                };
            }
        }

        #endregion

        #region Course Approval Operations

        /// <summary>
        /// Request course approval for admin review
        /// </summary>
        public async Task<CourseApprovalResult> RequestCourseApprovalAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                // Check if user is the author of the course
                var course = await _courseService.GetCourseDetailAsync(courseId, userId);
                if (course == null)
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "Course not found"
                    };
                }

                if (course.AuthorId != userId)
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "You are not authorized to request approval for this course"
                    };
                }

                // Check current approval status
                if (course.ApprovalStatus?.ToLower() == "approved")
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "This course is already approved"
                    };
                }

                if (course.ApprovalStatus?.ToLower() == "pending")
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "This course is already pending approval"
                    };
                }

                // Only allow request for courses that are in draft mode or were rejected
                if (!string.IsNullOrEmpty(course.ApprovalStatus) &&
                    course.ApprovalStatus.ToLower() != "draft" &&
                    course.ApprovalStatus.ToLower() != "rejected")
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "This course is not eligible for approval request"
                    };
                }

                // Update course status to pending
                var success = await _courseService.UpdateCourseApprovalStatusAsync(courseId, "Pending");
                if (success)
                {
                    // Send notification to all admins about the approval request
                    try
                    {
                        var notificationService = _serviceProvider.GetService<INotificationService>();
                        if (notificationService != null)
                        {
                            await notificationService.SendToRoleAsync(
                                "admin",
                                "New Course Approval Request",
                                $"Instructor {course.AuthorName} has requested approval for course '{course.CourseName}'. Please review and approve/reject this course.",
                                "course_approval",
                                userId
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send notification to admins about course approval request");
                        // Don't fail the whole operation if notification fails
                    }

                    return new CourseApprovalResult
                    {
                        Success = true,
                        Message = "Course approval request submitted successfully! Your course is now pending admin review."
                    };
                }
                else
                {
                    return new CourseApprovalResult
                    {
                        Success = false,
                        Message = "Failed to submit approval request. Please try again."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while requesting course approval for course {CourseId}", courseId);
                return new CourseApprovalResult
                {
                    Success = false,
                    Message = "An error occurred while submitting approval request."
                };
            }
        }

        #endregion
    }

    #region Result Classes

    public class CourseIndexResult
    {
        public bool Success { get; set; }
        public CourseListViewModel ViewModel { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class CourseSearchResult
    {
        public bool Success { get; set; }
        public List<CourseViewModel> Courses { get; set; } = new();
        public int TotalCourses { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CourseDetailResult
    {
        public bool Success { get; set; }
        public bool IsNotFound { get; set; }
        public CourseDetailViewModel? ViewModel { get; set; }
        public bool IsAuthor { get; set; }
        public string? CurrentUserId { get; set; }
        public string? ActiveTab { get; set; }
    }

    public class EnrollmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CreateCourseResult
    {
        public bool Success { get; set; }
        public CreateCourseViewModel? ViewModel { get; set; }
        public string? SuccessMessage { get; set; }
        public string? WarningMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool ReturnView { get; set; }
    }

    public class EditCourseResult
    {
        public bool Success { get; set; }
        public CreateCourseViewModel? ViewModel { get; set; }
        public string? CourseId { get; set; }
        public string? SuccessMessage { get; set; }
        public string? WarningMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool ReturnView { get; set; }
    }

    public class DeleteCourseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class InstructorCoursesResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Courses { get; set; }
    }

    public class CourseApprovalResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}








