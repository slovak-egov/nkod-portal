using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class FulltextResponse
    {
        public List<FulltextResponseDocument> Documents { get; } = new List<FulltextResponseDocument>(10);

        public int TotalCount { get; set; }

        public Dictionary<string, Facet> Facets { get; } = new Dictionary<string, Facet>();
    }
}
