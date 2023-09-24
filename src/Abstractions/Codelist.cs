using Newtonsoft.Json;

namespace Abstractions
{
    public class Codelist
    {
        public Codelist(string id)
        {
            Id = id;
        }

        public string Id { get; set; }

        public Guid FileId { get; set; }

        public Dictionary<string, string> Labels { get; } = new Dictionary<string, string>();

        public Dictionary<string, CodelistItem> Items { get; } = new Dictionary<string, CodelistItem>();

        public int ItemsCount { get; set; }
    }
}
