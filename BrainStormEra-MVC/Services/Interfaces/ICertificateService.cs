using BrainStormEra_MVC.Models.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface ICertificateService
    {
        Task<List<CertificateSummaryViewModel>> GetUserCertificatesAsync(string userId);
        Task<CertificateDetailsViewModel?> GetCertificateDetailsAsync(string userId, string courseId);
        Task<bool> ValidateCertificateAsync(string userId, string courseId);
        Task<string> GenerateCertificateCodeAsync(string courseId, string userId);
        Task InvalidateCacheAsync(string userId);
        Task<List<CertificateSummaryViewModel>> GetCachedUserCertificatesAsync(string userId);
    }
}
