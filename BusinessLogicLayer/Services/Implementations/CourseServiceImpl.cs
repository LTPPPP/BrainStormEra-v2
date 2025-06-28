using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Models;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private readonly ILessonService _lessonService;
        private readonly ILogger<CourseServiceImpl> _logger;
        private readonly IServiceProvider _serviceProvider;

        public CourseServiceImpl(
            ICourseService courseService,
            ICourseImageService courseImageService,
            ILessonService lessonService,
            ILogger<CourseServiceImpl> logger,
            IServiceProvider serviceProvider)
        {
            _courseService = courseService;
            _courseImageService = courseImageService;
            _lessonService = lessonService;
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
        /// Handle course enrollment with validation and point deduction
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

                // Get course info to check price
                var course = await _courseService.GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = "Course not found"
                    };
                }

                // If course is free, proceed with normal enrollment
                if (course.Price == 0)
                {
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
                            Message = "Enrollment failed"
                        };
                    }
                }

                // If course has price, check user points and deduct
                using var scope = _serviceProvider.CreateScope();
                var userRepo = scope.ServiceProvider.GetRequiredService<DataAccessLayer.Repositories.Interfaces.IUserRepo>();
                var context = scope.ServiceProvider.GetRequiredService<DataAccessLayer.Data.BrainStormEraContext>();

                var userWithPoints = await userRepo.GetUserWithPaymentPointAsync(userId);
                if (userWithPoints == null)
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var userPoints = userWithPoints.PaymentPoint ?? 0;
                if (userPoints < course.Price)
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = $"Insufficient points! You have {userPoints:N0} points but need {course.Price:N0} points. Please top up your account to enroll in this course."
                    };
                }

                // Use execution strategy for transaction
                var executionStrategy = context.Database.CreateExecutionStrategy();
                return await executionStrategy.ExecuteAsync<object, EnrollmentResult>(
                    new object(),
                    async (dbContext, state, cancellationToken) =>
                    {
                        var realContext = (DataAccessLayer.Data.BrainStormEraContext)dbContext;
                        using var transaction = await realContext.Database.BeginTransactionAsync(cancellationToken);
                        try
                        {
                            // Deduct points
                            var userAccount = await realContext.Accounts.FindAsync(new object[] { userId }, cancellationToken);
                            if (userAccount == null)
                            {
                                await transaction.RollbackAsync(cancellationToken);
                                return new EnrollmentResult
                                {
                                    Success = false,
                                    Message = "User account not found"
                                };
                            }

                            userAccount.PaymentPoint = (userAccount.PaymentPoint ?? 0) - course.Price;
                            userAccount.AccountUpdatedAt = DateTime.UtcNow;

                            // Check if already enrolled
                            var existingEnrollment = await realContext.Enrollments
                                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, cancellationToken);

                            if (existingEnrollment != null)
                            {
                                await transaction.RollbackAsync(cancellationToken);
                                return new EnrollmentResult
                                {
                                    Success = false,
                                    Message = "Already enrolled in this course"
                                };
                            }

                            // Create enrollment directly in the transaction context
                            var enrollment = new DataAccessLayer.Models.Enrollment
                            {
                                EnrollmentId = Guid.NewGuid().ToString(),
                                UserId = userId,
                                CourseId = courseId,
                                EnrollmentCreatedAt = DateTime.UtcNow,
                                EnrollmentUpdatedAt = DateTime.UtcNow,
                                EnrollmentStatus = 1, // Active status
                                ProgressPercentage = 0
                            };

                            realContext.Enrollments.Add(enrollment);

                            await realContext.SaveChangesAsync(cancellationToken);
                            await transaction.CommitAsync(cancellationToken);

                            return new EnrollmentResult
                            {
                                Success = true,
                                Message = $"Successfully enrolled! {course.Price:N0} points deducted from your account."
                            };
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            _logger.LogError(ex, "Error during enrollment transaction for course {CourseId}, user {UserId}", courseId, userId);
                            return new EnrollmentResult
                            {
                                Success = false,
                                Message = "An error occurred during enrollment"
                            };
                        }
                    },
                    null,
                    System.Threading.CancellationToken.None
                );
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

                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                List<dynamic> courseList;

                if (userRole?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Admin can see all courses
                    var allCourses = await _courseService.GetCoursesAsync(null, null, 1, int.MaxValue);
                    courseList = allCourses.Courses.Select(c => new
                    {
                        courseId = c.CourseId,
                        courseName = c.CourseName,
                        enrollmentCount = c.EnrollmentCount
                    }).ToList<dynamic>();
                }
                else
                {
                    // Instructor can see only their courses
                    var courses = await _courseService.GetInstructorCoursesAsync(userId, null, null, 1, int.MaxValue);
                    courseList = courses.Courses.Select(c => new
                    {
                        courseId = c.CourseId,
                        courseName = c.CourseName,
                        enrollmentCount = c.EnrollmentCount
                    }).ToList<dynamic>();
                }

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

        #region Learn Management Operations

        /// <summary>
        /// Get course learning data with chapters and lessons organized by type
        /// </summary>
        public async Task<LearnManagementResult> GetLearnManagementDataAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new LearnManagementResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                // Check if user is enrolled in the course
                var isEnrolled = await _courseService.IsUserEnrolledAsync(userId, courseId);
                if (!isEnrolled)
                {
                    return new LearnManagementResult
                    {
                        Success = false,
                        ErrorMessage = "You must be enrolled in this course to access learning content"
                    };
                }

                // Get course basic info first
                var course = await _courseService.GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    return new LearnManagementResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                // Get all chapters for this course
                var chapters = await _courseService.GetChaptersByCourseIdAsync(courseId);

                var chapterViewModels = new List<LearnChapterViewModel>();

                foreach (var chapter in chapters.OrderBy(c => c.ChapterOrder))
                {
                    // Get all lessons for each chapter
                    var lessons = await _courseService.GetLessonsByChapterIdAsync(chapter.ChapterId);

                    var lessonViewModels = new List<LearnLessonViewModel>();

                    foreach (var lesson in lessons.OrderBy(l => l.LessonOrder))
                    {
                        // Check if lesson is completed
                        var isCompleted = await _lessonService.IsLessonCompletedAsync(userId, lesson.LessonId);

                        lessonViewModels.Add(new LearnLessonViewModel
                        {
                            LessonId = lesson.LessonId,
                            LessonName = lesson.LessonName,
                            LessonDescription = lesson.LessonDescription ?? "",
                            LessonOrder = lesson.LessonOrder,
                            LessonType = lesson.LessonType?.LessonTypeName ?? "Content",
                            LessonTypeIcon = GetLessonTypeIcon(lesson.LessonType?.LessonTypeName),
                            IsLocked = lesson.IsLocked ?? false,
                            IsMandatory = lesson.IsMandatory ?? true,
                            EstimatedDuration = lesson.MinTimeSpent ?? 0,
                            IsCompleted = isCompleted,
                            ProgressPercentage = isCompleted ? 100 : 0
                        });
                    }

                    chapterViewModels.Add(new LearnChapterViewModel
                    {
                        ChapterId = chapter.ChapterId,
                        ChapterName = chapter.ChapterName,
                        ChapterDescription = chapter.ChapterDescription ?? "",
                        ChapterOrder = chapter.ChapterOrder ?? 0,
                        IsLocked = chapter.IsLocked ?? false,
                        Lessons = lessonViewModels
                    });
                }

                // Calculate overall course progress
                var courseProgress = await _lessonService.GetLessonCompletionPercentageAsync(userId, courseId);

                var viewModel = new LearnManagementViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription ?? "",
                    CourseImage = course.CourseImage ?? "/SharedMedia/defaults/default-course.png",
                    AuthorName = course.Author?.FullName ?? "Unknown Author",
                    IsEnrolled = true,
                    ProgressPercentage = courseProgress,
                    Chapters = chapterViewModels
                };

                return new LearnManagementResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading learn management data for course {CourseId}", courseId);
                return new LearnManagementResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the learning content."
                };
            }
        }

        private string GetLessonTypeIcon(string? lessonType)
        {
            return lessonType?.ToLower() switch
            {
                "video" => "fas fa-play-circle",
                "text" => "fas fa-file-text",
                "interactive" => "fas fa-gamepad",
                "quiz" => "fas fa-question-circle",
                "assignment" => "fas fa-tasks",
                "document" => "fas fa-file-pdf",
                _ => "fas fa-book"
            };
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








