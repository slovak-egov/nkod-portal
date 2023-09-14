using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;
using static Lucene.Net.Documents.Field;

namespace NkodSk.Abstractions
{
    public class DcatDataset : RdfObject
    {
        public DcatDataset(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public string? GetTitle(string language) => GetTextFromUriNode("dct:title", language);

        public void SetTitle(Dictionary<string, string> values)
        {
            SetTexts("dct:title", values);
        }

        public string? GetDescription(string language) => GetTextFromUriNode("dct:description", language);

        public Uri? Publisher
        {
            get => GetUriFromUriNode("dct:publisher");
            set => SetUriNode("dct:publisher", value);
        }

        public IEnumerable<Uri> GetThemes() => GetUrisFromUriNode("dcat:theme");

        public Uri? AccrualPeriodicity => GetUriFromUriNode("dct:accrualPeriodicity");

        public IEnumerable<string> GetKeywords(string language) => GetTextsFromUriNode("dcat:keyword", language);

        public Uri? Type => GetUriFromUriNode("dct:type");

        public IEnumerable<Uri> Spatial => GetUrisFromUriNode("dct:spatial");

        public DctTemporal? Temporal
        {
            get
            {
                IUriNode periodOfTimeNodeType = Graph.GetUriNode("dct:temporal");
                if (periodOfTimeNodeType is not null)
                {
                    IUriNode? periodOfTimeNode =  Graph.GetTriplesWithSubjectPredicate(Node, periodOfTimeNodeType).Select(x => x.Object).OfType<IUriNode>().FirstOrDefault();
                    if (periodOfTimeNode is not null)
                    {
                        return new DctTemporal(Graph, periodOfTimeNode);
                    }
                }
                return null;
            }
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

        public Uri? Documentation => GetUriFromUriNode("foaf:page");

        public Uri? Specification => GetUriFromUriNode("dct:conformsTo");

        public IEnumerable<Uri> EuroVocThemes => GetUrisFromUriNode("dcat:theme");

        public decimal? SpatialResolutionInMeters => GetDecimalFromUriNode("dcat:spatialResolutionInMeters");

        public string? TemporalResolution => GetTextFromUriNode("dct:temporalResolution");

        public Uri? IsPartOf => GetUriFromUriNode("dct:isPartOf");

        public IEnumerable<Uri> Distributions => GetUrisFromUriNode("dcat:distribution");

        public static DcatDataset Create(Uri uri)
        {
            IGraph graph = new Graph();
            RdfDocument.AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("dcat:Dataset");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            return new DcatDataset(graph, subject);
        }

        public static DcatDataset? Parse(string text)
        {
            (IGraph graph, IEnumerable<IUriNode> nodes) = Parse(text, "dcat:Dataset");
            IUriNode? node = nodes.FirstOrDefault();
            if (node is not null)
            {
                return new DcatDataset(graph, node);
            }
            return null;
        }

        public FileMetadata UpdateMetadata(bool isPublic, FileMetadata? metadata = null)
        {
            Guid id = metadata?.Id ?? Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Dictionary<string, string[]> values = new Dictionary<string, string[]>();
            if (metadata is null)
            {
                metadata = new FileMetadata(id, id.ToString(), FileType.DatasetRegistration, null, Publisher?.ToString(), isPublic, null, now, now, values);
            }
            else
            {
                metadata = metadata with { Publisher = Publisher?.ToString(), IsPublic = isPublic, AdditionalValues = values, LastModified = now };
            }
            return metadata;
        }
    }
}
