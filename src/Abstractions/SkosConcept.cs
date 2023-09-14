using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace NkodSk.Abstractions
{
    public class SkosConcept : RdfObject
    {
        public SkosConcept(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public string? Id => Uri.ToString();

        public string? GetLabel(string language) => GetTextFromUriNode("skos:prefLabel", language);

        public bool IsDeprecated => false;
    }
}
