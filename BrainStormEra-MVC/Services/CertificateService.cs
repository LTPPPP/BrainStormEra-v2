using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BrainStormEra_MVC.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly ICertificateRepository _certificateRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CertificateService> _logger;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

        public CertificateService(
            ICertificateRepository certificateRepository,
            IMemoryCache cache,
            ILogger<CertificateService> logger)
        {
            _certificateRepository = certificateRepository;
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

                var enrollments = await _certificateRepository.GetUserCompletedEnrollmentsAsync(userId);
                var result = enrollments.Select(e => new CertificateSummaryViewModel
                {
                    CourseId = e.CourseId,
                    CourseName = e.Course.CourseName,
                    CourseImage = e.Course.CourseImage ?? "/img/defaults/default-course.svg",
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

                var certificateData = await _certificateRepository.GetCertificateDataAsync(userId, courseId);
                if (certificateData == null) return null;

                var completionDuration = (certificateData.CertificateIssuedDate!.Value.ToDateTime(TimeOnly.MinValue)
                                        - certificateData.EnrollmentCreatedAt).TotalDays;

                var result = new CertificateDetailsViewModel
                {
                    CourseId = certificateData.CourseId,
                    CourseName = certificateData.Course.CourseName,
                    CourseDescription = certificateData.Course.CourseDescription ?? "",
                    CourseImage = certificateData.Course.CourseImage ?? "/img/defaults/default-course.svg",
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

            var result = await _certificateRepository.HasValidCertificateAsync(userId, courseId);
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
    }
}
