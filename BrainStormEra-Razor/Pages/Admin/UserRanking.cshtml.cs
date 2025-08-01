using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class UserRankingModel : PageModel
    {
        private readonly ILogger<UserRankingModel> _logger;
        private readonly IAdminService _adminService;

        public UserRankingViewModel UserRankingData { get; set; } = new UserRankingViewModel();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public new int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        public UserRankingModel(ILogger<UserRankingModel> logger, IAdminService adminService)
        {
            _logger = logger;
            _adminService = adminService;
        }

        public async Task OnGetAsync()
        {
            try
            {
                var result = await _adminService.GetUserRankingAsync(Page, PageSize);

                if (result != null)
                {
                    UserRankingData = result;
                    SuccessMessage = "User ranking loaded successfully.";
                }
                else
                {
                    ErrorMessage = "Failed to load user ranking data.";
                    UserRankingData = new UserRankingViewModel();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user ranking page");
                ErrorMessage = "An error occurred while loading user ranking data.";
                UserRankingData = new UserRankingViewModel();
            }
        }
    }
}