using Microsoft.AspNetCore.SignalR;
using BrainStormEra_MVC.Models;
using System.Threading.Tasks;
using System;

namespace BrainStormEra_MVC.Hubs
{
    public class ChatHub : Hub
    {
        private readonly BrainStormEraContext _context;

        public ChatHub(BrainStormEraContext context)
        {
            _context = context;
        }

        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task SendMessage(string conversationId, string senderId, string receiverId, string content)
        {
            // Create a new message
            var messageId = Guid.NewGuid().ToString();
            var message = new MessageEntity
            {
                MessageId = messageId,
                SenderId = senderId,
                ReceiverId = receiverId,
                ConversationId = conversationId,
                MessageContent = content,
                MessageType = "TEXT",
                IsRead = false,
                MessageCreatedAt = DateTime.UtcNow,
                MessageUpdatedAt = DateTime.UtcNow
            };

            // Save to database
            _context.Add(message);

            // Update conversation last message
            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.LastMessageId = messageId;
                conversation.LastMessageAt = DateTime.UtcNow;
                conversation.ConversationUpdatedAt = DateTime.UtcNow;
                _context.Update(conversation);
            }

            await _context.SaveChangesAsync();

            // Broadcast to all clients in the conversation group
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", message);
        }

        public async Task MarkMessageAsRead(string messageId, string userId)
        {
            var message = await _context.MessageEntities.FindAsync(messageId);
            if (message != null && message.ReceiverId == userId && message.IsRead != true)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                _context.Update(message);
                await _context.SaveChangesAsync();

                // Notify sender that message has been read
                await Clients.User(message.SenderId).SendAsync("MessageRead", messageId);
            }
        }
    }
}
