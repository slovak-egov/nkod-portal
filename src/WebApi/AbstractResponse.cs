using NkodSk.Abstractions;

namespace WebApi
{
    public class AbstractResponse<T>
    {
        public List<T> Items { get; } = new List<T>();

        public List<Facet>? Facets { get; set; }

        public int TotalCount { get; set; }
    }
}
