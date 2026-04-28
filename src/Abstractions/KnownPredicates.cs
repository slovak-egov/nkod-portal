using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace Abstractions
{
    public static class KnownPredicates
    {
        public static readonly Uri DctTitle = DctPredicate("title");

        public static readonly Uri DctDescription = DctPredicate("description");

        public static readonly Uri DctPublisher = DctPredicate("publisher");

        public static readonly Uri DctIssued = DctPredicate("issued");

        public static readonly Uri DctModified = DctPredicate("modified");

        public static readonly Uri DctAccrualPeriodicity = DctPredicate("accrualPeriodicity");

        public static readonly Uri DctType = DctPredicate("type");

        public static readonly Uri DctSpatial = DctPredicate("spatial");

        public static readonly Uri DctTemporal = DctPredicate("temporal");

        public static readonly Uri DctConformsTo = DctPredicate("conformsTo");

        public static readonly Uri DctRelation = DctPredicate("relation");

        public static readonly Uri DctFormat = DctPredicate("format");

        public static readonly Uri DcatDataset = DcatPredicate("dataset");
        
        public static readonly Uri DcatCatalogClass = DcatPredicate("Catalog");

        public static readonly Uri DcatDatasetClass = DcatPredicate("Dataset");

        public static readonly Uri DcatDatasetSeriesClass = DcatPredicate("DatasetSeries");

        public static readonly Uri DcatDistribution = DcatPredicate("distribution");

        public static readonly Uri DcatDistributionClass = DcatPredicate("Distribution");

        public static readonly Uri DcatDataServiceClass = DcatPredicate("DataService");

        public static readonly Uri DcatTheme = DcatPredicate("theme");

        public static readonly Uri DcatKeyword = DcatPredicate("keyword");

        public static readonly Uri DcatStartDate = DcatPredicate("startDate");

        public static readonly Uri DcatEndDate = DcatPredicate("endDate");

        public static readonly Uri DcatContactPoint = DcatPredicate("contactPoint");

        public static readonly Uri DcatLandingPage = DcatPredicate("landingPage");

        public static readonly Uri DcatTemporalResolution = DcatPredicate("temporalResolution");

        public static readonly Uri DcatSpatialResolutionInMeters = DcatPredicate("spatialResolutionInMeters");

        public static readonly Uri DcatInSeries = DcatPredicate("inSeries");

        public static readonly Uri DcatDownloadUrl = DcatPredicate("downloadURL");

        public static readonly Uri DcatAccessUrl = DcatPredicate("accessURL");

        public static readonly Uri DcatMediaType = DcatPredicate("mediaType");

        public static readonly Uri DcatCompressFormat = DcatPredicate("compressFormat");

        public static readonly Uri DcatPackageFormat = DcatPredicate("packageFormat");

        public static readonly Uri DcatAccessService = DcatPredicate("accessService");

        public static readonly Uri DcatEndpointUrl = DcatPredicate("endpointURL");

        public static readonly Uri DcatEndpointDescription = DcatPredicate("endpointDescription");

        public static readonly Uri VcardIndividual = VcardPredicate("Individual");

        public static readonly Uri VcardOrganization = VcardPredicate("Organization");

        public static readonly Uri VcardFn = DcatPredicate("fn");

        public static readonly Uri VcardHasEmail = DcatPredicate("hasEmail");

        public static readonly Uri FoafPage = FoafPredicate("page");

        public static readonly Uri FoafHomePage = FoafPredicate("homepage");

        public static readonly Uri DcatApApplicableLegislation = DcatApPredicate("applicableLegislation");

        public static readonly Uri DcatApHvdCategory = DcatApPredicate("hvdCategory");

        public static readonly Uri LegTermsOfUse = LegPredicate("termsOfUse");

        public static readonly Uri LegTermsOfUseClass = LegPredicate("TermsOfUse");

        public static readonly Uri LegAuthorsWorkType = LegPredicate("authorsWorkType");

        public static readonly Uri LegOriginalDatabaseType = LegPredicate("originalDatabaseType");
        
        public static readonly Uri LegDatabaseProtectedBySpecialRightsType = LegPredicate("databaseProtectedBySpecialRightsType");
        
        public static readonly Uri LegPersonalDataContainmentType = LegPredicate("personalDataContainmentType");

        public static readonly Uri LegAuthorName = LegPredicate("authorName");

        public static readonly Uri LegOriginalDatabaseAuthorName = LegPredicate("originalDatabaseAuthorName");

        public static readonly Uri HvdDatasetLegislation = new Uri("http://data.europa.eu/eli/reg_impl/2023/138/oj");

        public static readonly Uri HvdType = new Uri("http://publications.europa.eu/resource/authority/dataset-type/HVD");

        public static readonly Uri RdfType = new Uri(RdfSpecsHelper.RdfType);

        public static readonly Uri AuthorityDeprecated = new Uri("http://publications.europa.eu/ontology/authority/deprecated");

        public static readonly Uri SkosConcept = SkosPredicate("Concept");

        public static readonly Uri SkosConceptScheme = SkosPredicate("ConceptScheme");
       
        public static readonly Uri SkosPrefLabel = SkosPredicate("prefLabel");

        public static readonly Uri SkosHasTopConcept = SkosPredicate("hasTopConcept");

        public static readonly Uri DctIdentifier = DctPredicate("identifier");

        public static readonly Uri DctPeriodOfTime = DctPredicate("PeriodOfTime");

        public static readonly Uri LocalCatalogTypeDcatDocuments = new Uri("https://data.gov.sk/def/local-catalog-type/1");

        public static readonly Uri LocalCatalogTypeSparql = new Uri("https://data.gov.sk/def/local-catalog-type/2");

        public static readonly Uri ThemeCodelist = new Uri("http://publications.europa.eu/resource/authority/data-theme");

        public static readonly Uri FrequencyCodelist = new Uri("http://publications.europa.eu/resource/authority/frequency");

        public static readonly Uri TypeCodelist = new Uri("https://data.gov.sk/set/codelist/dataset-type");

        public static readonly Uri SpatialCodelist = new Uri("https://data.gov.sk/def/ontology/location");

        public static readonly Uri EuroVocPrefix = new Uri("http://eurovoc.europa.eu/");

        public static readonly Uri PersonalDataContainmentTypeCodelist = new Uri("https://data.gov.sk/set/codelist/personal-data-occurence-type");

        public static readonly Uri LicenseCodelist = new Uri("http://publications.europa.eu/resource/authority/licence");

        public static readonly Uri FormatCodelist = new Uri("http://publications.europa.eu/resource/authority/file-type");

        public static readonly Uri MediaTypeCodelist = new Uri("http://www.iana.org/assignments/media-types");

        public static Uri DctPredicate(string term) => new Uri($"http://purl.org/dc/terms/{term}");

        public static Uri DcatPredicate(string term) => new Uri($"http://www.w3.org/ns/dcat#{term}");

        public static Uri VcardPredicate(string term) => new Uri($"http://www.w3.org/2006/vcard/ns#{term}");

        public static Uri FoafPredicate(string term) => new Uri($"http://xmlns.com/foaf/0.1/{term}");

        public static Uri DcatApPredicate(string term) => new Uri($"http://data.europa.eu/r5r/{term}");

        public static Uri LegPredicate(string term) => new Uri($"https://data.gov.sk/def/ontology/legislation/{term}");

        public static Uri SkosPredicate(string term) => new Uri($"http://www.w3.org/2008/05/skos-xl#{term}");
    }
}
