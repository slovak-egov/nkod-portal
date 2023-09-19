using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace NkodSk.Abstractions
{
    public class DcatDataset : RdfObject
    {
        public DcatDataset(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public string? GetTitle(string language) => GetTextFromUriNode("dct:title", language);

        public IDictionary<string, List<string>> Title => GetTextsFromUriNode("dct:title");

        public void SetTitle(Dictionary<string, string> values)
        {
            SetTexts("dct:title", values);
        }

        public string? GetDescription(string language) => GetTextFromUriNode("dct:description", language);

        public IDictionary<string, List<string>> Description => GetTextsFromUriNode("dct:description");

        public void SetDescription(Dictionary<string, string> values)
        {
            SetTexts("dct:description", values);
        }

        public Uri? Publisher
        {
            get => GetUriFromUriNode("dct:publisher");
            set => SetUriNode("dct:publisher", value);
        }

        public IEnumerable<Uri> Themes
        {
            get => GetUrisFromUriNode("dcat:theme");
            set => SetUriNodes("dcat:theme", value);
        }

        public Uri? AccrualPeriodicity
        {
            get => GetUriFromUriNode("dct:accrualPeriodicity");
            set => SetUriNode("dct:accrualPeriodicity", value);
        }

        public IDictionary<string, List<string>> Keywords => GetTextsFromUriNode("dcat:keyword");

        public IEnumerable<string> GetKeywords(string language) => GetTextsFromUriNode("dcat:keyword", language);

        public void SetKeywords(Dictionary<string, IEnumerable<string>> texts) => SetTexts("dcat:keyword", texts);

        public Uri? Type
        {
            get => GetUriFromUriNode("dct:type");
            set => SetUriNode("dct:type", value);
        }

        public IEnumerable<Uri> Spatial
        {
            get => GetUrisFromUriNode("dct:spatial");
            set => SetUriNodes("dct:spatial", value);
        }

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

        public void SetTemporal(DateOnly? startDate, DateOnly? endDate)
        {
            RemoveUriNodes("dct:temporal");
            if (startDate.HasValue && endDate.HasValue)
            {
                DctTemporal temporal = new DctTemporal(Graph, CreateSubject("dct:temporal", "dct:PeriodOfTime"));

                temporal.StartDate = startDate.Value;
                temporal.EndDate = endDate.Value;
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

        public void SetContactPoint(LanguageDependedTexts? name, string? email)
        {
            RemoveUriNodes("dcat:contactPoint");
            if (name is not null || email is not null)
            {
                VcardKind contactPoint = new VcardKind(Graph, CreateSubject("dcat:contactPoint", "vcard:Individual"));
                contactPoint.SetNames(name ?? new LanguageDependedTexts());
                contactPoint.Email = email;
            }
        }

        public Uri? Documentation
        {
            get => GetUriFromUriNode("foaf:page");
            set => SetUriNode("foaf:page", value);
        }

        public Uri? Specification
        {
            get => GetUriFromUriNode("dct:conformsTo");
            set => SetUriNode("dct:conformsTo", value);
        }

        public decimal? SpatialResolutionInMeters
        {
            get => GetDecimalFromUriNode("dcat:spatialResolutionInMeters");
            set => SetDecimalToUriNode("dcat:spatialResolutionInMeters", value);
        }

        public string? TemporalResolution
        {
            get => GetTextFromUriNode("dct:temporalResolution");
            set => SetTextToUriNode("dct:temporalResolution", value);
        }


        public Uri? IsPartOf
        {
            get => GetUriFromUriNode("dct:isPartOf");
            set => SetUriNode("dct:isPartOf", value);
        }

        public bool ShouldBePublic
        {
            get => GetBooleanFromUriNode("custom:shouldBePublic") ?? true;
            set => SetBooleanToUriNode("custom:shouldBePublic", value);
        }

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
            isPublic = isPublic && ShouldBePublic;

            Uri? type = Type;
            if (type is not null)
            {
                values["https://data.gov.sk/set/codelist/dataset-type"] = new[] { type.ToString() };
            }

            foreach ((string language, List<string> texts) in Keywords)
            {
                values["keywords" + language] = texts.ToArray();
            }

            LanguageDependedTexts names = GetLiteralNodesFromUriNode("dct:title").ToArray();
            if (metadata is null)
            {
                metadata = new FileMetadata(id, names, FileType.DatasetRegistration, null, Publisher?.ToString(), isPublic, null, now, now, values);
            }
            else
            {
                metadata = metadata with { Name = names, Publisher = Publisher?.ToString(), IsPublic = isPublic, AdditionalValues = values, LastModified = now };
            }
            return metadata;
        }
    }
}
