using Microsoft.Extensions.Logging;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using BusinessLogicLayer.Constants;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using System;
using System.Linq;

namespace BusinessLogicLayer.Services.Implementations
{
    public class CertificateService : ICertificateService
    {
        private readonly ICertificateRepo _certificateRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly ILogger<CertificateService> _logger;
        private readonly BrainStormEraContext _context;
        private readonly IUserContextService _userContextService;
        private readonly IResponseService _responseService;

        public CertificateService(
            ICertificateRepo certificateRepo,
            ICourseRepo courseRepo,
            ILogger<CertificateService> logger,
            BrainStormEraContext context,
            IUserContextService userContextService,
            IResponseService responseService)
        {
            _certificateRepo = certificateRepo;
            _courseRepo = courseRepo;
            _logger = logger;
            _context = context;
            _userContextService = userContextService;
            _responseService = responseService;
        }

        public async Task<List<CertificateSummaryViewModel>> GetUserCertificatesAsync(string userId)
        {
            try
            {
                var enrollments = await _certificateRepo.GetUserCompletedEnrollmentsAsync(userId, null, 1, int.MaxValue);
                var result = enrollments.Select(e => new CertificateSummaryViewModel
                {
                    CourseId = e.CourseId,
                    CourseName = e.Course.CourseName,
                    CourseImage = e.Course.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    AuthorName = e.Course.Author.FullName ?? e.Course.Author.Username,
                    CompletedDate = e.CertificateIssuedDate!.Value.ToDateTime(TimeOnly.MinValue),
                    EnrollmentDate = e.EnrollmentCreatedAt,
                    FinalScore = e.ProgressPercentage ?? 0
                }).ToList();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certificates for user {UserId}", userId);
                return new List<CertificateSummaryViewModel>();
            }
        }

        public async Task<CertificateDetailsViewModel?> GetCertificateDetailsAsync(string userId, string courseId)
        {
            try
            {
                var certificateData = await _certificateRepo.GetCertificateDataAsync(userId, courseId);
                if (certificateData == null) return null;

                var completionDuration = (certificateData.CertificateIssuedDate!.Value.ToDateTime(TimeOnly.MinValue)
                                        - certificateData.EnrollmentCreatedAt).TotalDays;

                var result = new CertificateDetailsViewModel
                {
                    CourseId = certificateData.CourseId,
                    CourseName = certificateData.Course.CourseName,
                    CourseDescription = certificateData.Course.CourseDescription ?? "",
                    CourseImage = certificateData.Course.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    LearnerName = certificateData.User.FullName ?? certificateData.User.Username,
                    LearnerEmail = certificateData.User.UserEmail,
                    InstructorName = certificateData.Course.Author.FullName ?? certificateData.Course.Author.Username,
                    CompletedDate = certificateData.CertificateIssuedDate.Value.ToDateTime(TimeOnly.MinValue),
                    EnrollmentDate = certificateData.EnrollmentCreatedAt,
                    CompletionDurationDays = Math.Max(1, (int)Math.Round(completionDuration)),
                    FinalScore = certificateData.ProgressPercentage ?? 0,
                    CertificateCode = await GenerateCertificateCodeAsync(courseId, userId)
                };
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certificate details for user {UserId}, course {CourseId}", userId, courseId);
                return null;
            }
        }

        public async Task<bool> ValidateCertificateAsync(string userId, string courseId)
        {
            var result = await _certificateRepo.HasValidCertificateAsync(userId, courseId);
            return result;
        }

        public async Task<string> GenerateCertificateCodeAsync(string courseId, string userId)
        {
            return await Task.FromResult($"BSE-{courseId.Substring(0, Math.Min(8, courseId.Length)).ToUpper()}-{DateTime.Now.Year}");
        }

        public async Task InvalidateCacheAsync(string userId)
        {
            // Method removed: no longer needed
        }

