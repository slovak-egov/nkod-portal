using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace NkodSk.Abstractions
{
    public class SkosConcept : RdfObject
    {
        public SkosConcept(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public string? Id => Uri.ToString();

        public string? GetLabel(string language) => GetTextFromUriNode("skos:prefLabel", language);

        public void SetLabel(Dictionary<string, string> texts) => SetTexts("skos:prefLabel", texts);

        public bool IsDeprecated => false;

        public static SkosConcept? ParseXml(Stream stream)
        {
            IGraph graph = new Graph();
            SkosConceptScheme.AddDefaultNamespaces(graph);
            using StreamReader reader = new StreamReader(stream);
            RdfXmlParser parser = new RdfXmlParser();
            parser.Load(graph, reader);
            IEnumerable<IUriNode> nodes = RdfDocument.ParseNode(graph, "skos:Concept");
            IUriNode? node = nodes.FirstOrDefault();
            if (node is not null)
            {
                return new SkosConcept(graph, node);
            }
            return null;
        }
    }
}
