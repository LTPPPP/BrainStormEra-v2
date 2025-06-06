using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize(Roles = "Learner,learner")]
    public class CertificateController : BaseController
    {
        private readonly ICertificateService _certificateService;
        private readonly IUserContextService _userContextService;
        private readonly IResponseService _responseService;
        private readonly ILogger<CertificateController> _logger;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

        public CertificateController(
            ICertificateService certificateService,
            IUserContextService userContextService,
            IResponseService responseService,
            ILogger<CertificateController> logger,
            IMemoryCache cache)
        {
            _certificateService = certificateService;
            _userContextService = userContextService;
            _responseService = responseService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 6)
        {
            try
            {
                if (!IsUserAuthenticated())
                {
                    return RedirectToLogin("User not authenticated. Please log in again.");
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 6;

                var certificateList = await _certificateService.GetUserCertificatesAsync(CurrentUserId!, search, page, pageSize);
                SetViewBagData(certificateList);
                return View("~/Views/Certificates/Index.cshtml", certificateList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates for user {UserId}", CurrentUserId);
                return HandleIndexError();
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

                if (!IsUserAuthenticated())
                {
                    return RedirectToLogin("User not authenticated. Please log in again.");
                }

                var certificateDetails = await GetCachedCertificateDetails(courseId);
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
                if (!IsUserAuthenticated() || string.IsNullOrEmpty(courseId))
                {
                    return _responseService.HandleJsonError("Invalid request parameters.");
                }

                var isValid = await GetCachedCertificateValidation(courseId);
                if (!isValid)
                {
                    return _responseService.HandleJsonError("Certificate not found.");
                }

                var printUrl = Url.Action("Details", new { courseId });
                return _responseService.HandleJsonSuccess(new { printUrl }, "Certificate ready for printing.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate for course {CourseId}", courseId);
                return _responseService.HandleJsonError("An error occurred while preparing the certificate.");
            }
        }

        private bool IsUserAuthenticated()
        {
            return _userContextService.IsAuthenticated(User);
        }

        private IActionResult RedirectToLogin(string message)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction("Index", "Login");
        }

        private async Task<List<CertificateSummaryViewModel>> GetCachedUserCertificates()
        {
            var cacheKey = $"UserCertificates_{CurrentUserId}";
            if (_cache.TryGetValue(cacheKey, out List<CertificateSummaryViewModel>? cachedCertificates))
            {
                return cachedCertificates!;
            }

            var certificates = await _certificateService.GetUserCertificatesAsync(CurrentUserId!);
            _cache.Set(cacheKey, certificates, CacheExpiration);
            return certificates;
        }

        private async Task<CertificateDetailsViewModel?> GetCachedCertificateDetails(string courseId)
        {
            var cacheKey = $"CertificateDetails_{CurrentUserId}_{courseId}";
            if (_cache.TryGetValue(cacheKey, out CertificateDetailsViewModel? cachedDetails))
            {
                return cachedDetails;
            }

            var details = await _certificateService.GetCertificateDetailsAsync(CurrentUserId!, courseId);
            if (details != null)
            {
                _cache.Set(cacheKey, details, CacheExpiration);
            }
            return details;
        }

        private async Task<bool> GetCachedCertificateValidation(string courseId)
        {
            var cacheKey = $"CertificateValid_{CurrentUserId}_{courseId}";
            if (_cache.TryGetValue(cacheKey, out bool cachedValid))
            {
                return cachedValid;
            }

            var isValid = await _certificateService.ValidateCertificateAsync(CurrentUserId!, courseId);
            _cache.Set(cacheKey, isValid, CacheExpiration);
            return isValid;
        }

        private void SetViewBagData(CertificateListViewModel certificateList)
        {
            ViewBag.HasCertificates = certificateList.HasCertificates;
            ViewBag.TotalCertificates = certificateList.TotalCertificates;
            ViewBag.SearchQuery = certificateList.SearchQuery;
        }

        private void SetViewBagData(List<CertificateSummaryViewModel> certificates)
        {
            ViewBag.HasCertificates = certificates.Any();
            ViewBag.TotalCertificates = certificates.Count;
        }

        private IActionResult HandleIndexError()
        {
            TempData["ErrorMessage"] = "An error occurred while loading your certificates. Please try again.";
            var emptyList = new CertificateListViewModel
            {
                Certificates = new List<CertificateSummaryViewModel>(),
                SearchQuery = null,
                CurrentPage = 1,
                PageSize = 6,
                TotalCertificates = 0,
                TotalPages = 0
            };
            SetViewBagData(emptyList);
            return View("~/Views/Certificates/Index.cshtml", emptyList);
        }
    }
}
