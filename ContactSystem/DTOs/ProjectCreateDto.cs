using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    public class ProjectCreateDto
    {
        [Required(ErrorMessage = "ProjectName is required.")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "ProjectName must be 1-255 characters.")]
        public string ProjectName { get; set; } = string.Empty;
    }
}
