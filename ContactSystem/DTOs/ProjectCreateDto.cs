using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    /// <summary>Payload to create a new project.</summary>
    public class ProjectCreateDto
    {
        /// <summary>Human-readable name of the project.</summary>
        /// <remarks>Must be 1-255 characters. Stored as VARCHAR(255) on the server.</remarks>
        [Description("Project name (1-255 characters).")]
        [Required(ErrorMessage = "ProjectName is required.")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "ProjectName must be 1-255 characters.")]
        public string ProjectName { get; set; } = string.Empty;
    }
}
