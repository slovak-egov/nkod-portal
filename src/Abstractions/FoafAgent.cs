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
        public const string LegalFormCodelist = "https://data.gov.sk/set/codelist/CL000056";

        public FoafAgent(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public Dictionary<string, string> Name => GetTextsFromUriNode("foaf:name");

        public string? GetName(string language) => GetTextFromUriNode("foaf:name", language);

        public void SetNames(Dictionary<string, string> values)
        {
            SetTexts("foaf:name", values);
        }

        public Uri? HomePage
        {
            get => GetUriFromUriNode("foaf:homepage");
            set => SetUriNode("foaf:homepage", value);
        }

        public string? EmailAddress
        {
            get => GetTextFromUriNode("foaf:mbox");
            set => SetTextToUriNode("foaf:mbox", value);
        }

        public string? Phone
        {
            get => GetTextFromUriNode("foaf:phone");
            set => SetTextToUriNode("foaf:phone", value);
        }

        public Uri? LegalForm
        {
            get => GetUriFromUriNode("ls:legalForm");
            set => SetUriNode("ls:legalForm", value);
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

        public FileMetadata UpdateMetadata(FileMetadata? metadata = null)
        {
            Guid id = metadata?.Id ?? Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Dictionary<string, string[]> values = new Dictionary<string, string[]>();
            
            LanguageDependedTexts names = GetLiteralNodesFromUriNode("foaf:name").ToArray();
            if (metadata is null)
            {
                metadata = new FileMetadata(id, names, FileType.PublisherRegistration, null, Uri.ToString(), false, null, now, now, values);
            }
            else
            {
                metadata = metadata with { Name = names, Publisher = Uri.ToString(), AdditionalValues = values, LastModified = now };
            }
            return metadata;
        }
    }
}
