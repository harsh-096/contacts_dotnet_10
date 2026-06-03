using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    public class GroupCreateDto
    {
        [Description("Group name (1-255 characters).")]
        [Required(ErrorMessage = "GroupName is required.")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "GroupName must be 1-255 characters.")]
        public string GroupName { get; set; } = string.Empty;

        [Description("Id of the project this group belongs to. A project can have many groups.")]
        [Range(1, int.MaxValue, ErrorMessage = "ProjectId is required and must be a positive integer.")]
        public int ProjectId { get; set; }
    }
}
