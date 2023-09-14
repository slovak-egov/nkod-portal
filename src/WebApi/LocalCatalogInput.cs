using NkodSk.Abstractions;

namespace WebApi
{
    public class LocalCatalogInput
    {
        public string? Id { get; set; }

        public Dictionary<string, string>? Name { get; set; }

        public Dictionary<string, string>? Description { get; set; }

        public string? Endpoint { get; set; }

        public string? ConformsTo { get; set; }

        public DcatCatalog? MapToRdf(out Dictionary<string, string>? errors)
        {
            errors = null;
            return null;
        }
    }
}
