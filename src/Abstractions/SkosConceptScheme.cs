using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace NkodSk.Abstractions
{
    public class SkosConceptScheme : RdfObject
    {
        public static readonly Uri SkosUri = new Uri("http://www.w3.org/2004/02/skos/core#");

        public static readonly Uri OntologyAuthorityTable = new Uri("http://publications.europa.eu/ontology/authority/table.");

        public static readonly Uri OntologyAuthority = new Uri("http://publications.europa.eu/ontology/authority/");

        private readonly Dictionary<string, SkosConcept> concepts = new Dictionary<string, SkosConcept>(StringComparer.OrdinalIgnoreCase);

        public SkosConceptScheme(IGraph graph, IUriNode node) : base(graph, node)
        {
            IUriNode rdfTypeNode = graph.GetUriNode(new Uri(RdfSpecsHelper.RdfType));
            if (rdfTypeNode is not null)
            {
                IUriNode? typeNode = GetUriNode("skos:Concept");
                if (typeNode is not null)
                {
                    foreach (IUriNode conceptNode in Graph.GetTriplesWithPredicateObject(rdfTypeNode, typeNode).Select(n => n.Subject).OfType<IUriNode>())
                    {
                        SkosConcept concept = new SkosConcept(Graph, conceptNode);
                        string? code = concept.Id;
                        if (!string.IsNullOrEmpty(code))
                        {
                            concepts[code] = concept;
                        }
                    }
                }
            }
        }

        public string Id => Uri.ToString();

        public string? GetLabel(string language) => GetTextFromUriNode("ns1:prefLabel", language);

        public IEnumerable<SkosConcept> Concepts => concepts.Values;

        public SkosConcept? GetConcept(string code)
        {
            if (concepts.TryGetValue(code, out SkosConcept? concept))
            {
                return concept;
            }
            return null;
        }

        public static SkosConceptScheme? Parse(string text)
        {
            IGraph graph = new Graph();

            graph.NamespaceMap.AddNamespace("skos", SkosUri);
            graph.NamespaceMap.AddNamespace("ns0", OntologyAuthorityTable);
            graph.NamespaceMap.AddNamespace("ns1", OntologyAuthority);

            IEnumerable<IUriNode> nodes = RdfDocument.ParseNode(graph, text, "skos:ConceptScheme");
            IUriNode? node = nodes.FirstOrDefault();
            if (node is not null)
            {
                return new SkosConceptScheme(graph, node);
            }
            return null;
        }
    }
}
