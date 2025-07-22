using DataAccessLayer.Models.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using BusinessLogicLayer.Services.Implementations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace BusinessLogicLayer.Services.Interfaces
{
    // Move result types to the interface namespace for direct reference
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

    public interface ICertificateService
    {
        Task<List<CertificateSummaryViewModel>> GetUserCertificatesAsync(string userId);
        Task<CertificateListViewModel> GetUserCertificatesAsync(string userId, string? search, int page, int pageSize);
        Task<CertificateDetailsViewModel?> GetCertificateDetailsAsync(string userId, string courseId);
        Task<bool> ValidateCertificateAsync(string userId, string courseId);
        Task<string> GenerateCertificateCodeAsync(string courseId, string userId);
        Task InvalidateCacheAsync(string userId);
        Task<List<CertificateSummaryViewModel>> GetCachedUserCertificatesAsync(string userId);
        Task<bool> ProcessPendingCertificatesAsync(string userId);
        // Controller-facing methods
        Task<GetCertificatesIndexResult> GetCertificatesIndexAsync(ClaimsPrincipal user, string? search, int page, int pageSize);
        Task<GetCertificateDetailsResult> GetCertificateDetailsAsync(ClaimsPrincipal user, string courseId);
        Task<DownloadCertificateResult> DownloadCertificateAsync(ClaimsPrincipal user, string courseId, IUrlHelper urlHelper);
    }
}







