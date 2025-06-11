using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace DataAccessLayer.Repositories
{
    public class ChatbotRepo : BaseRepo<ChatbotConversation>, IChatbotRepo
    {
        private readonly ILogger<ChatbotRepo> _logger;

        public ChatbotRepo(BrainStormEraContext context, ILogger<ChatbotRepo> logger)
            : base(context)
        {
            _logger = logger;
        }

        // Conversation Management
        public async Task<ChatbotConversation> CreateConversationAsync(ChatbotConversation conversation)
        {
            try
            {
                if (string.IsNullOrEmpty(conversation.ConversationId))
                {
                    conversation.ConversationId = Guid.NewGuid().ToString();
                }
                conversation.ConversationTime = DateTime.UtcNow;

                await AddAsync(conversation);
                return conversation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation for user {UserId}", conversation.UserId);
                throw;
            }
        }

        public async Task<ChatbotConversation?> GetConversationByIdAsync(string conversationId)
        {
            try
            {
                return await GetQueryable()
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<IEnumerable<ChatbotConversation>> GetUserConversationsAsync(string userId, int page = 1, int pageSize = 10)
        {
            try
            {
                return await GetQueryable()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.ConversationTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(c => c.User)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUserConversationsCountAsync(string userId)
        {
            try
            {
                return await CountAsync(c => c.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting conversations for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateConversationAsync(ChatbotConversation conversation)
        {
            try
            {
                await UpdateAsync(conversation);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation {ConversationId}", conversation.ConversationId);
                return false;
            }
        }

        public async Task<bool> DeleteConversationAsync(string conversationId)
        {
            try
            {
                var conversation = await GetByIdAsync(conversationId);
                if (conversation != null)
                {
                    await DeleteAsync(conversation);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
                return false;
            }
        }

        public async Task<bool> DeleteUserConversationsAsync(string userId)
        {
            try
            {
                var conversations = await FindAsync(c => c.UserId == userId);
                if (conversations.Any())
                {
                    await DeleteRangeAsync(conversations);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversations for user {UserId}", userId);
                return false;
            }
        }

        // Message History
        public async Task<IEnumerable<ChatbotConversation>> GetConversationHistoryAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = GetQueryable().Where(c => c.UserId == userId);

                if (fromDate.HasValue)
                    query = query.Where(c => c.ConversationTime >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(c => c.ConversationTime <= toDate.Value);

                return await query
                    .OrderByDescending(c => c.ConversationTime)
                    .Include(c => c.User)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ChatbotConversation?> GetLastConversationAsync(string userId)
        {
            try
            {
                return await GetQueryable()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.ConversationTime)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last conversation for user {UserId}", userId);
                throw;
            }
        }

        // Analytics & Statistics
        public async Task<int> GetTotalConversationsAsync()
        {
            try
            {
                return await CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total conversations count");
                throw;
            }
        }

        public async Task<int> GetTotalConversationsAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return await CountAsync(c => c.ConversationTime >= fromDate && c.ConversationTime <= toDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations count for date range");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetConversationsPerUserAsync()
        {
            try
            {
                return await GetQueryable()
                    .GroupBy(c => c.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations per user");
                throw;
            }
        }

        public async Task<Dictionary<DateTime, int>> GetConversationsPerDayAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return await GetQueryable()
                    .Where(c => c.ConversationTime >= fromDate && c.ConversationTime <= toDate)
                    .GroupBy(c => c.ConversationTime.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Date, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations per day");
                throw;
            }
        }

        // Feedback & Rating
        public async Task<bool> UpdateFeedbackRatingAsync(string conversationId, byte rating)
        {
            try
            {
                var conversation = await GetByIdAsync(conversationId);
                if (conversation != null)
                {
                    conversation.FeedbackRating = rating;
                    await UpdateAsync(conversation);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feedback rating for conversation {ConversationId}", conversationId);
                return false;
            }
        }

        public async Task<IEnumerable<ChatbotConversation>> GetConversationsWithFeedbackAsync()
        {
            try
            {
                return await FindAsync(c => c.FeedbackRating.HasValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations with feedback");
                throw;
            }
        }

        public async Task<double> GetAverageRatingAsync()
        {
            try
            {
                var conversations = await GetQueryable()
                    .Where(c => c.FeedbackRating.HasValue)
                    .ToListAsync();

                return conversations.Any() ? conversations.Average(c => c.FeedbackRating!.Value) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average rating");
                throw;
            }
        }

        public async Task<Dictionary<byte, int>> GetRatingDistributionAsync()
        {
            try
            {
                return await GetQueryable()
                    .Where(c => c.FeedbackRating.HasValue)
                    .GroupBy(c => c.FeedbackRating!.Value)
                    .Select(g => new { Rating = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Rating, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rating distribution");
                throw;
            }
        }

        // Search & Filtering
        public async Task<IEnumerable<ChatbotConversation>> SearchConversationsAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            try
            {
                return await GetQueryable()
                    .Where(c => c.UserMessage.Contains(searchTerm) || c.BotResponse.Contains(searchTerm))
                    .OrderByDescending(c => c.ConversationTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(c => c.User)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching conversations with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<int> SearchConversationsCountAsync(string searchTerm)
        {
            try
            {
                return await CountAsync(c => c.UserMessage.Contains(searchTerm) || c.BotResponse.Contains(searchTerm));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting search conversations with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<ChatbotConversation>> GetConversationsByContextAsync(string context)
        {
            try
            {
                return await FindAsync(c => c.ConversationContext != null && c.ConversationContext.Contains(context));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations by context {Context}", context);
                throw;
            }
        }

        // Bulk Operations
        public async Task<bool> BulkDeleteConversationsAsync(IEnumerable<string> conversationIds)
        {
            try
            {
                var conversations = await GetQueryable()
                    .Where(c => conversationIds.Contains(c.ConversationId))
                    .ToListAsync();

                if (conversations.Any())
                {
                    await DeleteRangeAsync(conversations);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting conversations");
                return false;
            }
        }

        public async Task<bool> BulkUpdateContextAsync(IEnumerable<string> conversationIds, string newContext)
        {
            try
            {
                var conversations = await GetQueryable()
                    .Where(c => conversationIds.Contains(c.ConversationId))
                    .ToListAsync();

                if (conversations.Any())
                {
                    foreach (var conversation in conversations)
                    {
                        conversation.ConversationContext = newContext;
                    }
                    await UpdateRangeAsync(conversations);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating conversation context");
                return false;
            }
        }

        // Export & Backup
        public async Task<IEnumerable<ChatbotConversation>> ExportAllConversationsAsync()
        {
            try
            {
                return await GetQueryable()
                    .Include(c => c.User)
                    .OrderBy(c => c.ConversationTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting all conversations");
                throw;
            }
        }

        public async Task<IEnumerable<ChatbotConversation>> ExportUserConversationsAsync(string userId)
        {
            try
            {
                return await GetQueryable()
                    .Where(c => c.UserId == userId)
                    .Include(c => c.User)
                    .OrderBy(c => c.ConversationTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting conversations for user {UserId}", userId);
                throw;
            }
        }

        // Missing methods implementation
        public async Task<bool> SaveConversationAsync(ChatbotConversation conversation)
        {
            try
            {
                await AddAsync(conversation);
                await SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversation");
                return false;
            }
        }

        public async Task<List<ChatbotConversation>> GetConversationHistoryAsync(string userId, int limit)
        {
            try
            {
                return await GetQueryable()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.ConversationTime)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for user {UserId} with limit {Limit}", userId, limit);
                throw;
            }
        }
    }
}
