using Microsoft.AspNetCore.Http.HttpResults;

namespace WebApi
{
    public class PublisherView
    {
        public Guid Id { get; set; }

        public string? Key { get; set; }

        public string? Name { get; set; }

        public int DatasetCount { get; set; }

        public Dictionary<string, int>? Themes { get; set; }
    }
}
