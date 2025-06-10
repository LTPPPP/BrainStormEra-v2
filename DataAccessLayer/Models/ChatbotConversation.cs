using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class ChatbotConversation
{
    public string ConversationId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public DateTime ConversationTime { get; set; }

    public string UserMessage { get; set; } = null!;

    public string BotResponse { get; set; } = null!;

    public string? ConversationContext { get; set; }

    public byte? FeedbackRating { get; set; }

    public virtual Account User { get; set; } = null!;
}
