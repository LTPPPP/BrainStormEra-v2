using DataAccessLayer.Data;
using DataAccessLayer.Models;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface IChatbotService
    {
        Task<string> GetResponseAsync(string userMessage, string userId, string? context = null);
        Task<List<ChatbotConversation>> GetConversationHistoryAsync(string userId, int limit = 20);
        Task SaveConversationAsync(ChatbotConversation conversation);
        Task<bool> RateFeedbackAsync(string conversationId, byte rating);
    }
}
