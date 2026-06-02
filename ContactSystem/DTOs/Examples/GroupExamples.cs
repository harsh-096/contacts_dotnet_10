using Swashbuckle.AspNetCore.Filters;

namespace ContactSystem.DTOs.Examples
{
    public class CreateGroupExample : IExamplesProvider<GroupCreateDto>
    {
        public GroupCreateDto GetExamples() => new()
        {
            GroupName = "Backend",
            // ProjectId is REQUIRED — a project can have only one group.
            ProjectId = 1
        };
    }

    public class CreateGroupExample_Frontend : IExamplesProvider<GroupCreateDto>
    {
        public GroupCreateDto GetExamples() => new()
        {
            GroupName = "Frontend",
            ProjectId = 1
        };
    }

    public class CreateGroupExample_Mobile : IExamplesProvider<GroupCreateDto>
    {
        public GroupCreateDto GetExamples() => new()
        {
            GroupName = "Mobile",
            ProjectId = 1
        };
    }

    public class UpdateGroupExample : IExamplesProvider<GroupUpdateDto>
    {
        public GroupUpdateDto GetExamples() => new()
        {
            // Partial update example: only groupName is being changed.
            // All other fields are omitted and will be left untouched in the database.
            GroupName = "Backend-Renamed"
        };
    }

    public class UpdateGroupExample_MoveProject : IExamplesProvider<GroupUpdateDto>
    {
        public GroupUpdateDto GetExamples() => new()
        {
            // Move the group to a different project. The target project must
            // exist and must not already own a different group, otherwise
            // the API returns 409 Conflict.
            ProjectId = 2
        };
    }
}
