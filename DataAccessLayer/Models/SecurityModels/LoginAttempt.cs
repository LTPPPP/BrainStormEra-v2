namespace DataAccessLayer.Models.SecurityModels
{
    /// <summary>
    /// In-memory model to track login attempts for security monitoring
    /// </summary>
    public class LoginAttempt
    {
        public string Username { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime AttemptTime { get; set; }
        public bool IsSuccessful { get; set; }
        public string? UserAgent { get; set; }
        public string? FailureReason { get; set; }
    }
}
