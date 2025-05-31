using BrainStormEra_MVC.Models;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface ICertificateRepository
    {
        Task<List<Enrollment>> GetUserCompletedEnrollmentsAsync(string userId);
        Task<Enrollment?> GetCertificateDataAsync(string userId, string courseId);
        Task<bool> HasValidCertificateAsync(string userId, string courseId);
        Task<Certificate?> GetCertificateByCodeAsync(string certificateCode);
        Task<int> GetUserCertificateCountAsync(string userId);
    }
}
