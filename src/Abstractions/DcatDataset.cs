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
        public const string AccrualPeriodicityCodelist = "http://publications.europa.eu/resource/authority/frequency";

        public const string ThemeCodelist = "http://publications.europa.eu/resource/authority/data-theme";

        public const string TypeCodelist = "https://data.gov.sk/set/codelist/dataset-type";

        public const string SpatialCodelist = "https://data.gov.sk/def/ontology/location";

        public const string EuroVocThemeCodelist = "http://eurovoc.europa.eu/100141";

        public const string EuroVocPrefix = "http://eurovoc.europa.eu/";

        public const string HvdCategoryCodelist = "http://publications.europa.eu/resource/dataset/high-value-dataset-category";

        public const string HvdType = "http://publications.europa.eu/resource/authority/dataset-type/HVD";

        public const string HvdLegislation = "http://data.europa.eu/eli/reg_impl/2023/138/oj";

        private Guid? createdId;

        private DateTimeOffset? issued;

        private DateTimeOffset? modified;

        public DcatDataset(IGraph graph, IUriNode node) : base(graph, node)
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

        public IEnumerable<Uri> Themes
        {
            get => GetUrisFromUriNode("dcat:theme");
            set => SetUriNodes("dcat:theme", value);
        }

        public IEnumerable<Uri> EuroVocThemes => Themes.Where(t => t.ToString().StartsWith(EuroVocPrefix));

        public IEnumerable<Uri> NonEuroVocThemes => Themes.Where(t => !t.ToString().StartsWith(EuroVocPrefix));

        public Dictionary<string, List<string>> EuroVocThemeLabels => GetTextsFromUriNodeAll("custom:euroVocLabels");

        public IEnumerable<string> GetEuroVocLabelThemes(string language) => GetTextsFromUriNode("custom:euroVocLabels", language);

        public void SetEuroVocLabelThemes(Dictionary<string, List<string>> texts) => SetTexts("custom:euroVocLabels", texts);

        public Uri? AccrualPeriodicity
        {
            get => GetUriFromUriNode("dct:accrualPeriodicity");
            set => SetUriNode("dct:accrualPeriodicity", value);
        }

        public Dictionary<string, List<string>> Keywords => GetTextsFromUriNodeAll("dcat:keyword");

        public IEnumerable<string> GetKeywords(string language) => GetTextsFromUriNode("dcat:keyword", language);

        public void SetKeywords(Dictionary<string, List<string>> texts) => SetTexts("dcat:keyword", texts);

        public IEnumerable<Uri> Type
        {
            get => GetUrisFromUriNode("dct:type");
            set => SetUriNodes("dct:type", value);
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
            if (startDate.HasValue || endDate.HasValue)
            {
                DctTemporal temporal = new DctTemporal(Graph, CreateSubject("dct:temporal", "dct:PeriodOfTime", "temporal"));

                temporal.StartDate = startDate;
                temporal.EndDate = endDate;
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
                VcardKind contactPoint = new VcardKind(Graph, CreateSubject("dcat:contactPoint", "vcard:Individual", "contact-point"));
                contactPoint.SetNames(name ?? new LanguageDependedTexts());
                contactPoint.Email = !string.IsNullOrEmpty(email) ? email : null;
            }
        }

        public Uri? LandingPage
        {
            get => GetUriFromUriNode("dcat:landingPage");
            set => SetUriNode("dcat:landingPage", value);
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
            set => SetTextToUriNode("dct:temporalResolution", value, new Uri(RdfDocument.XsdPrefix + "duration"));
        }

        public IEnumerable<Uri> ApplicableLegislations
        {
            get => GetUrisFromUriNode("dcatap:applicableLegislation");
            set => SetUriNodes("dcatap:applicableLegislation", value);
        }

        public Uri? HvdCategory
        {
            get => GetUriFromUriNode("dcatap:hvdCategory");
            set => SetUriNode("dcatap:hvdCategory", value);
        }

        public bool IsHvd => Type.Any(t => string.Equals(t.ToString(), HvdType, StringComparison.OrdinalIgnoreCase));

        public Uri? IsPartOf
        {
            get => GetUriFromUriNode("dct:isPartOf");
            set => SetUriNode("dct:isPartOf", value);
        }

        public string? IsPartOfInternalId
        {
            get => GetTextFromUriNode("custom:isPartOf");
            set => SetTextToUriNode("custom:isPartOf", value);
        }

        public bool ShouldBePublic
        {
            get => GetBooleanFromUriNode("custom:shouldBePublic") ?? true;
            set => SetBooleanToUriNode("custom:shouldBePublic", value);
        }

        public bool IsSerie
        {
            get => GetBooleanFromUriNode("custom:isSerie") ?? false;
            set => SetBooleanToUriNode("custom:isSerie", value);
        }

        public DateTimeOffset? Issued
        {
            get => issued ?? GetDateTimeFromUriNode("dct:issued");
            set
            {
                issued = value;
                SetDateTimeToUriNode("dct:issued", value);
            }
        }

        public DateTimeOffset? Modified
        {
            get => modified ?? GetDateTimeFromUriNode("dct:modified");
            set
            {
                modified = value;
                SetDateTimeToUriNode("dct:modified", value);
            }
        }

        public IEnumerable<Uri> Distributions => GetUrisFromUriNode("dcat:distribution");

        public bool UpdateModifiedDate { get; set; }

        public static DcatDataset Create()
        {
            Guid id = Guid.NewGuid();
            Uri uri = new Uri($"https://data.gov.sk/set/{id}");

            IGraph graph = new Graph();
            RdfDocument.AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("dcat:Dataset");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            DcatDataset dataset = new DcatDataset(graph, subject);
            dataset.createdId = id;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            dataset.Issued = now;
            dataset.Modified = now;
            return dataset;
        }

        public static DcatDataset? Parse(string text)
        {
            (IGraph graph, IEnumerable<IUriNode> nodes) = Parse(text, "dcat:Dataset");
            IUriNode? node = nodes.FirstOrDefault();

            if (node is null)
            {
                nodes = RdfDocument.ParseNode(graph, "dcat:DatasetSeries");
                node = nodes.FirstOrDefault();
            }

            if (node is not null)
            {
                return new DcatDataset(graph, node);
            }
            return null;
        }

        public FileMetadata UpdateMetadata(bool isPublic, FoafAgent? publisher, FileMetadata? metadata = null, DateTimeOffset? effectiveDate = null)
        {
            Guid id = metadata?.Id ?? createdId ?? Guid.NewGuid();
            DateTimeOffset now = effectiveDate ?? DateTimeOffset.UtcNow;
            Dictionary<string, string[]> values = metadata?.AdditionalValues ?? new Dictionary<string, string[]>();
            isPublic = isPublic && ShouldBePublic;

            values[TypeCodelist] = Type.Select(v => v.ToString()).ToArray();
            values[ThemeCodelist] = NonEuroVocThemes.Select(v => v.ToString()).ToArray();
            values[AccrualPeriodicityCodelist] = AccrualPeriodicity is not null ? new[] { AccrualPeriodicity.ToString() } : Array.Empty<string>();
            values["serie"] = new string[] { IsSerie ? "1" : "0" };
            values["key"] = new string[] { Uri.ToString() };
            
            if (LandingPage is not null)
            {
                values["landingPage"] = new string[] { LandingPage.ToString() };
            }
            else
            {
                values.Remove("landingPage");
            }

            if (IsHarvested)
            {
                values["Harvested"] = new string[] { "true" };
            }
            else
            {
                values.Remove("Harvested");
            }

            if (publisher?.LegalForm is not null)
            {
                values[FoafAgent.LegalFormCodelist] = new string[] { publisher.LegalForm.ToString() };
            }

            foreach ((string language, List<string> texts) in Keywords)
            {
                values["keywords_" + language] = texts.ToArray();
            }

            Guid? parentId = metadata?.ParentFile;
            if (!string.IsNullOrEmpty(IsPartOfInternalId) && Guid.TryParse(IsPartOfInternalId, out Guid parentIdValue))
            {
                parentId = parentIdValue;
            }

            LanguageDependedTexts names = GetLiteralNodesFromUriNode("dct:title").ToArray();
            if (metadata is null)
            {
                metadata = new FileMetadata(id, names, FileType.DatasetRegistration, parentId, Publisher?.ToString(), isPublic, null, Issued ?? now, Modified ?? now, values);
            }
            else
            {
                metadata = metadata with { Name = names, ParentFile = parentId, Publisher = Publisher?.ToString(), IsPublic = isPublic, AdditionalValues = values, LastModified = UpdateModifiedDate ? now : (Modified ?? now) };
            }
            return metadata;
        }

        public async Task<FileMetadata> UpdateReferenceToParent(Guid? parentId, FileMetadata metadata, IDocumentStorageClient documentStorageClient)
        {
            IsPartOfInternalId = parentId?.ToString();

            if (parentId.HasValue)
            {
                FileState? state = await documentStorageClient.GetFileState(parentId.Value);
                if (state?.Content is not null)
                {
                    DcatDataset? parent = Parse(state.Content);
                    IsPartOf = parent?.Uri;
                }
                else
                {
                    IsPartOf = null;
                }
            }
            else
            {
                IsPartOf = null;
            }

            return metadata with { ParentFile = parentId };
        }

        public void RemoveAllDistributions()
        {
            foreach (Triple t in Graph.GetTriplesWithSubjectPredicate(Node, Graph.CreateUriNode("dcat:distribution")).ToList())
            {
                Graph.Retract(t);
                if (t.Object is IUriNode node)
                {
                    RemoveTriples(node);
                }
            }
        }

        public bool IsEqualTo(DcatDataset dataset)
        {
            if (!AreLaguagesEqual(Title, dataset.Title) ||
                !AreLaguagesEqual(Description, dataset.Description) ||
                !Equals(AccrualPeriodicity, dataset.AccrualPeriodicity) ||
                !AreEquivalent(Themes.ToList(), dataset.Themes.ToList()) ||
                !AreLaguagesEqual(Keywords, dataset.Keywords) ||
                !AreEquivalent(Spatial.ToList(), dataset.Spatial.ToList()) ||
                !Equals(Temporal?.StartDate, dataset.Temporal?.StartDate) ||
                !Equals(Temporal?.EndDate, dataset.Temporal?.EndDate) ||
                !AreLaguagesEqual(ContactPoint?.Name, dataset.ContactPoint?.Name) ||
                !Equals(ContactPoint?.Email, dataset.ContactPoint?.Email) ||
                !Equals(LandingPage, dataset.LandingPage) ||
                !Equals(Specification, dataset.Specification) ||
                !Equals(SpatialResolutionInMeters, dataset.SpatialResolutionInMeters) ||
                !Equals(TemporalResolution, dataset.TemporalResolution) ||
                !AreLaguagesEqual(EuroVocThemeLabels, dataset.EuroVocThemeLabels) ||
                !Equals(IsSerie, dataset.IsSerie) ||
                !Equals(IsPartOf, dataset.IsPartOf) ||
                !Equals(IsPartOfInternalId, dataset.IsPartOfInternalId) ||
                !Equals(ShouldBePublic, dataset.ShouldBePublic) ||
                !Equals(Publisher, dataset.Publisher))
            {
                return false;
            }

            return true;
        }
    }
}
