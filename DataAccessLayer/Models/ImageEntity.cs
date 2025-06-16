using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    [Table("Images")]
    public class ImageEntity
    {
        [Key]
        public string ImageId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string EntityId { get; set; } = string.Empty;

        [Required]
        public int ImageType { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Alt { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public long FileSize { get; set; }

        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(255)]
        public string? ThumbnailPath { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        [StringLength(500)]
        public string? OriginalPath { get; set; }

        // Navigation properties (optional, for future use)
        public virtual Course? Course { get; set; }
        public virtual Account? User { get; set; }
    }
}