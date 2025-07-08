namespace BusinessLogicLayer.DTOs.Security
{
    /// <summary>
    /// Result object for security validation checks
    /// </summary>
    public class SecurityCheckResult
    {
        public bool IsAllowed { get; set; } = true;
        public bool IsBlocked { get; set; } = false;
        public string? BlockReason { get; set; }
        public DateTime? BlockExpiresAt { get; set; }
        public int RemainingAttempts { get; set; } = 0;
        public TimeSpan? NextAttemptDelay { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Configuration for rate limiting and brute force protection
    /// </summary>
    public class SecurityConfig
    {
        public int MaxAttemptsPerMinute { get; set; } = 5;
        public int MaxAttemptsPerHour { get; set; } = 10;
        public int MaxAttemptsPerDay { get; set; } = 50;
        public int BlockDurationMinutes { get; set; } = 15;
        public int ExtendedBlockDurationHours { get; set; } = 24;
        public int MaxFailuresBeforeExtendedBlock { get; set; } = 20;
        public bool EnableIpBlocking { get; set; } = true;
        public bool EnableUserBlocking { get; set; } = true;
    }

    /// <summary>
    /// Request object for logging login attempts
    /// </summary>
    public class LoginAttemptRequest
    {
        public string Username { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public bool IsSuccessful { get; set; }
        public string? FailureReason { get; set; }
    }
}
