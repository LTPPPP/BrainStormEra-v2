using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IChatService
    {
        Task<List<MessageEntity>> GetConversationMessagesAsync(string senderId, string receiverId, int page = 1, int pageSize = 50);
        Task<MessageEntity?> SendMessageAsync(string senderId, string receiverId, string message, string? replyToMessageId = null);
        Task<bool> MarkMessageAsReadAsync(string messageId, string userId);
        Task<MessageEntity?> GetMessageByIdAsync(string messageId);
        Task<List<Conversation>> GetUserConversationsAsync(string userId);
        Task<int> GetUnreadMessageCountAsync(string userId, string senderId);
        Task<List<MessageEntity>> GetUnreadMessagesAsync(string userId);
        Task<bool> CanUserAccessConversationAsync(string userId, string conversationId);
        Task<List<Account>> GetChatUsersAsync(string currentUserId);
        Task<MessageEntity?> GetLastMessageBetweenUsersAsync(string userId1, string userId2);
        Task<bool> SetUserOnlineStatusAsync(string userId, bool isOnline);
        Task<Conversation?> GetOrCreateConversationAsync(string userId1, string userId2);
        Task<bool> DeleteMessageAsync(string messageId, string userId);
        Task<bool> EditMessageAsync(string messageId, string newContent, string userId);
    }
}
