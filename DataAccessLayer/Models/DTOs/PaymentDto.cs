namespace DataAccessLayer.Models.DTOs
{
    public class PaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? TransactionType { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentGatewayRef { get; set; }
    }

    public class PaymentRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
