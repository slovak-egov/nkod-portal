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
            graph.NamespaceMap.AddNamespace("custom", RdfDocument.CustomPrefix);
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

        private const int MaxShortIdLength = 40;

        private IEnumerable<SkosConcept> RetiredConcepts => GetUrisFromUriNode("custom:retiredConcept").Select(u => new SkosConcept(Graph, Graph.CreateUriNode(u))).ToList();

        // Short ids of current and retired concepts by concept IRI
        public Dictionary<string, string> GetShortIds()
        {
            Dictionary<string, string> shortIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (SkosConcept concept in Concepts.Concat(RetiredConcepts))
            {
                string? shortId = concept.ShortId;
                if (concept.Id is not null && IsValidShortId(shortId))
                {
                    shortIds.TryAdd(concept.Id, shortId!);
                }
            }
            return shortIds;
        }

        // Ensures that every concept has a unique short id. Ids from the previous version of the codelist are always preserved
        // and ids of concepts which are no longer present are kept as retired, so they are never reused for another concept.
        public void AssignShortIds(SkosConceptScheme? previous)
        {
            Dictionary<string, string> reserved = previous?.GetShortIds() ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (SkosConcept concept in RetiredConcepts)
            {
                string? shortId = concept.ShortId;
                if (concept.Id is not null && IsValidShortId(shortId))
                {
                    reserved.TryAdd(concept.Id, shortId!);
                }
            }

            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<SkosConcept> sortedConcepts = Concepts.Where(c => c.Id is not null).OrderBy(c => c.Id, StringComparer.Ordinal).ToList();

            List<SkosConcept> withoutReservedId = new List<SkosConcept>();
            foreach (SkosConcept concept in sortedConcepts)
            {
                if (reserved.TryGetValue(concept.Id!, out string? shortId) && used.Add(shortId))
                {
                    concept.ShortId = shortId;
                }
                else
                {
                    withoutReservedId.Add(concept);
                }
            }

            foreach (KeyValuePair<string, string> pair in reserved)
            {
                if (!concepts.ContainsKey(pair.Key))
                {
                    used.Add(pair.Value);
                }
            }

            List<SkosConcept> withoutId = new List<SkosConcept>();
            foreach (SkosConcept concept in withoutReservedId)
            {
                string? shortId = concept.ShortId;
                if (!IsValidShortId(shortId) || !used.Add(shortId!))
                {
                    withoutId.Add(concept);
                }
            }

            foreach (SkosConcept concept in withoutId)
            {
                string baseId = CreateShortIdBase(concept.Id!);
                string shortId = baseId;
                for (int i = 2; !used.Add(shortId); i++)
                {
                    shortId = $"{baseId}-{i}";
                }
                concept.ShortId = shortId;
            }

            List<Uri> retired = new List<Uri>();
            foreach (KeyValuePair<string, string> pair in reserved.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (!concepts.ContainsKey(pair.Key) && Uri.TryCreate(pair.Key, UriKind.Absolute, out Uri? uri))
                {
                    new SkosConcept(Graph, Graph.CreateUriNode(uri)).ShortId = pair.Value;
                    retired.Add(uri);
                }
            }
            SetUriNodes("custom:retiredConcept", retired);
        }

        public static bool IsValidShortId(string? shortId)
        {
            return !string.IsNullOrEmpty(shortId) && shortId.Length <= MaxShortIdLength + 8 && shortId.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
        }

        private static string CreateShortIdBase(string id)
        {
            string text = id.TrimEnd('/', '#');
            int index = text.LastIndexOfAny(new[] { '/', '#', ':' });
            if (index >= 0)
            {
                text = text.Substring(index + 1);
            }
            text = Uri.UnescapeDataString(text).Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (char.IsAsciiLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark && sb.Length > 0 && sb[^1] != '-')
                {
                    sb.Append('-');
                }
            }

            string shortId = sb.ToString().Trim('-');
            if (shortId.Length > MaxShortIdLength)
            {
                shortId = shortId.Substring(0, MaxShortIdLength).Trim('-');
            }
            return shortId.Length > 0 ? shortId : "item";
        }

        public static SkosConceptScheme Create(Uri uri)
        {
            IGraph graph = new Graph();
            RdfDocument.AddDefaultNamespaces(graph);
            AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("skos:ConceptScheme");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            return new SkosConceptScheme(graph, subject);
        }
    }
}
