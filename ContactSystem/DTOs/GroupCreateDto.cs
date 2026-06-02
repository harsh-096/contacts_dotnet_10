using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    /// <summary>Payload to create a new group.</summary>
    /// <remarks>
    /// A project can have at most one group. Supplying a ProjectId that already
    /// owns a different group returns 409 Conflict.
    /// </remarks>
    public class GroupCreateDto
    {
        /// <summary>Human-readable name of the group.</summary>
        /// <remarks>Must be 1-255 characters. Stored as VARCHAR(255) on the server.</remarks>
        [Description("Group name (1-255 characters).")]
        [Required(ErrorMessage = "GroupName is required.")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "GroupName must be 1-255 characters.")]
        public string GroupName { get; set; } = string.Empty;

        /// <summary>Id of the project this group belongs to.</summary>
        /// <remarks>
        /// REQUIRED. A project can have only one group; supplying a project that
        /// already owns a group returns 409 Conflict.
        /// </remarks>
        [Description("Id of the project this group belongs to. A project can have at most one group.")]
        [Range(1, int.MaxValue, ErrorMessage = "ProjectId is required and must be a positive integer.")]
        public int ProjectId { get; set; }
    }
}
