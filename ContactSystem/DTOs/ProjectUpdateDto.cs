using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    /// <summary>Partial-update payload for a project.</summary>
    /// <remarks>
    /// Only the fields that are present in the request body are updated; omitted fields
    /// keep their current database values. The id in the URL is authoritative; do not
    /// send an id in the body. At least one field must be provided.
    /// </remarks>
    [AtLeastOne(nameof(ProjectName),
        ErrorMessage = "At least one field (projectName) must be provided.")]
    public class ProjectUpdateDto
    {
        /// <summary>New project name. Optional on update.</summary>
        [Description("New project name (1-255 characters). Omit to leave unchanged.")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "ProjectName must be 1-255 characters.")]
        public string? ProjectName { get; set; }
    }
}
