using DataAccessLayer.Models.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface ICertificateService
    {
        Task<List<CertificateSummaryViewModel>> GetUserCertificatesAsync(string userId);
        Task<CertificateListViewModel> GetUserCertificatesAsync(string userId, string? search, int page, int pageSize);
        Task<CertificateDetailsViewModel?> GetCertificateDetailsAsync(string userId, string courseId);
        Task<bool> ValidateCertificateAsync(string userId, string courseId);
        Task<string> GenerateCertificateCodeAsync(string courseId, string userId);
        Task InvalidateCacheAsync(string userId);
        Task<List<CertificateSummaryViewModel>> GetCachedUserCertificatesAsync(string userId);
    }
}







