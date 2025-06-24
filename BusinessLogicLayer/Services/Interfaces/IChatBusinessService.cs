using BusinessLogicLayer.DTOs.Chat;
using BusinessLogicLayer.DTOs.Common;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IChatBusinessService
    {
        Task<ServiceResult<ChatIndexViewModel>> GetChatIndexViewModelAsync(string currentUserId);
        Task<ServiceResult<ConversationViewModel>> GetConversationViewModelAsync(string currentUserId, string receiverId);
        Task<ServiceResult<List<ChatMessageDTO>>> GetMessagesAsync(string currentUserId, GetMessagesRequest request);
        Task<ServiceResult<ChatMessageDTO>> SendMessageAsync(string currentUserId, SendMessageRequest request);
        Task<ServiceResult> MarkMessageAsReadAsync(string currentUserId, MarkAsReadRequest request);
        Task<ServiceResult<int>> GetUnreadCountAsync(string currentUserId, GetUnreadCountRequest request);
        Task<ServiceResult> DeleteMessageAsync(string currentUserId, DeleteMessageRequest request);
        Task<ServiceResult> EditMessageAsync(string currentUserId, EditMessageRequest request);
    }
}
