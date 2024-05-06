using Lucene.Net.Search.Similarities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Tokens;

namespace NkodSk.Abstractions
{
    public class DcatDistribution : RdfObject
    {
        //public const string AuthorsWorkTypeCodelist = "https://data.gov.sk/set/codelist/authors-work-type";

        //public const string OriginalDatabaseTypeCodelist = "https://data.gov.sk/set/codelist/original-database-type";

        //public const string DatabaseProtectedBySpecialRightsTypeCodelist = "https://data.gov.sk/set/codelist/database-creator-special-rights-type";

        public const string PersonalDataContainmentTypeCodelist = "https://data.gov.sk/set/codelist/personal-data-occurence-type";

        public const string LicenseCodelist = "http://publications.europa.eu/resource/authority/licence";

        public const string FormatCodelist = "http://publications.europa.eu/resource/authority/file-type";

        public const string MediaTypeCodelist = "http://www.iana.org/assignments/media-types";

        private Guid? createdId;

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

        public void SetTermsOfUse(Uri? authorsWorkType, Uri? originalDatabaseType, Uri? databaseProtectedBySpecialRightsType, Uri? personalDataContainmentType, string? authorName, string? originalDatabaseAuthorName)
        {
            RemoveUriNodes("leg:termsOfUse");
            LegTermsOfUse termsOfUse = new LegTermsOfUse(Graph, CreateSubject("leg:termsOfUse", "leg:TermsOfUse", "terms"));
            termsOfUse.AuthorsWorkType = authorsWorkType;
            termsOfUse.OriginalDatabaseType = originalDatabaseType;
            termsOfUse.DatabaseProtectedBySpecialRightsType = databaseProtectedBySpecialRightsType;
            termsOfUse.PersonalDataContainmentType = personalDataContainmentType;
            termsOfUse.OriginalDatabaseAuthorName = originalDatabaseAuthorName;
            termsOfUse.AuthorName = authorName;
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

        public Dictionary<string, string> Title => GetTextsFromUriNode("dct:title");

        public void SetTitle(Dictionary<string, string>? values)
        {
            SetTexts("dct:title", values);
        }

        public Uri? AccessService
        {
            get => GetUriFromUriNode("dcat:accessService");
            set => SetUriNode("dcat:accessService", value);
        }

        public DcatDataService? DataService
        {
            get
            {
                Uri? accessService = AccessService;
                if (accessService is not null)
                {
                    IUriNode node = Graph.GetUriNode(accessService);
                    return new DcatDataService(Graph, node);
                }
                return null;
            }
        }

        public DcatDataService GetOrCreateDataSerice()
        {
            DcatDataService? dataService = DataService;
            if (dataService is null)
            {
                dataService = new DcatDataService(Graph, CreateSubject("dcat:accessService", "dcat:DataService", "service"));
            }
            return dataService;
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

        public static DcatDistribution Create(Guid datasetId)
        {
            Guid id = Guid.NewGuid();
            Uri uri = new Uri($"https://data.gov.sk/set/{datasetId}/resource/{id}");

            IGraph graph = new Graph();
            RdfDocument.AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("dcat:Distribution");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            DcatDistribution distribution = new DcatDistribution(graph, subject);
            distribution.createdId = id;
            return distribution;
        }

        public FileMetadata UpdateMetadata(Guid datasetId, string? publisher, FileMetadata? metadata = null)
        {
            Guid id = metadata?.Id ?? createdId ?? Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Dictionary<string, string[]> values = new Dictionary<string, string[]>();

            values["key"] = new string[] { Uri.ToString() };

            if (IsHarvested)
            {
                values["Harvested"] = new string[] { "true" };
            }

            if (metadata is null)
            {
                metadata = new FileMetadata(id, Format?.ToString() ?? id.ToString(), FileType.DistributionRegistration, datasetId, publisher, true, null, now, now, values);
            }
            else
            {
                metadata = metadata with { Publisher = publisher, IsPublic = true, AdditionalValues = values, LastModified = now };
            }
            return metadata;
        }

        public FileMetadata UpdateMetadata(FileMetadata datasetMetadata, FileMetadata? metadata = null)
        {
            return UpdateMetadata(datasetMetadata.Id, datasetMetadata.Publisher, metadata);
        }

        public static FileMetadata ClearDatasetMetadata(FileMetadata metadata)
        {
            if (metadata.AdditionalValues is not null)
            {
                metadata.AdditionalValues.Remove(FormatCodelist);
                metadata.AdditionalValues.Remove(LicenseCodelist);
                metadata.AdditionalValues.Remove(PersonalDataContainmentTypeCodelist);
            }
            return metadata;
        }

        public FileMetadata UpdateDatasetMetadata(FileMetadata metadata)
        {
            Dictionary<string, string[]> values = metadata.AdditionalValues ?? new Dictionary<string, string[]>();

            HashSet<string> formats = values.ContainsKey(FormatCodelist) ? new HashSet<string>(values[FormatCodelist]) : new HashSet<string>();
            if (Format is not null)
            {
                formats.Add(Format.ToString());
            }
            string[] formatsAsArray = formats.ToArray();
            Array.Sort(formatsAsArray);
            values[FormatCodelist] = formatsAsArray;

            HashSet<string> licences = values.ContainsKey(LicenseCodelist) ? new HashSet<string>(values[LicenseCodelist]) : new HashSet<string>();
            if (TermsOfUse?.AuthorsWorkType is not null)
            {
                licences.Add(TermsOfUse.AuthorsWorkType.ToString());
            }
            if (TermsOfUse?.OriginalDatabaseType is not null)
            {
                licences.Add(TermsOfUse.OriginalDatabaseType.ToString());
            }
            if (TermsOfUse?.DatabaseProtectedBySpecialRightsType is not null)
            {
                licences.Add(TermsOfUse.DatabaseProtectedBySpecialRightsType.ToString());
            }
            string[] licencesAsArray = licences.ToArray();
            Array.Sort(licencesAsArray);
            values[LicenseCodelist] = licencesAsArray;

            HashSet<string> personalDatas = values.ContainsKey(PersonalDataContainmentTypeCodelist) ? new HashSet<string>(values[PersonalDataContainmentTypeCodelist]) : new HashSet<string>();
            if (TermsOfUse?.PersonalDataContainmentType is not null)
            {
                personalDatas.Add(TermsOfUse.PersonalDataContainmentType.ToString());
            }
            string[] personalDatasAsArray = personalDatas.ToArray();
            Array.Sort(personalDatasAsArray);
            values[PersonalDataContainmentTypeCodelist] = personalDatasAsArray;

            return metadata with { AdditionalValues = values };
        }

        public void IncludeInDataset(DcatDataset dataset)
        {
            dataset.Graph.Assert(dataset.Node, dataset.Graph.CreateUriNode("dcat:distribution"), Node);
            foreach (Triple t in Triples)
            {
                dataset.Graph.Assert(t);
            }
        }

        public bool IsEqualTo(DcatDistribution distribution)
        {
            if (!Equals(TermsOfUse?.AuthorsWorkType, distribution.TermsOfUse?.AuthorsWorkType) ||
                !Equals(TermsOfUse?.OriginalDatabaseType, distribution.TermsOfUse?.OriginalDatabaseType) ||
                !Equals(TermsOfUse?.DatabaseProtectedBySpecialRightsType, distribution.TermsOfUse?.DatabaseProtectedBySpecialRightsType) ||
                !Equals(TermsOfUse?.PersonalDataContainmentType, distribution.TermsOfUse?.PersonalDataContainmentType) ||
                !Equals(TermsOfUse?.AuthorName, distribution.TermsOfUse?.AuthorName) ||
                !Equals(TermsOfUse?.OriginalDatabaseAuthorName, distribution.TermsOfUse?.OriginalDatabaseAuthorName) ||
                !Equals(DownloadUrl, distribution.DownloadUrl) ||
                !Equals(AccessUrl, distribution.AccessUrl) ||
                !Equals(Format, distribution.Format) ||
                !Equals(MediaType, distribution.MediaType) ||
                !Equals(ConformsTo, distribution.ConformsTo) ||
                !Equals(CompressFormat, distribution.CompressFormat) ||
                !Equals(PackageFormat, distribution.PackageFormat) ||
                !AreLaguagesEqual(Title, distribution.Title))
            {
                return false;
            }
            return true;
        }
    }
}
