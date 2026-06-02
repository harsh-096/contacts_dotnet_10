using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    /// <summary>Partial-update payload for a group.</summary>
    /// <remarks>
    /// Only the fields that are present in the request body are updated; omitted fields
    /// keep their current database values. The id in the URL is authoritative; do not
    /// send an id in the body. At least one field must be provided.
    /// </remarks>
    [AtLeastOne(nameof(GroupName), nameof(ProjectId),
        ErrorMessage = "At least one field (groupName, projectId) must be provided.")]
    public class GroupUpdateDto
    {
        /// <summary>New group name. Optional on update.</summary>
        [Description("New group name (1-255 characters). Omit to leave unchanged.")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "GroupName must be 1-255 characters.")]
        public string? GroupName { get; set; }

        /// <summary>Move the group to a different project.</summary>
        /// <remarks>
        /// Optional. When supplied must reference an existing project that does not
        /// already own a different group, otherwise 409 Conflict is returned.
        /// </remarks>
        [Description("Move the group to another project. The target project must not already own a different group.")]
        [Range(1, int.MaxValue, ErrorMessage = "ProjectId must be a positive integer.")]
        public int? ProjectId { get; set; }
    }
}
