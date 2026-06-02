using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    public class GroupCreateDto
    {
        [Required(ErrorMessage = "GroupName is required.")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "GroupName must be 1-255 characters.")]
        public string GroupName { get; set; } = string.Empty;

        // ProjectId is optional. Omit it (or send null) to create a project-less group.
        // If a value is supplied, it must be a positive integer referencing an existing project.
        [Range(1, int.MaxValue, ErrorMessage = "ProjectId must be a positive integer.")]
        public int? ProjectId { get; set; }
    }
}
