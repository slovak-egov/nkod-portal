using Microsoft.AspNetCore.Mvc;

namespace WebApi
{
    public class AbstractQuery
    {
        public string? Language { get; set; } = "sk";

        public string? QueryText { get; set; } = string.Empty;

        public int? Page { get; set; }

        public int? PageSize { get; set; } = 10;

        public string? OrderBy { get; set; }

        public Dictionary<string, string[]> Filters { get; set; } = new Dictionary<string, string[]>();

        public List<string>? RequiredFacets { get; set; }
    }
}
