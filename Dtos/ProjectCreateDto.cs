using System.ComponentModel.DataAnnotations;

namespace OOOControlSystem.Dtos
{
    public class ProjectCreateDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
