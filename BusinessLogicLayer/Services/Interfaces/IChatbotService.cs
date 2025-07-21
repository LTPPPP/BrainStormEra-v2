using DataAccessLayer.Data;
using DataAccessLayer.Models;
using BusinessLogicLayer.Services.Implementations;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IChatbotService
    {
        Task<string> GetResponseAsync(string userMessage, string userId, string? context = null);
        Task<List<ChatbotConversation>> GetConversationHistoryAsync(string userId, int limit = 20);
        Task SaveConversationAsync(ChatbotConversation conversation);
        Task<bool> RateFeedbackAsync(string conversationId, byte rating);
        // Controller-facing methods
        Task<ChatbotService.ChatControllerResult> ProcessChatForControllerAsync(ChatbotService.ChatRequest request);
        Task<ChatbotService.ChatControllerResult> GetHistoryForControllerAsync(int limit = 20);
        Task<ChatbotService.ChatControllerResult> SubmitFeedbackForControllerAsync(ChatbotService.FeedbackRequest request);
    }
}







