using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using DataAccessLayer.Models.ViewModels;

namespace BrainStormEra_Razor.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class ChatbotHistoryModel : PageModel
    {
        private readonly ILogger<ChatbotHistoryModel> _logger;
        private readonly AdminService _adminService;

        public ChatbotHistoryViewModel ChatbotHistoryData { get; set; } = new ChatbotHistoryViewModel();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        public ChatbotHistoryModel(ILogger<ChatbotHistoryModel> logger, AdminService adminService)
        {
            _logger = logger;
            _adminService = adminService;
        }

        public async Task OnGetAsync()
        {
            try
            {
                var result = await _adminService.GetChatbotHistoryAsync(User, Search, UserId, FromDate, ToDate, Page, PageSize);

                if (result.IsSuccess)
                {
                    ChatbotHistoryData = result.Data ?? new ChatbotHistoryViewModel();
                    SuccessMessage = result.Message;
                }
                else
                {
                    ErrorMessage = result.Message;
                    ChatbotHistoryData = new ChatbotHistoryViewModel();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chatbot history page");
                ErrorMessage = "An error occurred while loading chatbot history data.";
                ChatbotHistoryData = new ChatbotHistoryViewModel();
            }
        }
    }
}