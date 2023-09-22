using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;
using static System.Net.Mime.MediaTypeNames;

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

        public void SetLabel(Dictionary<string, string> texts) => SetTexts("ns1:prefLabel", texts);

        public IEnumerable<SkosConcept> Concepts => concepts.Values;

        public SkosConcept? GetConcept(string code)
        {
            if (concepts.TryGetValue(code, out SkosConcept? concept))
            {
                return concept;
            }
            return null;
        }

        public SkosConcept CreateConcept(Uri id)
        {
            IUriNode subject = Graph.CreateUriNode(id);
            Graph.Assert(subject, Graph.GetUriNode(new Uri(RdfSpecsHelper.RdfType)), Graph.CreateUriNode("skos:Concept"));
            SkosConcept concept = new SkosConcept(Graph, subject);
            concepts[id.ToString()] = concept;
            return concept;
        }

        public static void AddDefaultNamespaces(IGraph graph)
        {
            graph.NamespaceMap.AddNamespace("skos", SkosUri);
            graph.NamespaceMap.AddNamespace("ns0", OntologyAuthorityTable);
            graph.NamespaceMap.AddNamespace("ns1", OntologyAuthority);
        }

        public static SkosConceptScheme? Parse(string text)
        {
            IGraph graph = new Graph();
            AddDefaultNamespaces(graph);
            IEnumerable<IUriNode> nodes = RdfDocument.ParseNode(graph, text, "skos:ConceptScheme");
            IUriNode? node = nodes.FirstOrDefault();
            if (node is not null)
            {
                return new SkosConceptScheme(graph, node);
            }
            return null;
        }

        public static SkosConceptScheme? Parse(Stream stream)
        {
            IGraph graph = new Graph();
            AddDefaultNamespaces(graph);
            IEnumerable<IUriNode> nodes = RdfDocument.ParseNode(graph, stream, "skos:ConceptScheme");
            IUriNode? node = nodes.FirstOrDefault();
            if (node is not null)
            {
                return new SkosConceptScheme(graph, node);
            }
            return null;
        }

        public override IEnumerable<RdfObject> RootObjects => base.RootObjects.Union(concepts.Values);

        public static SkosConceptScheme Create(Uri uri)
        {
            IGraph graph = new Graph();
            AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("skos:ConceptScheme");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            return new SkosConceptScheme(graph, subject);
        }
    }
}
