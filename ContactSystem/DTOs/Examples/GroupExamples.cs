using Swashbuckle.AspNetCore.Filters;

namespace ContactSystem.DTOs.Examples
{
    public class CreateGroupExample : IExamplesProvider<GroupCreateDto>
    {
        public GroupCreateDto GetExamples() => new()
        {
            GroupName = "Backend",
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
            ProjectId = 2
        };
    }

    public class UpdateGroupExample : IExamplesProvider<GroupUpdateDto>
    {
        public GroupUpdateDto GetExamples() => new()
        {
            GroupName = "Backend-Renamed"
        };
    }

    public class UpdateGroupExample_MoveContact : IExamplesProvider<GroupUpdateDto>
    {
        public GroupUpdateDto GetExamples() => new()
        {
            ProjectId = 2
        };
    }
}
