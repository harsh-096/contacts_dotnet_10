using Swashbuckle.AspNetCore.Filters;

namespace ContactSystem.DTOs.Examples
{
    public class CreateProjectExample : IExamplesProvider<ProjectCreateDto>
    {
        public ProjectCreateDto GetExamples() => new()
        {
            ProjectName = "Apollo"
        };
    }

    public class CreateProjectExample_Alternative : IExamplesProvider<ProjectCreateDto>
    {
        public ProjectCreateDto GetExamples() => new()
        {
            ProjectName = "Project Atlas"
        };
    }

    public class CreateProjectExample_Tech : IExamplesProvider<ProjectCreateDto>
    {
        public ProjectCreateDto GetExamples() => new()
        {
            ProjectName = "StockMarketAI"
        };
    }

    public class UpdateProjectExample : IExamplesProvider<ProjectUpdateDto>
    {
        public ProjectUpdateDto GetExamples() => new()
        {
            // Partial update example: only projectName is being changed.
            // All other fields are omitted and will be left untouched in the database.
            ProjectName = "Apollo-Renamed"
        };
    }

    public class UpdateProjectExample_NoChange : IExamplesProvider<ProjectUpdateDto>
    {
        public ProjectUpdateDto GetExamples() => new()
        {
            // Same payload shape; empty / null fields would normally be rejected
            // by the AtLeastOne validator, so at least one field is always provided.
            ProjectName = "Apollo"
        };
    }
}
