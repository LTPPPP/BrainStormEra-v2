using Microsoft.Extensions.Logging;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using BusinessLogicLayer.Constants;

namespace BusinessLogicLayer.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly ICertificateRepo _certificateRepo;
        private readonly ICourseRepo _courseRepo;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CertificateService> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

        public CertificateService(
            ICertificateRepo certificateRepo,
            ICourseRepo courseRepo,
            IMemoryCache cache,
            ILogger<CertificateService> logger)
        {
            _certificateRepo = certificateRepo;
            _courseRepo = courseRepo;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<CertificateSummaryViewModel>> GetUserCertificatesAsync(string userId)
        {
            try
            {
                var cacheKey = $"UserCertificates_{userId}";
                if (_cache.TryGetValue(cacheKey, out List<CertificateSummaryViewModel>? cached))
                    return cached!;

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

                _cache.Set(cacheKey, result, CacheExpiration);
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
                var cacheKey = $"CertificateDetails_{userId}_{courseId}";
                if (_cache.TryGetValue(cacheKey, out CertificateDetailsViewModel? cached))
                    return cached;

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

                _cache.Set(cacheKey, result, CacheExpiration);
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
            var cacheKey = $"CertificateValid_{userId}_{courseId}";
            if (_cache.TryGetValue(cacheKey, out bool cached))
                return cached;

            var result = await _certificateRepo.HasValidCertificateAsync(userId, courseId);
            _cache.Set(cacheKey, result, CacheExpiration);
            return result;
        }

        public async Task<string> GenerateCertificateCodeAsync(string courseId, string userId)
        {
            return await Task.FromResult($"BSE-{courseId.Substring(0, Math.Min(8, courseId.Length)).ToUpper()}-{DateTime.Now.Year}");
        }

        public async Task InvalidateCacheAsync(string userId)
        {
            var patterns = new[]
            {
                $"UserCertificates_{userId}",
                $"CertificateDetails_{userId}_",
                $"CertificateValid_{userId}_"
            };

            await Task.Run(() =>
            {
                foreach (var pattern in patterns)
                {
                    _cache.Remove(pattern);
                }
            });
        }

        public async Task<List<CertificateSummaryViewModel>> GetCachedUserCertificatesAsync(string userId)
        {
            var cacheKey = $"UserCertificates_{userId}";
            if (_cache.TryGetValue(cacheKey, out List<CertificateSummaryViewModel>? cached))
                return cached!;

            return await GetUserCertificatesAsync(userId);
        }

        public async Task<CertificateListViewModel> GetUserCertificatesAsync(string userId, string? search, int page, int pageSize)
        {
            try
            {
                var cacheKey = $"UserCertificatesPaginated_{userId}_{search}_{page}_{pageSize}";
                if (_cache.TryGetValue(cacheKey, out CertificateListViewModel? cached))
                    return cached!;

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

                _cache.Set(cacheKey, result, CacheExpiration);
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
    }
}








