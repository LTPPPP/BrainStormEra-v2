using System;
using System.Collections.Generic;

namespace BrainStormEra_MVC.Models;

public partial class ConversationParticipant
{
    public string ConversationId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string? ParticipantRole { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsMuted { get; set; }

    public string? LastReadMessageId { get; set; }

    public DateTime? LastReadAt { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual MessageEntity? LastReadMessage { get; set; }

    public virtual Account User { get; set; } = null!;
}
