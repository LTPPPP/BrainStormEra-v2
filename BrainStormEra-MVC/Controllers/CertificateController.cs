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
        private readonly CertificateServiceImpl _certificateServiceImpl;
        private readonly ILogger<CertificateController> _logger;

        public CertificateController(
            CertificateServiceImpl certificateServiceImpl,
            ILogger<CertificateController> logger)
        {
            _certificateServiceImpl = certificateServiceImpl;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 6)
        {
            var result = await _certificateServiceImpl.GetCertificatesIndexAsync(User, search, page, pageSize);

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
            var result = await _certificateServiceImpl.GetCertificateDetailsAsync(User, courseId);

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
        public async Task<IActionResult> Download(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Get certificate details
                var result = await _certificateServiceImpl.GetCertificateDetailsAsync(User, courseId);
                if (!result.IsSuccess)
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }

                // Generate PDF using Rotativa
                var pdfResult = new ViewAsPdf("~/Views/Certificates/CertificatePdf.cshtml", result.CertificateDetails)
                {
                    FileName = $"Certificate_{result.CertificateDetails!.CourseName.Replace(" ", "_")}_{result.CertificateDetails.LearnerName.Replace(" ", "_")}.pdf",
                    PageSize = Size.A4,
                    PageOrientation = Orientation.Landscape,
                    PageMargins = { Top = 10, Bottom = 10, Left = 10, Right = 10 }
                };

                return pdfResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate PDF");
                return Json(new { success = false, message = "An error occurred while generating the certificate" });
            }
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

                // Get certificate service from DI
                using var scope = HttpContext.RequestServices.CreateScope();
                var certificateService = scope.ServiceProvider.GetRequiredService<ICertificateService>();

                var result = await certificateService.ProcessPendingCertificatesAsync(userId);

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

