using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize(Roles = "Learner,learner")]
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
            }

            return View("~/Views/Certificates/Details.cshtml", result.CertificateDetails);
        }

        [HttpPost]
        public async Task<IActionResult> Download(string courseId)
        {
            var result = await _certificateServiceImpl.DownloadCertificateAsync(User, courseId, Url);
            return result.JsonResult!;
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
    }
}
