using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace NkodSk.Abstractions
{
    public class DcatCatalog : RdfObject
    {
        public DcatCatalog(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public string? GetTitle(string language) => GetTextFromUriNode("dct:title", language);

        public string? GetDescription(string language) => GetTextFromUriNode("dct:description", language);

        public Uri? Publisher => GetUriFromUriNode("dct:publisher");

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

        public Uri? HomePage => GetUriFromUriNode("foaf:homepage");

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

        public FileMetadata UpdateMetadata(FileMetadata? metadata = null)
        {
            Guid id = metadata?.Id ?? Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Dictionary<string, string[]> values = new Dictionary<string, string[]>();
            if (metadata is null)
            {
                metadata = new FileMetadata(id, id.ToString(), FileType.DatasetRegistration, null, Publisher?.ToString(), true, null, now, now, values);
            }
            else
            {
                metadata = metadata with { Publisher = Publisher?.ToString(), IsPublic = true, AdditionalValues = values, LastModified = now };
            }
            return metadata;
        }
    }
}
