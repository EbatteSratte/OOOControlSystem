using OOOControlSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OOOControlSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public int TokenVersion { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public List<Project> CreatedProjects { get; set; } = new();
        [JsonIgnore]
        public List<Project> OwnedProjects { get; set; } = new();
        [JsonIgnore]
        public List<Defect> ReportedDefects { get; set; } = new();
        [JsonIgnore]
        public List<Defect> AssignedDefects { get; set; } = new();
    }
}
