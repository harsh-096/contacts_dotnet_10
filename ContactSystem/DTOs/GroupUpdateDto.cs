using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    [AtLeastOne(nameof(GroupName), nameof(ProjectId),
        ErrorMessage = "At least one field (groupName, projectId) must be provided.")]
    public class GroupUpdateDto
    {
        [Description("New group name (1-255 characters). Omit to leave unchanged.")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "GroupName must be 1-255 characters.")]
        public string? GroupName { get; set; }

        [Description("Move the group to another project. The target project must already exist.")]
        [Range(1, int.MaxValue, ErrorMessage = "ProjectId must be a positive integer.")]
        public int? ProjectId { get; set; }
    }
}
