using DataAccessLayer.Models;

namespace BusinessLogicLayer.DTOs.Chat
{
    public class ChatMessageDTO
    {
        public string MessageId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public string? ReplyToMessageId { get; set; }
        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
    }

    public class ChatUserDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? UserImage { get; set; }
        public DateTime? LastActive { get; set; }
        public bool IsOnline { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }

        // Course relationship information
        public string UserRole { get; set; } = string.Empty;
        public List<CourseRelationshipInfo> CourseRelationships { get; set; } = new();
    }

    public class CourseRelationshipInfo
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string RelationshipType { get; set; } = string.Empty; // "Enrolled" or "Teaching"
        public DateTime EnrollmentDate { get; set; }
        public decimal ProgressPercentage { get; set; }
    }

    public class ConversationViewModel
    {
        public string CurrentUserId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public List<ChatUserDTO> ChatUsers { get; set; } = new();
        public List<ChatMessageDTO> Messages { get; set; } = new();
    }

    public class ChatIndexViewModel
    {
        public string CurrentUserId { get; set; } = string.Empty;
        public List<ChatUserDTO> Users { get; set; } = new();
    }
}
