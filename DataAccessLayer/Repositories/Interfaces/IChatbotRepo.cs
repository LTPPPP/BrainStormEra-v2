using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Models;
using System.Linq.Expressions;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IChatbotRepo : IBaseRepo<ChatbotConversation>
    {
        // Conversation Management
        Task<ChatbotConversation> CreateConversationAsync(ChatbotConversation conversation);
        Task<ChatbotConversation?> GetConversationByIdAsync(string conversationId);
        Task<IEnumerable<ChatbotConversation>> GetUserConversationsAsync(string userId, int page = 1, int pageSize = 10);
        Task<int> GetUserConversationsCountAsync(string userId);
        Task<bool> UpdateConversationAsync(ChatbotConversation conversation);
        Task<bool> DeleteConversationAsync(string conversationId);
        Task<bool> DeleteUserConversationsAsync(string userId);
        Task<bool> SaveConversationAsync(ChatbotConversation conversation);

        // Message History
        Task<IEnumerable<ChatbotConversation>> GetConversationHistoryAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<ChatbotConversation>> GetConversationHistoryAsync(string userId, int limit);
        Task<ChatbotConversation?> GetLastConversationAsync(string userId);

        // Analytics & Statistics
        Task<int> GetTotalConversationsAsync();
        Task<int> GetTotalConversationsAsync(DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, int>> GetConversationsPerUserAsync();
        Task<Dictionary<DateTime, int>> GetConversationsPerDayAsync(DateTime fromDate, DateTime toDate);

        // Feedback & Rating
        Task<bool> UpdateFeedbackRatingAsync(string conversationId, byte rating);
        Task<IEnumerable<ChatbotConversation>> GetConversationsWithFeedbackAsync();
        Task<double> GetAverageRatingAsync();
        Task<Dictionary<byte, int>> GetRatingDistributionAsync();

        // Search & Filtering
        Task<IEnumerable<ChatbotConversation>> SearchConversationsAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<int> SearchConversationsCountAsync(string searchTerm);
        Task<IEnumerable<ChatbotConversation>> GetConversationsByContextAsync(string context);

        // Bulk Operations
        Task<bool> BulkDeleteConversationsAsync(IEnumerable<string> conversationIds);
        Task<bool> BulkUpdateContextAsync(IEnumerable<string> conversationIds, string newContext);

        // Export & Backup
        Task<IEnumerable<ChatbotConversation>> ExportAllConversationsAsync();
        Task<IEnumerable<ChatbotConversation>> ExportUserConversationsAsync(string userId);
    }
}
