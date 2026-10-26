using Newtonsoft.Json;
using NkodSk.Abstractions;

namespace Abstractions
{
    public class Codelist
    {
        private Dictionary<string, CodelistItem>? itemsByShortId;

        public Codelist(string id)
        {
            Id = id;
        }

        public string Id { get; set; }

        public Guid FileId { get; set; }

        public Dictionary<string, string> Labels { get; } = new Dictionary<string, string>();

        public Dictionary<string, CodelistItem> Items { get; } = new Dictionary<string, CodelistItem>(StringComparer.OrdinalIgnoreCase);

        public int ItemsCount { get; set; }

        // Finds item by its IRI or by its short id, the index of short ids is created on first use, so items must not be changed afterwards
        public CodelistItem? GetItem(string idOrShortId)
        {
            if (Items.TryGetValue(idOrShortId, out CodelistItem? item))
            {
                return item;
            }

            if (!SkosConceptScheme.IsValidShortId(idOrShortId))
            {
                return null;
            }

            Dictionary<string, CodelistItem>? index = Volatile.Read(ref itemsByShortId);
            if (index is null)
            {
                index = new Dictionary<string, CodelistItem>(StringComparer.OrdinalIgnoreCase);
                foreach (CodelistItem i in Items.Values)
                {
                    if (!string.IsNullOrEmpty(i.ShortId))
                    {
                        index.TryAdd(i.ShortId, i);
                    }
                }
                Volatile.Write(ref itemsByShortId, index);
            }

            return index.GetValueOrDefault(idOrShortId);
        }
    }
}
