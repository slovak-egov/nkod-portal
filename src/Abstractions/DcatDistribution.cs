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
    public class DcatDistribution : RdfObject
    {
        public DcatDistribution(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public LegTermsOfUse? TermsOfUse
        {
            get
            {
                IUriNode nodeType = Graph.GetUriNode("leg:termsOfUse");
                if (nodeType is not null)
                {
                    IUriNode? node = Graph.GetTriplesWithSubjectPredicate(Node, nodeType).Select(x => x.Object).OfType<IUriNode>().FirstOrDefault();
                    if (node is not null)
                    {
                        return new LegTermsOfUse(Graph, node);
                    }
                }
                return null;
            }
        }

        public void SetTermsOfUse(Uri? authorsWorkType, Uri? originalDatabaseType, Uri? databaseProtectedBySpecialRightsType, Uri? personalDataContainmentType)
        {
            RemoveUriNodes("leg:termsOfUse");
            LegTermsOfUse termsOfUse = new LegTermsOfUse(Graph, CreateSubject("leg:termsOfUse", "leg:TermsOfUse"));
            termsOfUse.AuthorsWorkType = authorsWorkType;
            termsOfUse.OriginalDatabaseType = originalDatabaseType;
            termsOfUse.DatabaseProtectedBySpecialRightsType = databaseProtectedBySpecialRightsType;
            termsOfUse.PersonalDataContainmentType = personalDataContainmentType;
        }


        public Uri? DownloadUrl
        {
            get => GetUriFromUriNode("dcat:downloadURL");
            set => SetUriNode("dcat:downloadURL", value);
        }

        public Uri? AccessUrl
        {
            get => GetUriFromUriNode("dcat:accessURL");
            set => SetUriNode("dcat:accessURL", value);
        }

        public Uri? Format
        {
            get => GetUriFromUriNode("dct:format");
            set => SetUriNode("dct:format", value);
        }

        public Uri? MediaType
        {
            get => GetUriFromUriNode("dcat:mediaType");
            set => SetUriNode("dcat:mediaType", value);
        }

        public Uri? ConformsTo
        {
            get => GetUriFromUriNode("dct:conformsTo");
            set => SetUriNode("dct:conformsTo", value);
        }

        public Uri? CompressFormat
        {
            get => GetUriFromUriNode("dcat:compressFormat");
            set => SetUriNode("dcat:compressFormat", value);
        }

        public Uri? PackageFormat
        {
            get => GetUriFromUriNode("dcat:packageFormat");
            set => SetUriNode("dcat:packageFormat", value);
        }

        public string? GetTitle(string language) => GetTextFromUriNode("dct:title", language);

        public IDictionary<string, List<string>> Title => GetTextsFromUriNode("dct:title");

        public void SetTitle(Dictionary<string, string>? values)
        {
            SetTexts("dct:title", values);
        }

        public Uri? AccessService
        {
            get => GetUriFromUriNode("dcat:accessService"); 
            set => SetUriNode("dcat:accessService", value);
        }

        public static DcatDistribution? Parse(string text)
        {
            (IGraph graph, IEnumerable<IUriNode> nodes) = Parse(text, "dcat:Distribution");
            IUriNode? node = nodes.FirstOrDefault();
            if (node is not null)
            {
                return new DcatDistribution(graph, node);
            }
            return null;
        }

        public static DcatDistribution Create(Uri uri)
        {
            IGraph graph = new Graph();
            RdfDocument.AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("dcat:Distribution");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            return new DcatDistribution(graph, subject);
        }

        public FileMetadata UpdateMetadata(FileMetadata datasetMetadata, FileMetadata? metadata = null)
        {
            Guid id = metadata?.Id ?? Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Dictionary<string, string[]> values = new Dictionary<string, string[]>();
            if (metadata is null)
            {
                metadata = new FileMetadata(id, Format?.ToString() ?? id.ToString(), FileType.DistributionRegistration, datasetMetadata.Id, datasetMetadata.Publisher?.ToString(), true, null, now, now, values);
            }
            else
            {
                metadata = metadata with { Publisher = datasetMetadata.Publisher?.ToString(), IsPublic = true, AdditionalValues = values, LastModified = now };
            }
            return metadata;
        }
    }
}
