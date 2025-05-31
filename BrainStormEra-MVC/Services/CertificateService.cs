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
                var enrollments = await _certificateRepository.GetUserCompletedEnrollmentsAsync(userId);

                return enrollments.Select(e => new CertificateSummaryViewModel
                {
                    CourseId = e.CourseId,
                    CourseName = e.Course.CourseName,
                    CourseImage = e.Course.CourseImage ?? "/img/default-course.png",
                    AuthorName = e.Course.Author.FullName ?? e.Course.Author.Username,
                    CompletedDate = e.CertificateIssuedDate!.Value.ToDateTime(TimeOnly.MinValue),
                    EnrollmentDate = e.EnrollmentCreatedAt,
                    FinalScore = e.ProgressPercentage ?? 0
                }).ToList();
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
                var certificateData = await _certificateRepository.GetCertificateDataAsync(userId, courseId);
                if (certificateData == null) return null;

                var completionDuration = (certificateData.CertificateIssuedDate!.Value.ToDateTime(TimeOnly.MinValue)
                                        - certificateData.EnrollmentCreatedAt).TotalDays;

                return new CertificateDetailsViewModel
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
                    CertificateCode = await GenerateCertificateCodeAsync(courseId, userId)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certificate details for user {UserId}, course {CourseId}", userId, courseId);
                return null;
            }
        }
        public async Task<bool> ValidateCertificateAsync(string userId, string courseId)
        {
            return await _certificateRepository.HasValidCertificateAsync(userId, courseId);
        }

        public async Task<string> GenerateCertificateCodeAsync(string courseId, string userId)
        {
            return await Task.FromResult($"BSE-{courseId.Substring(0, Math.Min(8, courseId.Length)).ToUpper()}-{DateTime.Now.Year}");
        }
    }
}
