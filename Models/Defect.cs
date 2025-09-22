using OOOControlSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OOOControlSystem.Models
{
    public class Defect
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public DefectStatus Status { get; set; }

        [Required]
        [MaxLength(50)]
        public DefectPriority Priority { get; set; }

        [Required]
        [ForeignKey("Project")]
        public int ProjectId { get; set; }

        [ForeignKey("AssignedUser")]
        public int? AssignedToId { get; set; }

        [Required]
        [ForeignKey("Reporter")]
        public int ReportedById { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<string> AttachmentPaths { get; set; } = new List<string>();
        public List<DefectHistoryEntry> History { get; set; } = new List<DefectHistoryEntry>();

        public Project Project { get; set; } = null!;
        public User? AssignedUser { get; set; }
        public User Reporter { get; set; } = null!;
        public class DefectHistoryEntry
        {
            public DefectStatus Status { get; set; }
            public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
            public int ChangedById { get; set; }
            public string Description { get; set; } = string.Empty;
        }
    }
}
