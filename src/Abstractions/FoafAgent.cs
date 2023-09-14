using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace NkodSk.Abstractions
{
    public class FoafAgent : RdfObject
    {
        public FoafAgent(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public string? GetName(string language) => GetTextFromUriNode("foaf:name", language);

        public static FoafAgent? Parse(string text)
        {
            (IGraph graph, IEnumerable<IUriNode> nodes) = Parse(text, "foaf:Agent");
            IUriNode? node = nodes.FirstOrDefault();
            if (node is not null)
            {
                return new FoafAgent(graph, node);
            }
            return null;
        }
    }
}
