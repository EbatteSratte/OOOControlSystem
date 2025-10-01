using OOOControlSystem.Models.Enums;

namespace OOOControlSystem.Dtos
{
    public class DefectCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = nameof(DefectPriority.Medium);
        public int ProjectId { get; set; }
        public int? AssignedToId { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
