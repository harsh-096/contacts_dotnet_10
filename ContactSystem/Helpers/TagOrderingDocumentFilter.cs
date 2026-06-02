using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ContactSystem.Helpers
{
    public class TagOrderingDocumentFilter : IDocumentFilter
    {
        private readonly string[] _tagOrder;

        public TagOrderingDocumentFilter(string[] tagOrder)
        {
            _tagOrder = tagOrder;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var ordered = new List<OpenApiTag>();

            // 1) Emit the tags in the caller-specified order first.
            foreach (var tagName in _tagOrder)
            {
                var tag = swaggerDoc.Tags.FirstOrDefault(t =>
                    string.Equals(t.Name, tagName, StringComparison.OrdinalIgnoreCase));
                if (tag != null) ordered.Add(tag);
            }

            // 2) Append any remaining tags that were not in the explicit list
            //    (e.g. new controllers added later) so nothing is lost.
            foreach (var tag in swaggerDoc.Tags)
            {
                if (!ordered.Any(t => string.Equals(t.Name, tag.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ordered.Add(tag);
                }
            }

            swaggerDoc.Tags = ordered;
        }
    }
}
