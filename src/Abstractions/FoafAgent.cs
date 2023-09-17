using AngleSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace NkodSk.Abstractions
{
    public class FoafAgent : RdfObject
    {
        public FoafAgent(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public string? GetName(string language) => GetTextFromUriNode("foaf:name", language);

        public void SetNames(Dictionary<string, string> values)
        {
            SetTexts("foaf:name", values);
        }

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

        public static FoafAgent Create(Uri uri)
        {
            IGraph graph = new Graph();
            RdfDocument.AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("foaf:Agent");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            return new FoafAgent(graph, subject);
        }

        public FileMetadata UpdateMetadata(bool isPublic, FileMetadata? metadata = null)
        {
            Guid id = metadata?.Id ?? Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Dictionary<string, string[]> values = new Dictionary<string, string[]>();
            
            LanguageDependedTexts names = GetLiteralNodesFromUriNode("foaf:name").ToArray();
            if (metadata is null)
            {
                metadata = new FileMetadata(id, names, FileType.PublisherRegistration, null, Uri.ToString(), isPublic, null, now, now, values);
            }
            else
            {
                metadata = metadata with { Name = names, Publisher = Uri.ToString(), IsPublic = isPublic, AdditionalValues = values, LastModified = now };
            }
            return metadata;
        }
    }
}
