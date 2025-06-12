using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface ICertificateRepo : IBaseRepo<Certificate>
    {
        // Certificate query methods
        Task<List<Certificate>> GetUserCertificatesAsync(string userId);
        Task<Certificate?> GetCertificateByCourseAsync(string userId, string courseId);
        Task<List<Certificate>> GetCertificatesByCourseAsync(string courseId);
        Task<Certificate?> GetCertificateWithDetailsAsync(string certificateId);
        Task<Enrollment?> GetCertificateDataAsync(string userId, string courseId);
        Task<bool> HasValidCertificateAsync(string userId, string courseId);
        Task<List<Enrollment>> GetUserCompletedEnrollmentsAsync(string userId, string? search, int page, int pageSize);
        Task<int> GetUserCompletedEnrollmentsCountAsync(string userId, string? search);

        // Certificate generation and management
        Task<string> GenerateCertificateAsync(string userId, string courseId);
        Task<bool> UpdateCertificateAsync(Certificate certificate);
        Task<bool> DeleteCertificateAsync(string certificateId, string userId);
        Task<bool> RegenerateCertificateAsync(string certificateId);

        // Certificate validation and verification
        Task<Certificate?> VerifyCertificateAsync(string certificateId, string verificationCode);
        Task<bool> IsCertificateValidAsync(string certificateId);
        Task<bool> HasUserEarnedCertificateAsync(string userId, string courseId);
        Task<string> GenerateVerificationCodeAsync(string certificateId);

        // Certificate download and export
        Task<byte[]?> GetCertificatePdfAsync(string certificateId);
        Task<bool> UpdateCertificateFilePathAsync(string certificateId, string filePath);
        Task<string?> GetCertificateFilePathAsync(string certificateId);

        // Certificate statistics
        Task<int> GetUserCertificateCountAsync(string userId);
        Task<int> GetCourseCertificateCountAsync(string courseId);
        Task<List<Certificate>> GetRecentCertificatesAsync(int count = 10);
        Task<decimal> GetCertificateCompletionRateAsync(string courseId);

        // Certificate templates and customization
        Task<bool> UpdateCertificateTemplateAsync(string courseId, string templatePath);
        Task<string?> GetCertificateTemplateAsync(string courseId);
        Task<bool> CustomizeCertificateAsync(string certificateId, Dictionary<string, string> customFields);

        // Certificate sharing and social features
        Task<bool> ShareCertificateAsync(string certificateId, string platform);
        Task<List<Certificate>> GetPublicCertificatesAsync(string userId);
        Task<bool> UpdateCertificateVisibilityAsync(string certificateId, bool isPublic);

        // Certificate expiration and renewal
        Task<List<Certificate>> GetExpiringCertificatesAsync(int daysBeforeExpiry = 30);
        Task<bool> RenewCertificateAsync(string certificateId);
        Task<bool> UpdateCertificateExpiryAsync(string certificateId, DateTime? expiryDate);
        Task<List<Certificate>> GetExpiredCertificatesAsync();

        // Certificate search and filtering
        Task<List<Certificate>> SearchCertificatesAsync(string searchTerm, string? userId = null);
        Task<List<Certificate>> GetCertificatesByDateRangeAsync(DateTime startDate, DateTime endDate, string? userId = null);
        Task<List<Certificate>> GetCertificatesByInstructorAsync(string instructorId);
    }
}
