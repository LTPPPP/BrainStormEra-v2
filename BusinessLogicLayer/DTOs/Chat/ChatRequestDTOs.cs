namespace BusinessLogicLayer.DTOs.Chat
{
    public class SendMessageRequest
    {
        public string ReceiverId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ReplyToMessageId { get; set; }
    }

    public class MarkAsReadRequest
    {
        public string MessageId { get; set; } = string.Empty;
    }

    public class DeleteMessageRequest
    {
        public string MessageId { get; set; } = string.Empty;
    }

    public class EditMessageRequest
    {
        public string MessageId { get; set; } = string.Empty;
        public string NewContent { get; set; } = string.Empty;
    }

    public class GetMessagesRequest
    {
        public string ReceiverId { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetUnreadCountRequest
    {
        public string SenderId { get; set; } = string.Empty;
    }
}
