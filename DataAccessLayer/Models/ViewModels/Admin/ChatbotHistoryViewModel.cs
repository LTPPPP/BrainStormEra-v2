using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    public class ChatbotHistoryViewModel
    {
        public List<ChatbotConversationItem> Conversations { get; set; } = new List<ChatbotConversationItem>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalConversations { get; set; } = 0;

        // Filter properties
        public string? SearchQuery { get; set; }
        public string? UserIdFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Statistics
        public int TotalUsers { get; set; } = 0;
        public double AverageRating { get; set; } = 0;
        public int TotalRatings { get; set; } = 0;
        public Dictionary<int, int> RatingDistribution { get; set; } = new Dictionary<int, int>();
    }

    public class ChatbotConversationItem
    {
        public string ConversationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserImage { get; set; } = "/SharedMedia/defaults/default-avatar.svg";
        public DateTime ConversationTime { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string BotResponse { get; set; } = string.Empty;
        public string? ConversationContext { get; set; }
        public int? FeedbackRating { get; set; }

        // Formatted properties for display
        public string FormattedTime => ConversationTime.ToString("dd/MM/yyyy HH:mm:ss");
        public string ShortUserMessage => UserMessage.Length > 100 ? UserMessage.Substring(0, 100) + "..." : UserMessage;
        public string ShortBotResponse => BotResponse.Length > 100 ? BotResponse.Substring(0, 100) + "..." : BotResponse;
        public string RatingDisplay => FeedbackRating?.ToString() ?? "No rating";
        public string RatingClass => FeedbackRating switch
        {
            1 => "text-danger",
            2 => "text-warning",
            3 => "text-info",
            4 => "text-primary",
            5 => "text-success",
            _ => "text-muted"
        };
    }
}