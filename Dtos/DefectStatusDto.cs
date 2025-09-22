using OOOControlSystem.Models.Enums;

namespace OOOControlSystem.Dtos
{
    public class DefectStatusDto
    {
        public string Status { get; set; } = nameof(DefectStatus.InProgress);
        public string StatusDescription { get; set; } = String.Empty;
    }
}
