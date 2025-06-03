using Microsoft.AspNetCore.Mvc;
using BrainStormEra_MVC.Services.Interfaces;
using BrainStormEra_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    public class CourseController : BaseController
    {
        private readonly ICourseService _courseService;
        private readonly ICourseImageService _courseImageService;
        private readonly ILogger<CourseController> _logger;

        public CourseController(
            ICourseService courseService,
            ICourseImageService courseImageService,
            ILogger<CourseController> logger)
        {
            _courseService = courseService;
            _courseImageService = courseImageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? category, int page = 1, int pageSize = 12)
        {
            try
            {
                CourseListViewModel viewModel;

                // Check if user is instructor and should see only their courses
                if (CurrentUserRole?.Equals("Instructor", StringComparison.OrdinalIgnoreCase) == true && !string.IsNullOrEmpty(CurrentUserId))
                {
                    viewModel = await _courseService.GetInstructorCoursesAsync(CurrentUserId, search, category, page, pageSize);
                }
                else
                {
                    viewModel = await _courseService.GetCoursesAsync(search, category, page, pageSize);
                }

                return View("~/Views/Courses/Index.cshtml", viewModel);
            }
            catch (Exception)
            {
                ViewBag.Error = "An error occurred while loading courses. Please try again later.";
                return View("~/Views/Courses/Index.cshtml", new CourseListViewModel());
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // Get current user ID if authenticated
                string? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    currentUserId = User.FindFirst("UserId")?.Value;
                }

                var viewModel = await _courseService.GetCourseDetailAsync(id, currentUserId);
                if (viewModel == null)
                {
                    return NotFound();
                }

                if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(currentUserId))
                {
                    viewModel.IsEnrolled = await _courseService.IsUserEnrolledAsync(currentUserId, id);

                    // Check if current user is the course author
                    ViewBag.IsAuthor = viewModel.AuthorId == currentUserId;
                    ViewBag.CurrentUserId = currentUserId;
                }

                viewModel.CanEnroll = !viewModel.IsEnrolled;
                return View("~/Views/Courses/Details.cshtml", viewModel);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Enroll(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var isAlreadyEnrolled = await _courseService.IsUserEnrolledAsync(userId, courseId);
                if (isAlreadyEnrolled)
                {
                    return Json(new { success = false, message = "Already enrolled in this course" });
                }

                var success = await _courseService.EnrollUserAsync(userId, courseId);
                if (success)
                {
                    return Json(new { success = true, message = "Successfully enrolled in course!" });
                }
                else
                {
                    return Json(new { success = false, message = "Course requires payment or enrollment failed" });
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred during enrollment" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchCourses(string? search, string? category, int page = 1, int pageSize = 12, string? sortBy = "newest")
        {
            try
            {
                var courses = await _courseService.SearchCoursesAsync(search, category, page, pageSize, sortBy);
                var totalCourses = courses.Count;
                var totalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

                var result = new
                {
                    success = true,
                    courses = courses,
                    totalCourses = totalCourses,
                    totalPages = totalPages,
                    currentPage = page,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                };

                return Json(result);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while searching courses" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Instructor,instructor")]
        public async Task<IActionResult> CreateCourse()
        {
            try
            {
                var model = new CreateCourseViewModel
                {
                    AvailableCategories = await _courseService.GetCategoriesAsync()
                };
                return View("~/Views/Courses/CreateCourse.cshtml", model);
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while loading the create course page.";
                return RedirectToAction("InstructorDashboard", "Home");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CreateCourseViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userId = User.FindFirst("UserId")?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        TempData["ErrorMessage"] = "User not authenticated";
                        return RedirectToAction("Index", "Login");
                    }

                    var courseId = await _courseService.CreateCourseAsync(model, userId);

                    // Handle course image upload if provided
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
                            TempData["WarningMessage"] = uploadResult.ErrorMessage ?? "Failed to upload course image.";
                        }
                    }

                    TempData["SuccessMessage"] = "Course created successfully! Your course is now pending approval.";
                    return RedirectToAction("InstructorDashboard", "Home");
                }

                // If model state is invalid, reload the form with categories
                model.AvailableCategories = await _courseService.GetCategoriesAsync();
                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
                return View("~/Views/Courses/CreateCourse.cshtml", model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the course. Please try again.";

                try
                {
                    model.AvailableCategories = await _courseService.GetCategoriesAsync();
                }
                catch (Exception)
                {
                    model.AvailableCategories = new List<CourseCategoryViewModel>();
                }
                return View("~/Views/Courses/CreateCourse.cshtml", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchCategories(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return Json(new List<CategoryAutocompleteItem>());
                }

                var categories = await _courseService.SearchCategoriesAsync(term);
                return Json(categories);
            }
            catch (Exception)
            {
                return Json(new List<CategoryAutocompleteItem>());
            }
        }

        [HttpGet]
        [Authorize(Roles = "Instructor,instructor")]
        public async Task<IActionResult> EditCourse(string id)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated";
                    return RedirectToAction("Index", "Login");
                }

                var model = await _courseService.GetCourseForEditAsync(id, userId);
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Course not found or you are not authorized to edit this course.";
                    return RedirectToAction("InstructorDashboard", "Home");
                }

                ViewBag.CourseId = id;
                return View("~/Views/Courses/EditCourse.cshtml", model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the edit course page.";
                return RedirectToAction("InstructorDashboard", "Home");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(string id, CreateCourseViewModel model)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated";
                    return RedirectToAction("Index", "Login");
                }

                if (ModelState.IsValid)
                {
                    var success = await _courseService.UpdateCourseAsync(id, model, userId);
                    if (success)
                    {
                        // Handle course image upload if provided
                        if (model.CourseImage != null)
                        {
                            var uploadResult = await _courseImageService.UploadCourseImageAsync(model.CourseImage, id);
                            if (uploadResult.Success && !string.IsNullOrEmpty(uploadResult.ImagePath))
                            {
                                await _courseService.UpdateCourseImageAsync(id, uploadResult.ImagePath);
                            }
                            else
                            {
                                TempData["WarningMessage"] = uploadResult.ErrorMessage ?? "Failed to upload course image.";
                            }
                        }

                        TempData["SuccessMessage"] = "Course updated successfully!";
                        return RedirectToAction("InstructorDashboard", "Home");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Course not found or you are not authorized to edit this course.";
                        return RedirectToAction("InstructorDashboard", "Home");
                    }
                }

                // If model state is invalid, reload the form with categories
                model.AvailableCategories = await _courseService.GetCategoriesAsync();
                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
                ViewBag.CourseId = id;
                return View("~/Views/Courses/EditCourse.cshtml", model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the course. Please try again.";
                return RedirectToAction("InstructorDashboard", "Home");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(string id)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var success = await _courseService.DeleteCourseAsync(id, userId);
                if (success)
                {
                    return Json(new { success = true, message = "Course deleted successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Course not found, you are not authorized to delete this course, or the course has enrolled students." });
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while deleting the course." });
            }
        }
    }
}
