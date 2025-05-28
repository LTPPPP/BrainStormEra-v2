using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BrainStormEra_MVC.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    public class ChatController : Controller
    {
        private readonly BrainStormEraContext _context;

        public ChatController(BrainStormEraContext context)
        {
            _context = context;
        }

        // GET: Chat
        public async Task<IActionResult> Index()
        {
            // For demo purposes, assuming user is logged in and has ID (should get from session/auth)
            string currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account"); // Redirect to login if not logged in
            }

            // Get all conversations where the current user is a participant
            var conversationsIds = await _context.ConversationParticipants
                .Where(cp => cp.UserId == currentUserId && cp.IsActive == true)
                .Select(cp => cp.ConversationId)
                .ToListAsync();

            var conversations = await _context.Conversations
                .Where(c => conversationsIds.Contains(c.ConversationId))
                .Include(c => c.LastMessage)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            // Get other users in each conversation for display
            var conversationViewModels = new List<ConversationViewModel>();
            foreach (var conversation in conversations)
            {
                var participants = await _context.ConversationParticipants
                    .Where(cp => cp.ConversationId == conversation.ConversationId && cp.UserId != currentUserId)
                    .Include(cp => cp.User)
                    .ToListAsync();

                var unreadCount = await _context.MessageEntities
                    .CountAsync(m => m.ConversationId == conversation.ConversationId &&
                                    m.ReceiverId == currentUserId &&
                                    m.IsRead != true);

                conversationViewModels.Add(new ConversationViewModel
                {
                    Conversation = conversation,
                    OtherParticipants = participants.Select(p => p.User).ToList(),
                    UnreadMessageCount = unreadCount
                });
            }

            ViewBag.CurrentUserId = currentUserId;
            return View(conversationViewModels);
        }        // GET: Chat/Conversation/5
        public async Task<IActionResult> Conversation(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Response.StatusCode = 404;
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            string currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user is a participant in this conversation
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(cp => cp.ConversationId == id && cp.UserId == currentUserId);

            if (participant == null)
            {
                Response.StatusCode = 404;
                ViewData["ErrorMessage"] = "You are not a participant in this conversation.";
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            // Get conversation details
            var conversation = await _context.Conversations
                .Include(c => c.ConversationParticipants)
                .ThenInclude(cp => cp.User)
                .FirstOrDefaultAsync(c => c.ConversationId == id);

            if (conversation == null)
            {
                Response.StatusCode = 404;
                ViewData["ErrorMessage"] = "Conversation not found.";
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            // Get messages for this conversation
            var messages = await _context.MessageEntities
                .Where(m => m.ConversationId == id)
                .OrderBy(m => m.MessageCreatedAt)
                .ToListAsync();

            // Get other participants
            var otherParticipants = conversation.ConversationParticipants
                .Where(cp => cp.UserId != currentUserId)
                .Select(cp => cp.User)
                .ToList();

            var viewModel = new ChatViewModel
            {
                Conversation = conversation,
                Messages = messages,
                OtherParticipants = otherParticipants,
                CurrentUserId = currentUserId
            };

            // Mark unread messages as read
            var unreadMessages = messages
                .Where(m => m.ReceiverId == currentUserId && m.IsRead != true)
                .ToList();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                _context.Update(message);
            }

            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
            }

            return View(viewModel);
        }

        // GET: Chat/Users
        public async Task<IActionResult> Users()
        {
            string currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var users = await _context.Accounts
                .Where(u => u.UserId != currentUserId && u.IsBanned != true)
                .ToListAsync();

            return View(users);
        }

        // POST: Chat/CreateConversation
        [HttpPost]
        public async Task<IActionResult> CreateConversation(string userId)
        {
            string currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            // Check if conversation already exists between these two users
            var existingConversation = await _context.ConversationParticipants
                .Where(cp => cp.UserId == currentUserId)
                .Join(_context.ConversationParticipants.Where(cp => cp.UserId == userId),
                    cp1 => cp1.ConversationId,
                    cp2 => cp2.ConversationId,
                    (cp1, cp2) => cp1.ConversationId)
                .FirstOrDefaultAsync();

            // If conversation exists, redirect to it
            if (!string.IsNullOrEmpty(existingConversation))
            {
                return RedirectToAction("Conversation", new { id = existingConversation });
            }

            // Create new conversation
            var conversationId = Guid.NewGuid().ToString();
            var otherUser = await _context.Accounts.FindAsync(userId);

            var conversation = new Conversation
            {
                ConversationId = conversationId,
                ConversationType = "PRIVATE",
                CreatedBy = currentUserId,
                IsActive = true,
                ConversationCreatedAt = DateTime.UtcNow,
                ConversationUpdatedAt = DateTime.UtcNow,
                ConversationName = otherUser?.FullName ?? "New Conversation"
            };

            // Add participants
            var currentUserParticipant = new ConversationParticipant
            {
                ConversationId = conversationId,
                UserId = currentUserId,
                ParticipantRole = "MEMBER",
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            var otherUserParticipant = new ConversationParticipant
            {
                ConversationId = conversationId,
                UserId = userId,
                ParticipantRole = "MEMBER",
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Conversations.Add(conversation);
            _context.ConversationParticipants.Add(currentUserParticipant);
            _context.ConversationParticipants.Add(otherUserParticipant);

            await _context.SaveChangesAsync();

            return RedirectToAction("Conversation", new { id = conversationId });
        }        // GET: Chat/GetUserConversations
        [HttpGet]
        public async Task<IActionResult> GetUserConversations(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            var conversationsIds = await _context.ConversationParticipants
                .Where(cp => cp.UserId == userId && cp.IsActive == true)
                .Select(cp => new { conversationId = cp.ConversationId })
                .ToListAsync();

            return Json(conversationsIds);
        }

        // GET: Chat/GetRecentConversations
        [HttpGet]
        public async Task<IActionResult> GetRecentConversations(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            // Get all conversations where the current user is a participant
            var conversationsIds = await _context.ConversationParticipants
                .Where(cp => cp.UserId == userId && cp.IsActive == true)
                .Select(cp => cp.ConversationId)
                .ToListAsync();

            var conversations = await _context.Conversations
                .Where(c => conversationsIds.Contains(c.ConversationId))
                .Include(c => c.LastMessage)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            // Get other users in each conversation for display
            var conversationViewModels = new List<object>();
            foreach (var conversation in conversations)
            {
                var participants = await _context.ConversationParticipants
                    .Where(cp => cp.ConversationId == conversation.ConversationId && cp.UserId != userId)
                    .Include(cp => cp.User)
                    .ToListAsync();

                var otherParticipant = participants.FirstOrDefault()?.User;
                if (otherParticipant == null) continue;

                var lastMessage = await _context.MessageEntities
                    .Where(m => m.ConversationId == conversation.ConversationId)
                    .OrderByDescending(m => m.MessageCreatedAt)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.MessageEntities
                    .CountAsync(m => m.ConversationId == conversation.ConversationId &&
                                    m.ReceiverId == userId &&
                                    m.IsRead != true);

                conversationViewModels.Add(new
                {
                    conversationId = conversation.ConversationId,
                    otherParticipant = new
                    {
                        userId = otherParticipant.UserId,
                        fullName = otherParticipant.FullName ?? "Unknown User"
                    },
                    lastMessage = lastMessage != null ? new
                    {
                        messageId = lastMessage.MessageId,
                        messageContent = lastMessage.MessageContent,
                        messageCreatedAt = lastMessage.MessageCreatedAt,
                        isRead = lastMessage.IsRead,
                        senderId = lastMessage.SenderId
                    } : null,
                    unreadMessageCount = unreadCount
                });
            }

            return Json(conversationViewModels);
        }        // Helper method to get current user ID (replace with your auth mechanism)
        private string GetCurrentUserId()
        {
            // For demo purposes, we'll use a hard-coded user ID
            // In a real application, you should get this from your authentication system
            // Example: return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check if we have user ID in the session
            byte[]? userIdBytes = null;
            if (HttpContext.Session.TryGetValue("UserId", out userIdBytes) && userIdBytes != null)
            {
                return System.Text.Encoding.UTF8.GetString(userIdBytes);
            }

            // For demo, return a fixed ID if no auth
            // TODO: Replace with actual authentication
            return "user123"; // Replace with actual user authentication
        }
    }

    // View Models
    public class ConversationViewModel
    {
        public Conversation Conversation { get; set; } = null!;
        public IEnumerable<Account> OtherParticipants { get; set; } = new List<Account>();
        public int UnreadMessageCount { get; set; }
    }

    public class ChatViewModel
    {
        public Conversation Conversation { get; set; } = null!;
        public IEnumerable<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
        public IEnumerable<Account> OtherParticipants { get; set; } = new List<Account>();
        public string CurrentUserId { get; set; } = string.Empty;
    }
}
