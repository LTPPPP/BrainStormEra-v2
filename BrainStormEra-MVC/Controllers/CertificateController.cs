using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize(Roles = "Learner,learner")]
    public class CertificateController : Controller
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<CertificateController> _logger;

        public CertificateController(BrainStormEraContext context, ILogger<CertificateController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Display all completed courses with certificates for the current learner
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }                // Get completed courses with certificates
                var enrollmentData = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.UserId == userId &&
                               e.EnrollmentStatus == 5 &&
                               e.CertificateIssuedDate != null)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Author)
                    .Select(e => new
                    {
                        CourseId = e.CourseId,
                        CourseName = e.Course.CourseName,
                        CourseImage = e.Course.CourseImage,
                        AuthorName = e.Course.Author.FullName ?? e.Course.Author.Username,
                        CertificateIssuedDate = e.CertificateIssuedDate!.Value,
                        EnrollmentDate = e.EnrollmentCreatedAt,
                        FinalScore = e.ProgressPercentage ?? 0
                    })
                    .ToListAsync();

                // Convert DateOnly to DateTime after the query
                var completedCourses = enrollmentData
                    .Select(e => new CertificateSummaryViewModel
                    {
                        CourseId = e.CourseId,
                        CourseName = e.CourseName,
                        CourseImage = e.CourseImage ?? "/img/default-course.png",
                        AuthorName = e.AuthorName,
                        CompletedDate = e.CertificateIssuedDate.ToDateTime(TimeOnly.MinValue),
                        EnrollmentDate = e.EnrollmentDate,
                        FinalScore = e.FinalScore
                    })
                    .OrderByDescending(c => c.CompletedDate)
                    .ToList();

                ViewBag.HasCertificates = completedCourses.Any();
                ViewBag.TotalCertificates = completedCourses.Count;

                return View(completedCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates for user");
                TempData["ErrorMessage"] = "An error occurred while loading your certificates. Please try again.";

                // Ensure ViewBag values are set even in error case
                ViewBag.HasCertificates = false;
                ViewBag.TotalCertificates = 0;

                return View(new List<CertificateSummaryViewModel>());
            }
        }

        /// <summary>
        /// Display detailed certificate information for a specific course
        /// </summary>
        public async Task<IActionResult> Details(string courseId)
        {
            try
            {
                if (string.IsNullOrEmpty(courseId))
                {
                    TempData["ErrorMessage"] = "Course ID is required.";
                    return RedirectToAction("Index");
                }

                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }

                // Get certificate details
                var certificateData = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.UserId == userId &&
                               e.CourseId == courseId &&
                               e.EnrollmentStatus == 5 &&
                               e.CertificateIssuedDate != null)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Author)
                    .Include(e => e.User)
                    .FirstOrDefaultAsync();

                if (certificateData == null)
                {
                    TempData["ErrorMessage"] = "Certificate not found or course not completed.";
                    return RedirectToAction("Index");
                }

                // Calculate completion duration
                var completionDuration = (certificateData.CertificateIssuedDate!.Value.ToDateTime(TimeOnly.MinValue)
                                        - certificateData.EnrollmentCreatedAt).TotalDays;

                var viewModel = new CertificateDetailsViewModel
                {
                    CourseId = certificateData.CourseId,
                    CourseName = certificateData.Course.CourseName,
                    CourseDescription = certificateData.Course.CourseDescription ?? "",
                    CourseImage = certificateData.Course.CourseImage ?? "/img/default-course.png",
                    LearnerName = certificateData.User.FullName ?? certificateData.User.Username,
                    LearnerEmail = certificateData.User.UserEmail,
                    InstructorName = certificateData.Course.Author.FullName ?? certificateData.Course.Author.Username,
                    CompletedDate = certificateData.CertificateIssuedDate.Value.ToDateTime(TimeOnly.MinValue),
                    EnrollmentDate = certificateData.EnrollmentCreatedAt,
                    CompletionDurationDays = Math.Max(1, (int)Math.Round(completionDuration)),
                    FinalScore = certificateData.ProgressPercentage ?? 0,
                    CertificateCode = $"BSE-{courseId.Substring(0, 8).ToUpper()}-{DateTime.Now.Year}"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate details for course {CourseId}", courseId);
                TempData["ErrorMessage"] = "An error occurred while loading the certificate details.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Download or print the certificate (placeholder for future implementation)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Download(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(courseId))
                {
                    return Json(new { success = false, message = "Invalid request parameters." });
                }

                // Verify the user has a certificate for this course
                var hasCertificate = await _context.Enrollments
                    .AnyAsync(e => e.UserId == userId &&
                                  e.CourseId == courseId &&
                                  e.EnrollmentStatus == 5 &&
                                  e.CertificateIssuedDate != null);

                if (!hasCertificate)
                {
                    return Json(new { success = false, message = "Certificate not found." });
                }

                // TODO: Implement actual certificate PDF generation
                // For now, redirect to the certificate details page for printing
                var printUrl = Url.Action("Details", new { courseId });
                return Json(new { success = true, printUrl, message = "Certificate ready for printing." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate for course {CourseId}", courseId);
                return Json(new { success = false, message = "An error occurred while preparing the certificate." });
            }
        }
    }
}
