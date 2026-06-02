using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    [AtLeastOne(nameof(ProjectName),
        ErrorMessage = "At least one field (projectName) must be provided.")]
    public class ProjectUpdateDto
    {
        [StringLength(255, MinimumLength = 1, ErrorMessage = "ProjectName must be 1-255 characters.")]
        public string? ProjectName { get; set; }
    }
}
