using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

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

        public Uri? DownloadUrl => GetUriFromUriNode("dcat:downloadURL");

        public Uri? AccessUrl => GetUriFromUriNode("dcat:accessURL");

        public Uri? Format => GetUriFromUriNode("dct:format");

        public Uri? MediaType => GetUriFromUriNode("dcat:mediaType");

        public Uri? ConformsTo => GetUriFromUriNode("dct:conformsTo");

        public Uri? CompressFormat => GetUriFromUriNode("dcat:compressFormat");

        public Uri? PackageFormat => GetUriFromUriNode("dcat:packageFormat");

        public string? GetTitle(string language) => GetTextFromUriNode("dct:title", language);

        public Uri? AccessService => GetUriFromUriNode("dcat:accessService");

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

        public FileMetadata UpdateMetadata(FileMetadata datasetMetadata, FileMetadata? metadata = null)
        {
            Guid id = metadata?.Id ?? Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Dictionary<string, string[]> values = new Dictionary<string, string[]>();
            if (metadata is null)
            {
                metadata = new FileMetadata(id, id.ToString(), FileType.DatasetRegistration, null, datasetMetadata.Publisher?.ToString(), datasetMetadata.IsPublic, null, now, now, values);
            }
            else
            {
                metadata = metadata with { Publisher = datasetMetadata.Publisher?.ToString(), IsPublic = datasetMetadata.IsPublic, AdditionalValues = values, LastModified = now };
            }
            return metadata;
        }
    }
}
