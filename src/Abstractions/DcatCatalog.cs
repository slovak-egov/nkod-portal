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
    public class DcatCatalog : RdfObject
    {
        public const string LocalCatalogTypeCodelist = "https://data.gov.sk/def/local-catalog-type";

        public DcatCatalog(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public string? GetTitle(string language) => GetTextFromUriNode("dct:title", language);

        public Dictionary<string, string> Title => GetTextsFromUriNode("dct:title");

        public void SetTitle(Dictionary<string, string> values)
        {
            SetTexts("dct:title", values);
        }

        public string? GetDescription(string language) => GetTextFromUriNode("dct:description", language);

        public Dictionary<string, string> Description => GetTextsFromUriNode("dct:description");

        public void SetDescription(Dictionary<string, string> values)
        {
            SetTexts("dct:description", values);
        }

        public Uri? Publisher
        {
            get => GetUriFromUriNode("dct:publisher");
            set => SetUriNode("dct:publisher", value);
        }

        public VcardKind? ContactPoint
        {
            get
            {
                IUriNode nodeType = Graph.GetUriNode("dcat:contactPoint");
                if (nodeType is not null)
                {
                    IUriNode? contactPointNode = Graph.GetTriplesWithSubjectPredicate(Node, nodeType).Select(x => x.Object).OfType<IUriNode>().FirstOrDefault();
                    if (contactPointNode is not null)
                    {
                        return new VcardKind(Graph, contactPointNode);
                    }
                }
                return null;
            }
        }

        public void SetContactPoint(LanguageDependedTexts? name, string? email)
        {
            RemoveUriNodes("dcat:contactPoint");
            if (name is not null || email is not null)
            {
                VcardKind contactPoint = new VcardKind(Graph, CreateSubject("dcat:contactPoint", "vcard:Individual"));
                contactPoint.SetNames(name);
                contactPoint.Email = !string.IsNullOrEmpty(email) ? email : null;
            }
        }

        public Uri? HomePage
        {
            get => GetUriFromUriNode("foaf:homepage");
            set => SetUriNode("foaf:homepage", value);
        }

        public bool ShouldBePublic
        {
            get => GetBooleanFromUriNode("custom:shouldBePublic") ?? true;
            set => SetBooleanToUriNode("custom:shouldBePublic", value);
        }

        public Uri? Type
        {
            get
            {
                IUriNode rdfTypeNode = Graph.GetUriNode(new Uri(RdfSpecsHelper.RdfType));
                if (rdfTypeNode is not null)
                {
                    return Graph.GetTriplesWithSubjectPredicate(Node, rdfTypeNode).Select(n => n.Object).OfType<IUriNode>().Select(n => n.Uri).Where(u => u.ToString().StartsWith(LocalCatalogTypeCodelist, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                }
                return null;
            }
            set
            {
                IUriNode rdfTypeNode = Graph.GetUriNode(new Uri(RdfSpecsHelper.RdfType));
                if (rdfTypeNode is not null)
                {
                    foreach (Triple t in Graph.GetTriplesWithSubjectPredicate(Node, rdfTypeNode))
                    {
                        if (t.Object is IUriNode node && node.Uri.ToString().StartsWith(LocalCatalogTypeCodelist, StringComparison.OrdinalIgnoreCase))
                        {
                            Graph.Retract(t);
                        }
                    }
                    Graph.Assert(Node, rdfTypeNode, Graph.CreateUriNode(value));
                }
            }
        }

        public Uri? EndpointUrl
        {
            get => GetUriFromUriNode("dcat:endpointURL");
            set => SetUriNode("dcat:endpointURL", value);
        }

        public static DcatCatalog? Parse(string text)
        {
            (IGraph graph, IEnumerable<IUriNode> nodes) = Parse(text, "dcat:Catalog");
            IUriNode? node = nodes.FirstOrDefault();
            if (node is not null)
            {
                return new DcatCatalog(graph, node);
            }
            return null;
        }

        public static DcatCatalog Create(Uri uri)
        {
            IGraph graph = new Graph();
            RdfDocument.AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("dcat:Catalog");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            return new DcatCatalog(graph, subject);
        }

        public FileMetadata UpdateMetadata(FileMetadata? metadata = null)
        {
            Guid id = metadata?.Id ?? Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Dictionary<string, string[]> values = new Dictionary<string, string[]>();

            values[LocalCatalogTypeCodelist] = Type is not null ? new[] { Type.ToString() } : Array.Empty<string>();

            LanguageDependedTexts names = GetLiteralNodesFromUriNode("dct:title").ToArray();

            if (metadata is null)
            {
                metadata = new FileMetadata(id, names, FileType.LocalCatalogRegistration, null, Publisher?.ToString(), ShouldBePublic, null, now, now, values);
            }
            else
            {
                metadata = metadata with { Name = names, Publisher = Publisher?.ToString(), IsPublic = ShouldBePublic, AdditionalValues = values, LastModified = now };
            }
            return metadata;
        }
    }
}
