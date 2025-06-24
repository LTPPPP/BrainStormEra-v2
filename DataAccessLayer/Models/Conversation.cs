using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class Conversation
{
    public string ConversationId { get; set; } = null!;

    public string? ConversationType { get; set; }

    public string CreatedBy { get; set; } = null!;

    public bool? IsActive { get; set; }

    public string? LastMessageId { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public DateTime ConversationCreatedAt { get; set; }

    public DateTime ConversationUpdatedAt { get; set; }

    public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();

    public virtual Account CreatedByNavigation { get; set; } = null!;

    public virtual MessageEntity? LastMessage { get; set; }

    public virtual ICollection<MessageEntity> MessageEntities { get; set; } = new List<MessageEntity>();
}
