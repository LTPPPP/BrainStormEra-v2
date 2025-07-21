using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataAccessLayer.Repositories
{
    public class ChatRepo : BaseRepo<MessageEntity>, IChatRepo
    {
        private readonly ILogger<ChatRepo> _logger;

        public ChatRepo(BrainStormEraContext context, ILogger<ChatRepo> logger) : base(context)
        {
            _logger = logger;
        }
        public async Task<List<MessageEntity>> GetConversationMessagesAsync(string senderId, string receiverId, int page = 1, int pageSize = 50)
        {
            return await _context.MessageEntities
                .Where(m => (m.SenderId == senderId && m.ReceiverId == receiverId) ||
                           (m.SenderId == receiverId && m.ReceiverId == senderId))
                .Where(m => (m.IsDeletedBySender != true) && (m.IsDeletedByReceiver != true))
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.ReplyToMessage)
                .OrderByDescending(m => m.MessageCreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<MessageEntity?> GetMessageByIdAsync(string messageId)
        {
            return await _context.MessageEntities
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.ReplyToMessage)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
        {
            return await _context.Conversations
                .Include(c => c.ConversationParticipants)
                    .ThenInclude(cp => cp.User)
                .Include(c => c.LastMessage)
                    .ThenInclude(lm => lm!.Sender)
                .Where(c => c.ConversationParticipants.Any(cp => cp.UserId == userId && cp.IsActive == true)).Where(c => c.IsActive == true)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        public async Task<Conversation?> GetOrCreateConversationAsync(string userId1, string userId2)
        {
            try
            {
                _logger.LogInformation($"Getting or creating conversation between {userId1} and {userId2}");

                // Try to find existing conversation between these two users
                var existingConversation = await _context.Conversations
                    .Include(c => c.ConversationParticipants)
                    .Where(c => c.ConversationType == "private" && c.IsActive == true)
                    .Where(c => c.ConversationParticipants.Count == 2 &&
                               c.ConversationParticipants.Any(cp => cp.UserId == userId1) &&
                               c.ConversationParticipants.Any(cp => cp.UserId == userId2))
                    .FirstOrDefaultAsync();

                if (existingConversation != null)
                {
                    _logger.LogInformation($"Found existing conversation: {existingConversation.ConversationId}");
                    return existingConversation;
                }

                _logger.LogInformation("Creating new conversation");

                // Create new conversation
                var conversationId = Guid.NewGuid().ToString();
                _logger.LogInformation($"Creating conversation with ID: {conversationId}");

                var conversation = new Conversation
                {
                    ConversationId = conversationId,
                    ConversationType = "private", // Use 'private' to match DB constraint
                    CreatedBy = userId1,
                    IsActive = true,
                    ConversationCreatedAt = DateTime.UtcNow,
                    ConversationUpdatedAt = DateTime.UtcNow
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync(); // Save conversation first
                _logger.LogInformation($"Conversation saved successfully");

                // Add participants after conversation is saved
                var participant1 = new ConversationParticipant
                {
                    ConversationId = conversationId,
                    UserId = userId1,
                    ParticipantRole = "member", // Use 'member' to match DB constraint
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsMuted = false
                };

                var participant2 = new ConversationParticipant
                {
                    ConversationId = conversationId,
                    UserId = userId2,
                    ParticipantRole = "member", // Use 'member' to match DB constraint
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsMuted = false
                };

                _context.ConversationParticipants.Add(participant1);
                _context.ConversationParticipants.Add(participant2);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Participants added successfully");

                return await GetConversationByIdAsync(conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating conversation between {userId1} and {userId2}");
                return null;
            }
        }

        public async Task<bool> MarkMessageAsReadAsync(string messageId, string userId)
        {
            var message = await _context.MessageEntities
                .FirstOrDefaultAsync(m => m.MessageId == messageId && m.ReceiverId == userId);

            if (message == null || message.IsRead == true)
                return false;

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<int> GetUnreadMessageCountAsync(string userId, string senderId)
        {
            return await _context.MessageEntities
                .Where(m => m.ReceiverId == userId && m.SenderId == senderId && m.IsRead != true)
                .CountAsync();
        }

        public async Task<List<MessageEntity>> GetUnreadMessagesAsync(string userId)
        {
            return await _context.MessageEntities
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == userId && m.IsRead != true)
                .OrderBy(m => m.MessageCreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CanUserAccessConversationAsync(string userId, string conversationId)
        {
            return await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId &&
                               cp.UserId == userId &&
                               cp.IsActive == true);
        }
        public async Task<List<Account>> GetChatUsersAsync(string currentUserId)
        {
            try
            {
                // Get current user to determine their role
                var currentUser = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.UserId == currentUserId);

                if (currentUser == null)
                    return new List<Account>();

                var allUsers = new List<Account>();

                if (currentUser.UserRole == "LEARNER")
                {
                    // For learners: Get instructors whose courses they have enrolled in
                    var instructors = await _context.Accounts
                        .Where(a => a.UserId != currentUserId && a.UserRole == "INSTRUCTOR")
                        .Where(a => _context.Enrollments.Any(e =>
                            e.UserId == currentUserId &&
                            e.Course.AuthorId == a.UserId &&
                            e.EnrollmentStatus == 1)) // Only active enrollments
                        .ToListAsync();

                    allUsers.AddRange(instructors);
                }
                else if (currentUser.UserRole == "INSTRUCTOR")
                {
                    // For instructors: Get learners who have enrolled in their courses
                    var learners = await _context.Accounts
                        .Where(a => a.UserId != currentUserId && a.UserRole == "LEARNER")
                        .Where(a => _context.Enrollments.Any(e =>
                            e.UserId == a.UserId &&
                            e.Course.AuthorId == currentUserId &&
                            e.EnrollmentStatus == 1)) // Only active enrollments
                        .ToListAsync();

                    allUsers.AddRange(learners);
                }
                else if (currentUser.UserRole == "ADMIN")
                {
                    // For admins: Get all users (both instructors and learners)
                    var instructors = await _context.Accounts
                        .Where(a => a.UserId != currentUserId && a.UserRole == "INSTRUCTOR")
                        .ToListAsync();

                    var learners = await _context.Accounts
                        .Where(a => a.UserId != currentUserId && a.UserRole == "LEARNER")
                        .ToListAsync();

                    allUsers.AddRange(instructors);
                    allUsers.AddRange(learners);
                }

                // Remove duplicates and order by last login
                return allUsers.Distinct().OrderByDescending(u => u.LastLogin).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting chat users for user {UserId}", currentUserId);
                return new List<Account>();
            }
        }
        public async Task<MessageEntity?> GetLastMessageBetweenUsersAsync(string userId1, string userId2)
        {
            return await _context.MessageEntities
                .Include(m => m.Sender)
                .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                           (m.SenderId == userId2 && m.ReceiverId == userId1))
                .Where(m => (m.IsDeletedBySender != true) && (m.IsDeletedByReceiver != true))
                .OrderByDescending(m => m.MessageCreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateMessageAsync(MessageEntity message)
        {
            _context.MessageEntities.Update(message);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Conversation?> GetConversationByIdAsync(string conversationId)
        {
            return await _context.Conversations
                .Include(c => c.ConversationParticipants)
                    .ThenInclude(cp => cp.User)
                .Include(c => c.LastMessage)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        }

        public async Task<bool> CreateConversationParticipantAsync(ConversationParticipant participant)
        {
            _context.ConversationParticipants.Add(participant);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateConversationAsync(Conversation conversation)
        {
            _context.Conversations.Update(conversation);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
