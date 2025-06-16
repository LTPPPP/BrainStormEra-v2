using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models.ViewModels;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using System.Text.Json;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class CoursesModel : PageModel
    {
        private readonly IAdminService _adminService;
        private readonly ICourseRepo _courseRepo;
        private readonly BrainStormEraContext _context;
        private readonly ILogger<CoursesModel> _logger;

        public CoursesModel(
            IAdminService adminService,
            ICourseRepo courseRepo,
            BrainStormEraContext context,
            ILogger<CoursesModel> logger)
        {
            _adminService = adminService;
            _courseRepo = courseRepo;
            _context = context;
            _logger = logger;
        }

        // Properties for the view
        public List<AdminCourseViewModel> Courses { get; set; } = new();
        public List<CourseCategoryViewModel> Categories { get; set; } = new();
        public string SearchQuery { get; set; } = "";
        public string CategoryFilter { get; set; } = "";
        public string StatusFilter { get; set; } = "";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCourses { get; set; }
        public int PageSize { get; set; } = 10;

        // Statistics
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }
        public int BannedCount { get; set; }

        public async Task OnGetAsync(string? search, string? categoryFilter, string? statusFilter, int page = 1, int pageSize = 10)
        {
            try
            {
                SearchQuery = search ?? "";
                CategoryFilter = categoryFilter ?? "";
                StatusFilter = statusFilter ?? "";
                CurrentPage = page;
                PageSize = pageSize;

                await LoadCoursesAsync();
                await LoadCategoriesAsync();
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses page");
                TempData["Error"] = "An error occurred while loading the courses.";
            }
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync([FromBody] UpdateCourseStatusRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.CourseId))
                {
                    return new JsonResult(new { success = false, message = "Course ID is required" });
                }

                var adminId = User.FindFirst("UserId")?.Value;
                var success = await _adminService.UpdateCourseStatusAsync(request.CourseId, request.IsApproved, adminId);

                if (success)
                {
                    var action = request.IsApproved ? "approved" : "rejected";
                    _logger.LogInformation("Course {CourseId} {Action} by admin {AdminId}", request.CourseId, action, adminId);

                    return new JsonResult(new { success = true, message = $"Course {action} successfully" });
                }

                return new JsonResult(new { success = false, message = "Failed to update course status" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course status for CourseId: {CourseId}", request.CourseId);
                return new JsonResult(new { success = false, message = "An error occurred while updating course status" });
            }
        }

        public async Task<IActionResult> OnPostToggleFeatureAsync([FromBody] ToggleFeatureRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.CourseId))
                {
                    return new JsonResult(new { success = false, message = "Course ID is required" });
                }

                var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == request.CourseId);
                if (course == null)
                {
                    return new JsonResult(new { success = false, message = "Course not found" });
                }

                course.IsFeatured = request.IsFeatured;
                course.CourseUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var action = request.IsFeatured ? "featured" : "unfeatured";
                _logger.LogInformation("Course {CourseId} {Action} by admin {AdminId}",
                    request.CourseId, action, User.FindFirst("UserId")?.Value);

                return new JsonResult(new { success = true, message = $"Course {action} successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling feature status for CourseId: {CourseId}", request.CourseId);
                return new JsonResult(new { success = false, message = "An error occurred while updating feature status" });
            }
        }

        public async Task<IActionResult> OnPostBanCourseAsync([FromBody] BanCourseRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.CourseId))
                {
                    return new JsonResult(new { success = false, message = "Course ID is required" });
                }

                var adminId = User.FindFirst("UserId")?.Value;
                var success = await _adminService.BanCourseAsync(request.CourseId, adminId);

                if (success)
                {
                    _logger.LogInformation("Course {CourseId} banned by admin {AdminId}", request.CourseId, adminId);
                    return new JsonResult(new { success = true, message = "Course banned successfully" });
                }

                return new JsonResult(new { success = false, message = "Failed to ban course" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning course for CourseId: {CourseId}", request.CourseId);
                return new JsonResult(new { success = false, message = "An error occurred while banning course" });
            }
        }

        public async Task<IActionResult> OnGetCourseDetailsAsync(string courseId)
        {
            try
            {
                if (string.IsNullOrEmpty(courseId))
                {
                    return Content("<div class='alert alert-danger'>Course ID is required</div>");
                }

                var course = await _context.Courses
                    .Include(c => c.Author)
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Enrollments)
                    .Include(c => c.Chapters)
                        .ThenInclude(ch => ch.Lessons)
                    .Include(c => c.Feedbacks)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);

                if (course == null)
                {
                    return Content("<div class='alert alert-danger'>Course not found</div>");
                }

                var html = GenerateCourseDetailsHtml(course);
                return Content(html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course details for CourseId: {CourseId}", courseId);
                return Content("<div class='alert alert-danger'>Error loading course details</div>");
            }
        }

        private async Task LoadCoursesAsync()
        {
            try
            {
                var query = _context.Courses
                    .Include(c => c.Author)
                    .Include(c => c.CourseCategories)
                    .Include(c => c.Enrollments)
                    .Where(c => c.CourseStatus != 4); // Exclude archived courses

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    query = query.Where(c =>
                        c.CourseName.Contains(SearchQuery) ||
                        (c.CourseDescription != null && c.CourseDescription.Contains(SearchQuery)) ||
                        c.Author.FullName!.Contains(SearchQuery) ||
                        c.CourseCategories.Any(cc => cc.CourseCategoryName!.Contains(SearchQuery)));
                }

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(CategoryFilter))
                {
                    query = query.Where(c => c.CourseCategories.Any(cc => cc.CourseCategoryName == CategoryFilter));
                }

                // Apply status filter
                if (!string.IsNullOrWhiteSpace(StatusFilter))
                {
                    switch (StatusFilter.ToLower())
                    {
                        case "approved":
                            query = query.Where(c => c.ApprovalStatus == "approved");
                            break;
                        case "pending":
                            query = query.Where(c => c.ApprovalStatus == "pending");
                            break;
                        case "rejected":
                            query = query.Where(c => c.ApprovalStatus == "rejected");
                            break;
                        case "banned":
                            query = query.Where(c => c.ApprovalStatus == "banned");
                            break;
                        case "draft":
                            query = query.Where(c => c.ApprovalStatus == "draft");
                            break;
                    }
                }

                // Get total count
                TotalCourses = await query.CountAsync();
                TotalPages = (int)Math.Ceiling((double)TotalCourses / PageSize);

                // Get paginated results
                var courses = await query
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Convert to view model
                Courses = courses.Select(c => new AdminCourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CourseDescription = c.CourseDescription ?? "",
                    CoursePicture = c.CourseImage ?? "/img/defaults/default-course.svg",
                    Price = c.Price,
                    DifficultyLevel = c.DifficultyLevel?.ToString(),
                    EstimatedDuration = c.EstimatedDuration,
                    CreatedAt = c.CourseCreatedAt,
                    UpdatedAt = c.CourseUpdatedAt,
                    IsApproved = c.ApprovalStatus?.ToLower() == "approved",
                    IsFeatured = c.IsFeatured ?? false,
                    IsActive = c.CourseStatus == 1,
                    InstructorId = c.AuthorId,
                    InstructorName = c.Author.FullName ?? c.Author.Username,
                    InstructorEmail = c.Author.UserEmail,
                    EnrollmentCount = c.Enrollments.Count,
                    Categories = c.CourseCategories.Select(cc => cc.CourseCategoryName!).ToList()
                }).ToList();

                // Calculate additional properties
                foreach (var course in Courses)
                {
                    // Get the original course for status determination
                    var originalCourse = courses.First(c => c.CourseId == course.CourseId);
                    course.IsApproved = originalCourse.ApprovalStatus?.ToLower() == "approved";

                    // Calculate revenue (simplified - in real scenario you'd calculate from payment transactions)
                    course.Revenue = course.EnrollmentCount * course.Price;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses");
                Courses = new List<AdminCourseViewModel>();
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                Categories = await _context.CourseCategories
                    .Where(cc => cc.IsActive == true)
                    .Select(cc => new CourseCategoryViewModel
                    {
                        CategoryId = cc.CourseCategoryId,
                        CategoryName = cc.CourseCategoryName!,
                        CategoryDescription = cc.CategoryDescription,
                        CategoryIcon = cc.CategoryIcon,
                        CourseCount = cc.Courses.Count(c => c.CourseStatus != 4) // Exclude archived
                    })
                    .OrderBy(cc => cc.CategoryName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                Categories = new List<CourseCategoryViewModel>();
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                var courseStats = await _context.Courses
                    .Where(c => c.CourseStatus != 4) // Exclude archived
                    .GroupBy(c => c.ApprovalStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                ApprovedCount = courseStats.FirstOrDefault(s => s.Status == "approved")?.Count ?? 0;
                PendingCount = courseStats.FirstOrDefault(s => s.Status == "pending")?.Count ?? 0;
                RejectedCount = courseStats.FirstOrDefault(s => s.Status == "rejected")?.Count ?? 0;
                BannedCount = courseStats.FirstOrDefault(s => s.Status == "banned")?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading statistics");
                ApprovedCount = PendingCount = RejectedCount = BannedCount = 0;
            }
        }

        private string GenerateCourseDetailsHtml(DataAccessLayer.Models.Course course)
        {
            var avgRating = course.Feedbacks.Any() ? course.Feedbacks.Where(f => f.StarRating.HasValue).Average(f => f.StarRating!.Value) : 0;
            var totalLessons = course.Chapters.Sum(ch => ch.Lessons.Count);
            var imageUrl = !string.IsNullOrEmpty(course.CourseImage) ? course.CourseImage : "/img/defaults/default-course.svg";
            var statusClass = GetStatusBadgeClass(course.ApprovalStatus ?? "pending");
            var difficultyClass = GetDifficultyBadgeClass(course.DifficultyLevel);
            var ratingStars = GenerateRatingStars(avgRating);

            return $@"
                <div class='course-detail-container'>
                    <!-- Course Header -->
                    <div class='course-header mb-4'>
                        <div class='row align-items-center'>
                            <div class='col-md-4'>
                                <div class='course-image-container'>
                                    <img src='{imageUrl}' 
                                         alt='{course.CourseName}' 
                                         class='course-detail-image img-fluid rounded shadow'
                                         onerror=""this.onerror=null; this.src='/img/defaults/default-course.svg';"">
                                </div>
                            </div>
                            <div class='col-md-8'>
                                <div class='d-flex justify-content-between align-items-start mb-2'>
                                    <h3 class='course-title mb-1'>{course.CourseName}</h3>
                                    <span class='badge {statusClass} fs-6'>{course.ApprovalStatus?.ToUpper()}</span>
                                </div>
                                <p class='course-description text-muted mb-3'>{course.CourseDescription}</p>
                                
                                <!-- Quick Stats -->
                                <div class='quick-stats d-flex flex-wrap gap-3 mb-3'>
                                    <div class='stat-item'>
                                        <i class='fas fa-star text-warning me-1'></i>
                                        <span class='fw-semibold'>{avgRating:F1}</span>
                                        <small class='text-muted'>({course.Feedbacks.Count} reviews)</small>
                                    </div>
                                    <div class='stat-item'>
                                        <i class='fas fa-users text-primary me-1'></i>
                                        <span class='fw-semibold'>{course.Enrollments.Count}</span>
                                        <small class='text-muted'>enrolled</small>
                                    </div>
                                    <div class='stat-item'>
                                        <i class='fas fa-clock text-info me-1'></i>
                                        <span class='fw-semibold'>{course.EstimatedDuration ?? 0}h</span>
                                        <small class='text-muted'>duration</small>
                                    </div>
                                    <div class='stat-item'>
                                        <i class='fas fa-book text-success me-1'></i>
                                        <span class='fw-semibold'>{totalLessons}</span>
                                        <small class='text-muted'>lessons</small>
                                    </div>
                                </div>
                                
                                <!-- Price & Difficulty -->
                                <div class='d-flex align-items-center gap-3'>
                                    <div class='price-tag'>
                                        <i class='fas fa-tag me-1'></i>
                                        <span class='fw-bold {(course.Price > 0 ? "text-success" : "text-primary")}'>{(course.Price > 0 ? $"${course.Price:N2}" : "FREE")}</span>
                                    </div>
                                    <span class='badge {difficultyClass}'>{GetDifficultyText(course.DifficultyLevel)}</span>
                                    {(course.IsFeatured == true ? "<span class='badge bg-warning'><i class='fas fa-star'></i> Featured</span>" : "")}
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Course Details -->
                    <div class='course-details'>
                        <div class='row'>
                            <!-- Left Column -->
                            <div class='col-md-6'>
                                <div class='detail-section mb-4'>
                                    <h6 class='section-title'><i class='fas fa-user-tie me-2'></i>Instructor Information</h6>
                                    <div class='instructor-info p-3 bg-light rounded'>
                                        <div class='d-flex align-items-center'>
                                            <img src='/img/defaults/default-avatar.svg' alt='Instructor' class='instructor-avatar me-3'>
                                            <div>
                                                <div class='fw-semibold'>{course.Author.FullName ?? course.Author.Username}</div>
                                                <small class='text-muted'>{course.Author.UserEmail}</small>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                
                                <div class='detail-section mb-4'>
                                    <h6 class='section-title'><i class='fas fa-tags me-2'></i>Categories</h6>
                                    <div class='categories-container'>
                                        {(course.CourseCategories.Any() ?
                                            string.Join(" ", course.CourseCategories.Select(cc => $"<span class='badge bg-secondary me-1 mb-1'>{cc.CourseCategoryName}</span>")) :
                                            "<span class='text-muted'>No categories assigned</span>")}
                                    </div>
                                </div>
                            </div>
                            
                            <!-- Right Column -->
                            <div class='col-md-6'>
                                <div class='detail-section mb-4'>
                                    <h6 class='section-title'><i class='fas fa-chart-bar me-2'></i>Course Statistics</h6>
                                    <div class='stats-grid'>
                                        <div class='stat-card p-2 border rounded mb-2'>
                                            <div class='d-flex justify-content-between'>
                                                <span>Total Enrollments:</span>
                                                <strong class='text-primary'>{course.Enrollments.Count}</strong>
                                            </div>
                                        </div>
                                        <div class='stat-card p-2 border rounded mb-2'>
                                            <div class='d-flex justify-content-between'>
                                                <span>Total Chapters:</span>
                                                <strong class='text-info'>{course.Chapters.Count}</strong>
                                            </div>
                                        </div>
                                        <div class='stat-card p-2 border rounded mb-2'>
                                            <div class='d-flex justify-content-between'>
                                                <span>Total Lessons:</span>
                                                <strong class='text-success'>{totalLessons}</strong>
                                            </div>
                                        </div>
                                        <div class='stat-card p-2 border rounded mb-2'>
                                            <div class='d-flex justify-content-between'>
                                                <span>Reviews:</span>
                                                <strong class='text-warning'>{course.Feedbacks.Count}</strong>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                
                                <div class='detail-section mb-4'>
                                    <h6 class='section-title'><i class='fas fa-star me-2'></i>Rating Overview</h6>
                                    <div class='rating-overview p-3 bg-light rounded'>
                                        <div class='d-flex align-items-center mb-2'>
                                            <div class='rating-stars me-2'>{ratingStars}</div>
                                            <span class='fw-bold'>{avgRating:F1}</span>
                                            <small class='text-muted ms-2'>out of 5</small>
                                        </div>
                                        <small class='text-muted'>Based on {course.Feedbacks.Count} reviews</small>
                                    </div>
                                </div>
                            </div>
                        </div>
                        
                        <!-- Timeline -->
                        <div class='detail-section'>
                            <h6 class='section-title'><i class='fas fa-history me-2'></i>Timeline</h6>
                            <div class='timeline p-3 bg-light rounded'>
                                <div class='timeline-item mb-2'>
                                    <i class='fas fa-plus-circle text-primary me-2'></i>
                                    <strong>Created:</strong> {course.CourseCreatedAt:MMM dd, yyyy 'at' HH:mm}
                                </div>
                                <div class='timeline-item mb-2'>
                                    <i class='fas fa-edit text-info me-2'></i>
                                    <strong>Last Updated:</strong> {course.CourseUpdatedAt:MMM dd, yyyy 'at' HH:mm}
                                </div>
                                {(course.ApprovedAt.HasValue ? $@"
                                <div class='timeline-item'>
                                    <i class='fas fa-check-circle text-success me-2'></i>
                                    <strong>Approved:</strong> {course.ApprovedAt.Value:MMM dd, yyyy 'at' HH:mm}
                                </div>" : "")}
                            </div>
                        </div>
                    </div>
                </div>";
        }

        private string GetStatusBadgeClass(string status)
        {
            return status.ToLower() switch
            {
                "approved" => "bg-success",
                "pending" => "bg-warning",
                "rejected" => "bg-danger",
                "banned" => "bg-dark",
                "draft" => "bg-secondary",
                _ => "bg-secondary"
            };
        }

        private string GetDifficultyBadgeClass(byte? level)
        {
            return level switch
            {
                1 => "bg-success",      // Beginner - Green
                2 => "bg-info",         // Intermediate - Blue  
                3 => "bg-warning",      // Advanced - Orange
                4 => "bg-danger",       // Expert - Red
                _ => "bg-secondary"     // Unknown - Gray
            };
        }

        private string GenerateRatingStars(double rating)
        {
            var stars = "";
            for (int i = 1; i <= 5; i++)
            {
                if (i <= rating)
                    stars += "<i class='fas fa-star text-warning'></i>";
                else if (i - 0.5 <= rating)
                    stars += "<i class='fas fa-star-half-alt text-warning'></i>";
                else
                    stars += "<i class='far fa-star text-warning'></i>";
            }
            return stars;
        }

        private string GetDifficultyText(byte? level)
        {
            return level switch
            {
                1 => "Beginner",
                2 => "Intermediate",
                3 => "Advanced",
                4 => "Expert",
                _ => "Unknown"
            };
        }
    }

    public class UpdateCourseStatusRequest
    {
        public string CourseId { get; set; } = "";
        public bool IsApproved { get; set; }
    }

    public class ToggleFeatureRequest
    {
        public string CourseId { get; set; } = "";
        public bool IsFeatured { get; set; }
    }

    public class BanCourseRequest
    {
        public string CourseId { get; set; } = "";
    }
}