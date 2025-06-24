using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IChatRepo : IBaseRepo<MessageEntity>
    {
        Task<List<MessageEntity>> GetConversationMessagesAsync(string senderId, string receiverId, int page = 1, int pageSize = 50);
        Task<MessageEntity?> GetMessageByIdAsync(string messageId);
        Task<List<Conversation>> GetUserConversationsAsync(string userId);
        Task<Conversation?> GetOrCreateConversationAsync(string userId1, string userId2);
        Task<bool> MarkMessageAsReadAsync(string messageId, string userId);
        Task<int> GetUnreadMessageCountAsync(string userId, string senderId);
        Task<List<MessageEntity>> GetUnreadMessagesAsync(string userId);
        Task<bool> CanUserAccessConversationAsync(string userId, string conversationId);
        Task<List<Account>> GetChatUsersAsync(string currentUserId);
        Task<MessageEntity?> GetLastMessageBetweenUsersAsync(string userId1, string userId2); Task<bool> UpdateMessageAsync(MessageEntity message);
        Task<bool> UpdateConversationAsync(Conversation conversation);
        Task<Conversation?> GetConversationByIdAsync(string conversationId);
        Task<bool> CreateConversationParticipantAsync(ConversationParticipant participant);
    }
}
