using System.ComponentModel.DataAnnotations;

namespace BrainStormEra_MVC.Models
{
    /// <summary>
    /// Entity to track delete operations and maintain audit trail
    /// </summary>
    public partial class DeleteAuditLog
    {
        [Key]
        public string LogId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        public string EntityId { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Operation { get; set; } = string.Empty; // SoftDelete, HardDelete, Restore

        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? EntitySnapshot { get; set; } // JSON snapshot of entity before operation

        public string? AffectedRelatedEntities { get; set; } // JSON list of affected entities

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        // Navigation properties
        public virtual Account User { get; set; } = null!;
    }

    /// <summary>
    /// Configuration for entity deletion policies
    /// </summary>
    public class EntityDeletionPolicy
    {
        public string EntityType { get; set; } = string.Empty;
        public bool AllowSoftDelete { get; set; } = true;
        public bool AllowHardDelete { get; set; } = false;
        public bool RequiresAdminApproval { get; set; } = false;
        public int RetentionDays { get; set; } = 30; // Days to keep soft-deleted items
        public List<string> BlockingRelationships { get; set; } = new List<string>();
        public List<string> CascadeDeleteRelationships { get; set; } = new List<string>();
    }
}
