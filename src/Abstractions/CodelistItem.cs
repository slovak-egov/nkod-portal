namespace Abstractions
{
    public class CodelistItem
    {
        public CodelistItem(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public Dictionary<string, string> Labels { get; } = new Dictionary<string, string>();

        public bool IsDeprecated { get; set; }
    }
}
