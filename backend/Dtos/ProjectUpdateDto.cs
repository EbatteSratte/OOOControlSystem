using System.ComponentModel.DataAnnotations;

namespace OOOControlSystem.Dtos
{
    public class ProjectUpdateDto
    {
        [MaxLength(255)]
        public string? Name { get; set; }

        public string? Description { get; set; }
        public string? Status { get; set; }
    }
}
