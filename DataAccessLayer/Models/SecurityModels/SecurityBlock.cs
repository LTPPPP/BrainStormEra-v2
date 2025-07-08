namespace DataAccessLayer.Models.SecurityModels
{
    /// <summary>
    /// In-memory model to track blocked IPs and users for security
    /// </summary>
    public class SecurityBlock
    {
        public string? Username { get; set; }
        public string? IpAddress { get; set; }
        public DateTime BlockedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string BlockType { get; set; } = string.Empty; // "IP", "User", "UserAndIP"
        public string? Reason { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
