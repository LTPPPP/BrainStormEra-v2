using DataAccessLayer.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace BrainStormEra_MVC.Services.Implementations
{
    public class CertificateServiceImpl
    {
        private readonly ICertificateService _certificateService;
        private readonly IUserContextService _userContextService;
        private readonly IResponseService _responseService;
        private readonly ILogger<CertificateServiceImpl> _logger;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

        public CertificateServiceImpl(
            ICertificateService certificateService,
            IUserContextService userContextService,
            IResponseService responseService,
            ILogger<CertificateServiceImpl> logger,
            IMemoryCache cache)
        {
            _certificateService = certificateService;
            _userContextService = userContextService;
            _responseService = responseService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<GetCertificatesIndexResult> GetCertificatesIndexAsync(ClaimsPrincipal user, string? search, int page, int pageSize)
        {
            try
            {
                if (!_userContextService.IsAuthenticated(user))
                {
                    _logger.LogWarning("Unauthenticated user attempted to access certificates");
                    return new GetCertificatesIndexResult
                    {
                        IsSuccess = false,
                        RedirectToLogin = true,
                        ErrorMessage = "User not authenticated. Please log in again."
                    };
                }

                var userId = user?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims for certificates request");
                    return new GetCertificatesIndexResult
                    {
                        IsSuccess = false,
                        RedirectToLogin = true,
                        ErrorMessage = "User not authenticated. Please log in again."
                    };
                }

                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 6;

                var certificateList = await _certificateService.GetUserCertificatesAsync(userId, search, page, pageSize);

                return new GetCertificatesIndexResult
                {
                    IsSuccess = true,
                    CertificateList = certificateList,
                    UserId = userId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates for user");
                return new GetCertificatesIndexResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while loading your certificates. Please try again.",
                    ShowErrorView = true
                };
            }
        }

        public async Task<GetCertificateDetailsResult> GetCertificateDetailsAsync(ClaimsPrincipal user, string courseId)
        {
            try
            {
                if (string.IsNullOrEmpty(courseId))
                {
                    _logger.LogWarning("Course ID is required for certificate details");
                    return new GetCertificateDetailsResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Course ID is required.",
                        RedirectToIndex = true
                    };
                }

                if (!_userContextService.IsAuthenticated(user))
                {
                    _logger.LogWarning("Unauthenticated user attempted to access certificate details for course {CourseId}", courseId);
                    return new GetCertificateDetailsResult
                    {
                        IsSuccess = false,
                        RedirectToLogin = true,
                        ErrorMessage = "User not authenticated. Please log in again."
                    };
                }

                var userId = user?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims for certificate details request");
                    return new GetCertificateDetailsResult
                    {
                        IsSuccess = false,
                        RedirectToLogin = true,
                        ErrorMessage = "User not authenticated. Please log in again."
                    };
                }

                var certificateDetails = await GetCachedCertificateDetails(courseId, userId);
                if (certificateDetails == null)
                {
                    _logger.LogWarning("Certificate not found for course {CourseId} and user {UserId}", courseId, userId);
                    return new GetCertificateDetailsResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Certificate not found or course not completed.",
                        RedirectToIndex = true
                    };
                }

                return new GetCertificateDetailsResult
                {
                    IsSuccess = true,
                    CertificateDetails = certificateDetails
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate details for course {CourseId}", courseId);
                return new GetCertificateDetailsResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while loading the certificate details.",
                    RedirectToIndex = true
                };
            }
        }

        public async Task<DownloadCertificateResult> DownloadCertificateAsync(ClaimsPrincipal user, string courseId, Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper)
        {
            try
            {
                if (!_userContextService.IsAuthenticated(user) || string.IsNullOrEmpty(courseId))
                {
                    _logger.LogWarning("Invalid request parameters for certificate download: CourseId={CourseId}", courseId);
                    return new DownloadCertificateResult
                    {
                        IsSuccess = false,
                        JsonResult = _responseService.HandleJsonError("Invalid request parameters.")
                    };
                }

                var userId = user?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims for certificate download request");
                    return new DownloadCertificateResult
                    {
                        IsSuccess = false,
                        JsonResult = _responseService.HandleJsonError("Invalid request parameters.")
                    };
                }

                var isValid = await GetCachedCertificateValidation(courseId, userId);
                if (!isValid)
                {
                    _logger.LogWarning("Certificate validation failed for course {CourseId} and user {UserId}", courseId, userId);
                    return new DownloadCertificateResult
                    {
                        IsSuccess = false,
                        JsonResult = _responseService.HandleJsonError("Certificate not found.")
                    };
                }

                var printUrl = urlHelper.Action("Details", new { courseId });
                return new DownloadCertificateResult
                {
                    IsSuccess = true,
                    JsonResult = _responseService.HandleJsonSuccess(new { printUrl }, "Certificate ready for printing.")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate for course {CourseId}", courseId);
                return new DownloadCertificateResult
                {
                    IsSuccess = false,
                    JsonResult = _responseService.HandleJsonError("An error occurred while preparing the certificate.")
                };
            }
        }

        private async Task<CertificateDetailsViewModel?> GetCachedCertificateDetails(string courseId, string userId)
        {
            var cacheKey = $"CertificateDetails_{userId}_{courseId}";
            if (_cache.TryGetValue(cacheKey, out CertificateDetailsViewModel? cachedDetails))
            {
                return cachedDetails;
            }

            var details = await _certificateService.GetCertificateDetailsAsync(userId, courseId);
            if (details != null)
            {
                _cache.Set(cacheKey, details, CacheExpiration);
            }
            return details;
        }

        private async Task<bool> GetCachedCertificateValidation(string courseId, string userId)
        {
            var cacheKey = $"CertificateValid_{userId}_{courseId}";
            if (_cache.TryGetValue(cacheKey, out bool cachedValid))
            {
                return cachedValid;
            }

            var isValid = await _certificateService.ValidateCertificateAsync(userId, courseId);
            _cache.Set(cacheKey, isValid, CacheExpiration);
            return isValid;
        }
    }

    // Result classes for structured returns
    public class GetCertificatesIndexResult
    {
        public bool IsSuccess { get; set; }
        public CertificateListViewModel? CertificateList { get; set; }
        public string? UserId { get; set; }
        public bool RedirectToLogin { get; set; }
        public bool ShowErrorView { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class GetCertificateDetailsResult
    {
        public bool IsSuccess { get; set; }
        public CertificateDetailsViewModel? CertificateDetails { get; set; }
        public bool RedirectToLogin { get; set; }
        public bool RedirectToIndex { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DownloadCertificateResult
    {
        public bool IsSuccess { get; set; }
        public IActionResult? JsonResult { get; set; }
    }
}
