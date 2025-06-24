using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using BusinessLogicLayer.Services.Interfaces;

namespace BusinessLogicLayer.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                // Mark user as online
                await _chatService.SetUserOnlineStatusAsync(userId, true);

                _logger.LogInformation($"User {userId} connected to ChatHub with connection {Context.ConnectionId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                // Mark user as offline
                await _chatService.SetUserOnlineStatusAsync(userId, false);

                _logger.LogInformation($"User {userId} disconnected from ChatHub");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string receiverId, string message, string? replyToMessageId = null)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (senderId == null) return;

            try
            {
                var messageEntity = await _chatService.SendMessageAsync(senderId, receiverId, message, replyToMessageId); if (messageEntity != null)
                {
                    // Send directly to receiver (not using group to avoid issues)
                    await Clients.User(receiverId).SendAsync("ReceiveMessage", new
                    {
                        messageId = messageEntity.MessageId,
                        senderId = messageEntity.SenderId,
                        receiverId = messageEntity.ReceiverId,
                        content = messageEntity.MessageContent,
                        messageType = messageEntity.MessageType,
                        replyToMessageId = messageEntity.ReplyToMessageId,
                        createdAt = messageEntity.MessageCreatedAt,
                        senderName = messageEntity.Sender?.Username ?? "Unknown",
                        senderAvatar = messageEntity.Sender?.UserImage
                    });

                    // Send confirmation to sender
                    await Clients.Caller.SendAsync("MessageSent", new
                    {
                        messageId = messageEntity.MessageId,
                        senderId = messageEntity.SenderId,
                        receiverId = messageEntity.ReceiverId,
                        content = messageEntity.MessageContent,
                        messageType = messageEntity.MessageType,
                        replyToMessageId = messageEntity.ReplyToMessageId,
                        createdAt = messageEntity.MessageCreatedAt
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message from {senderId} to {receiverId}");
                await Clients.Caller.SendAsync("MessageError", "Failed to send message");
            }
        }

        public async Task MarkMessageAsRead(string messageId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;

            try
            {
                await _chatService.MarkMessageAsReadAsync(messageId, userId);                // Notify sender that message was read
                var message = await _chatService.GetMessageByIdAsync(messageId);
                if (message != null)
                {
                    await Clients.User(message.SenderId).SendAsync("MessageRead", new
                    {
                        messageId,
                        readerId = userId,
                        readAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking message {messageId} as read by {userId}");
            }
        }

        public async Task StartTyping(string receiverId)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (senderId == null) return;

            await Clients.User(receiverId).SendAsync("UserStartedTyping", senderId);
        }

        public async Task StopTyping(string receiverId)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (senderId == null) return;

            await Clients.User(receiverId).SendAsync("UserStoppedTyping", senderId);
        }
    }
}
