using BusinessLogicLayer.DTOs.Common;
using BusinessLogicLayer.Services.Implementations;
using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Interface for managing chat URLs with secure hashing
    /// </summary>
    public interface IChatUrlService
    {
        /// <summary>
        /// Generate a secure chat URL for a conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="userId1">First user ID</param>
        /// <param name="userId2">Second user ID</param>
        /// <returns>Service result with the generated URL</returns>
        Task<ServiceResult<string>> GenerateChatUrlAsync(string conversationId, string userId1, string userId2);

        /// <summary>
        /// Generate a quick chat URL between two users
        /// </summary>
        /// <param name="currentUserId">Current user ID</param>
        /// <param name="targetUserId">Target user ID</param>
        /// <returns>Service result with the generated URL</returns>
        Task<ServiceResult<string>> GenerateQuickChatUrlAsync(string currentUserId, string targetUserId);

        /// <summary>
        /// Generate a message-specific URL
        /// </summary>
        /// <param name="messageId">Message ID</param>
        /// <param name="userId">User ID accessing the message</param>
        /// <returns>Service result with the generated URL</returns>
        Task<ServiceResult<string>> GenerateMessageUrlAsync(string messageId, string userId);

        /// <summary>
        /// Verify and process a secure chat URL
        /// </summary>
        /// <param name="hash">The hash from the URL</param>
        /// <param name="currentUserId">Current user ID</param>
        /// <returns>Service result with conversation details</returns>
        Task<ServiceResult<ChatUrlAccessResult>> VerifyAndAccessChatUrlAsync(string hash, string currentUserId);

        /// <summary>
        /// Verify and process a quick chat URL
        /// </summary>
        /// <param name="hash">The hash from the URL</param>
        /// <param name="currentUserId">Current user ID</param>
        /// <returns>Service result with quick chat details</returns>
        Task<ServiceResult<ChatUrlAccessResult>> VerifyAndAccessQuickChatAsync(string hash, string currentUserId);

        /// <summary>
        /// Verify and process a message URL
        /// </summary>
        /// <param name="hash">The hash from the URL</param>
        /// <param name="currentUserId">Current user ID</param>
        /// <returns>Service result with message access details</returns>
        Task<ServiceResult<MessageUrlAccessResult>> VerifyAndAccessMessageUrlAsync(string hash, string currentUserId);

        /// <summary>
        /// Generate multiple URL types for a conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="currentUserId">Current user ID</param>
        /// <param name="otherUserId">Other user ID</param>
        /// <returns>Service result with all URL types</returns>
        Task<ServiceResult<ChatUrlBundle>> GenerateChatUrlBundleAsync(string conversationId, string currentUserId, string otherUserId);
    }
}
