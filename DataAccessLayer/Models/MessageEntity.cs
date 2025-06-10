using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class MessageEntity
{
    public string MessageId { get; set; } = null!;

    public string SenderId { get; set; } = null!;

    public string ReceiverId { get; set; } = null!;

    public string ConversationId { get; set; } = null!;

    public string MessageContent { get; set; } = null!;

    public string? MessageType { get; set; }

    public string? AttachmentUrl { get; set; }

    public string? AttachmentName { get; set; }

    public long? AttachmentSize { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public bool? IsDeletedBySender { get; set; }

    public bool? IsDeletedByReceiver { get; set; }

    public string? ReplyToMessageId { get; set; }

    public bool? IsEdited { get; set; }

    public DateTime? EditedAt { get; set; }

    public string? OriginalContent { get; set; }

    public DateTime MessageCreatedAt { get; set; }

    public DateTime MessageUpdatedAt { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public virtual ICollection<MessageEntity> InverseReplyToMessage { get; set; } = new List<MessageEntity>();

    public virtual Account Receiver { get; set; } = null!;

    public virtual MessageEntity? ReplyToMessage { get; set; }

    public virtual Account Sender { get; set; } = null!;
}
