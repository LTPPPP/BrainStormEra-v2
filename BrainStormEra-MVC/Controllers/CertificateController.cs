using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BrainStormEra_MVC.Filters;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize(Roles = "learner")]
    public class CertificateController : BaseController
    {
        private readonly ICertificateService _certificateService;
        private readonly ILogger<CertificateController> _logger;

        public CertificateController(
            ICertificateService certificateService,
            ILogger<CertificateController> logger)
        {
            _certificateService = certificateService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 6)
        {
            var result = await _certificateService.GetCertificatesIndexAsync(User, search, page, pageSize);

            if (!result.IsSuccess)
            {
                if (result.RedirectToLogin)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Login");
                }

                if (result.ShowErrorView)
                {
                    return HandleIndexError(result.ErrorMessage);
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return View("~/Views/Certificates/Index.cshtml");
            }

            SetViewBagData(result.CertificateList!);
            return View("~/Views/Certificates/Index.cshtml", result.CertificateList);
        }
        [RequireAuthentication("You need to login to view certificate details. Please login to continue.")]
        public async Task<IActionResult> Details(string courseId)
        {
            var result = await _certificateService.GetCertificateDetailsAsync(User, courseId);

            if (!result.IsSuccess)
            {
                if (result.RedirectToLogin)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Login");
                }

                if (result.RedirectToIndex)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index");
                }

                return HandleDetailsError(result.ErrorMessage);
            }

            return View("~/Views/Certificates/Details.cshtml", result.CertificateDetails);
        }

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Download(string courseId)
        {
            try
            {
                if (string.IsNullOrEmpty(courseId))
                {
                    TempData["ErrorMessage"] = "Course ID is required";
                    return RedirectToAction("Index");
                }

                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User not authenticated";
                    return RedirectToAction("Index", "Login");
                }

                // Validate certificate first
                var isValid = await _certificateService.ValidateCertificateAsync(userId, courseId);
                if (!isValid)
                {
                    TempData["ErrorMessage"] = "Certificate not found or invalid";
                    return RedirectToAction("Index");
                }

                // Get certificate details using the proper method
                var certificateDetails = await _certificateService.GetCertificateDetailsAsync(userId, courseId);
                if (certificateDetails == null)
                {
                    TempData["ErrorMessage"] = "Certificate details not found";
                    return RedirectToAction("Index");
                }

                // Sanitize filename
                var safeCourseName = System.Text.RegularExpressions.Regex.Replace(certificateDetails.CourseName, @"[^\w\s-]", "");
                var safeLearnerName = System.Text.RegularExpressions.Regex.Replace(certificateDetails.LearnerName, @"[^\w\s-]", "");
                var fileName = $"Certificate_{safeCourseName.Replace(" ", "_")}_{safeLearnerName.Replace(" ", "_")}.pdf";

                // Generate PDF using Rotativa
                var pdfResult = new ViewAsPdf("~/Views/Certificates/CertificatePdf.cshtml", certificateDetails)
                {
                    FileName = fileName,
                    PageSize = Size.A4,
                    PageOrientation = Orientation.Landscape,
                    PageMargins = { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                    CustomSwitches = "--disable-smart-shrinking --print-media-type --no-stop-slow-scripts"
                };

                return pdfResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate PDF for course {CourseId}", courseId);
                TempData["ErrorMessage"] = "An error occurred while generating the certificate. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult TestDownload()
        {
            // This is a test endpoint to verify PDF generation works
            var testModel = new CertificateDetailsViewModel
            {
                CourseId = "test-course-123",
                CourseName = "Test Course",
                CourseDescription = "This is a test course for certificate generation",
                CourseImage = "/SharedMedia/defaults/default-course.svg",
                LearnerName = "Test Student",
                LearnerEmail = "test@example.com",
                InstructorName = "Test Instructor",
                CompletedDate = DateTime.Now,
                EnrollmentDate = DateTime.Now.AddDays(-30),
                CompletionDurationDays = 30,
                FinalScore = 95,
                CertificateCode = "TEST-12345-2024"
            };

            var pdfResult = new ViewAsPdf("~/Views/Certificates/CertificatePdf.cshtml", testModel)
            {
                FileName = "Test_Certificate.pdf",
                PageSize = Size.A4,
                PageOrientation = Orientation.Landscape,
                PageMargins = { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                CustomSwitches = "--disable-smart-shrinking --print-media-type --no-stop-slow-scripts"
            };

            return pdfResult;
        }

        [HttpPost]
        [RequireAuthentication("You need to login to process certificates. Please login to continue.")]
        public async Task<IActionResult> ProcessPendingCertificates()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _certificateService.ProcessPendingCertificatesAsync(userId);

                if (result)
                {
                    return Json(new { success = true, message = "Certificates processed successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "No pending certificates found or error occurred" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending certificates");
                return Json(new { success = false, message = "An error occurred while processing certificates" });
            }
        }

        private void SetViewBagData(CertificateListViewModel certificateList)
        {
            ViewBag.HasCertificates = certificateList.HasCertificates;
            ViewBag.TotalCertificates = certificateList.TotalCertificates;
            ViewBag.SearchQuery = certificateList.SearchQuery;
        }

        private IActionResult HandleIndexError(string? errorMessage = null)
        {
            TempData["ErrorMessage"] = errorMessage ?? "An error occurred while loading your certificates. Please try again.";
            var emptyList = new CertificateListViewModel
            {
                Certificates = new List<CertificateSummaryViewModel>(),
                SearchQuery = null,
                CurrentPage = 1,
                PageSize = 10,
                TotalCertificates = 0,
                TotalPages = 0
            };
            SetViewBagData(emptyList);
            return View("~/Views/Certificates/Index.cshtml", emptyList);
        }

        private IActionResult HandleDetailsError(string? errorMessage = null)
        {
            TempData["ErrorMessage"] = errorMessage ?? "An error occurred while loading certificate details. Please try again.";
            return View("~/Views/Certificates/Details.cshtml");
        }
    }
}