        public async Task<List<CertificateSummaryViewModel>> GetCachedUserCertificatesAsync(string userId)
        {
            // Method removed: no longer needed
            return await GetUserCertificatesAsync(userId);
        }

        public async Task<CertificateListViewModel> GetUserCertificatesAsync(string userId, string? search, int page, int pageSize)
        {
            try
            {
                var enrollments = await _certificateRepo.GetUserCompletedEnrollmentsAsync(userId, search, page, pageSize);
                var totalCount = await _certificateRepo.GetUserCompletedEnrollmentsCountAsync(userId, search);

                var certificates = enrollments.Select(e => new CertificateSummaryViewModel
                {
                    CourseId = e.CourseId,
                    CourseName = e.Course.CourseName,
                    CourseImage = e.Course.CourseImage ?? MediaConstants.Defaults.DefaultCoursePath,
                    AuthorName = e.Course.Author.FullName ?? e.Course.Author.Username,
                    CompletedDate = e.CertificateIssuedDate!.Value.ToDateTime(TimeOnly.MinValue),
                    EnrollmentDate = e.EnrollmentCreatedAt,
                    FinalScore = e.ProgressPercentage ?? 0
                }).ToList();

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var result = new CertificateListViewModel
                {
                    Certificates = certificates,
                    SearchQuery = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCertificates = totalCount,
                    TotalPages = totalPages
                };
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated certificates for user {UserId}", userId);
                return new CertificateListViewModel
                {
                    Certificates = new List<CertificateSummaryViewModel>(),
                    SearchQuery = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCertificates = 0,
                    TotalPages = 0
                };
            }
        }

        public async Task<bool> ProcessPendingCertificatesAsync(string userId)
        {
            try
            {
                // Get all enrollments with 100% progress but no certificate
                var pendingEnrollments = await _context.Enrollments
                    .Include(e => e.Course)
                    .Where(e => e.UserId == userId &&
                               e.ProgressPercentage >= 100 &&
                               !e.CertificateIssuedDate.HasValue)
                    .ToListAsync();

                var certificatesIssued = 0;

                foreach (var enrollment in pendingEnrollments)
                {
                    // Update enrollment status and certificate date
                    enrollment.EnrollmentStatus = 3; // Completed
                    enrollment.CertificateIssuedDate = DateOnly.FromDateTime(DateTime.UtcNow);

                    // Create certificate record
                    var certificate = new Certificate
                    {
                        CertificateId = Guid.NewGuid().ToString(),
                        EnrollmentId = enrollment.EnrollmentId,
                        UserId = userId,
                        CourseId = enrollment.CourseId,
                        CertificateCode = GenerateCertificateCode(),
                        CertificateName = "Certificate of Completion",
                        IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        IsValid = true,
                        FinalScore = enrollment.ProgressPercentage ?? 100,
                        CertificateCreatedAt = DateTime.UtcNow
                    };

                    _context.Certificates.Add(certificate);
                    certificatesIssued++;
                }

                if (certificatesIssued > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Issued {Count} certificates for user {UserId}", certificatesIssued, userId);

                    // Invalidate cache for this user
                    await InvalidateCacheAsync(userId);
                }

                return certificatesIssued > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending certificates for user {UserId}", userId);
                return false;
            }
        }

        private string GenerateCertificateCode()
        {
            return Guid.NewGuid().ToString("N")[..8].ToUpper();
        }

        // Additional methods for the wrapper functionality
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

                var certificateList = await GetUserCertificatesAsync(userId, search, page, pageSize);

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

                var certificateDetails = await GetCertificateDetailsAsync(userId, courseId);
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

                var isValid = await ValidateCertificateAsync(userId, courseId);
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
            // Method removed: no longer needed
            return await GetCertificateDetailsAsync(userId, courseId);
        }

        private async Task<bool> GetCachedCertificateValidation(string courseId, string userId)
        {
            // Method removed: no longer needed
            return await ValidateCertificateAsync(userId, courseId);
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








