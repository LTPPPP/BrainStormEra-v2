using DataAccessLayer.Data;
using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface ICertificateRepository
    {
        Task<List<Enrollment>> GetUserCompletedEnrollmentsAsync(string userId);
        Task<List<Enrollment>> GetUserCompletedEnrollmentsAsync(string userId, string? search, int page, int pageSize);
        Task<int> GetUserCompletedEnrollmentsCountAsync(string userId, string? search);
        Task<Enrollment?> GetCertificateDataAsync(string userId, string courseId);
        Task<bool> HasValidCertificateAsync(string userId, string courseId);
        Task<Certificate?> GetCertificateByCodeAsync(string certificateCode);
        Task<int> GetUserCertificateCountAsync(string userId);
    }
}







