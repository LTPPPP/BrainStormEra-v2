using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize(Roles = "Learner,learner")]
    public class CertificateController : Controller
    {
        private readonly ICertificateService _certificateService;
        private readonly ILogger<CertificateController> _logger;

        public CertificateController(ICertificateService certificateService, ILogger<CertificateController> logger)
        {
            _certificateService = certificateService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated. Please log in again.";
                    return RedirectToAction("Index", "Login");
                }

                var completedCourses = await _certificateService.GetUserCertificatesAsync(userId);

                ViewBag.HasCertificates = completedCourses.Any();
                ViewBag.TotalCertificates = completedCourses.Count;

                return View("~/Views/Certificates/Index.cshtml", completedCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates for user");
                TempData["ErrorMessage"] = "An error occurred while loading your certificates. Please try again.";

                ViewBag.HasCertificates = false;
                ViewBag.TotalCertificates = 0;
                return View("~/Views/Certificates/Index.cshtml", new List<CertificateSummaryViewModel>());
            }
        }

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

                var certificateDetails = await _certificateService.GetCertificateDetailsAsync(userId, courseId);
                if (certificateDetails == null)
                {
                    TempData["ErrorMessage"] = "Certificate not found or course not completed.";
                    return RedirectToAction("Index");
                }

                return View("~/Views/Certificates/Details.cshtml", certificateDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate details for course {CourseId}", courseId);
                TempData["ErrorMessage"] = "An error occurred while loading the certificate details.";
                return RedirectToAction("Index");
            }
        }

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

                var isValid = await _certificateService.ValidateCertificateAsync(userId, courseId);
                if (!isValid)
                {
                    return Json(new { success = false, message = "Certificate not found." });
                }

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
