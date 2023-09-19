using NkodSk.Abstractions;
using System.Xml.Linq;

namespace WebApi
{
    public class DistributionInput
    {
        public string? Id { get; set; }

        public string? DatasetId { get; set; }

        public string? AuthorsWorkType { get; set; }

        public string? OriginalDatabaseType { get; set; }

        public string? DatabaseProtectedBySpecialRightsType { get; set; }

        public string? PersonalDataContainmentType { get; set; }

        public string? DownloadUrl { get; set; }

        public string? AccessUrl { get; set; }

        public string? Format { get; set; }

        public string? MediaType { get; set; }

        public string? ConformsTo { get; set; }

        public string? CompressFormat { get; set; }

        public string? PackageFormat { get; set; }

        public Dictionary<string, string>? Title { get; set; }

        public string? FileId { get; set; }

        public async Task<ValidationResults> Validate(string publisher, IDocumentStorageClient documentStorage, ICodelistProviderClient codelistProvider)
        {
            ValidationResults results = new ValidationResults();

            await results.ValidateRequiredCodelistValue(nameof(AuthorsWorkType), AuthorsWorkType, "https://data.gov.sk/def/ontology/law/authorsWorkType", codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(OriginalDatabaseType), OriginalDatabaseType, "https://data.gov.sk/def/ontology/law/originalDatabaseType", codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(DatabaseProtectedBySpecialRightsType), DatabaseProtectedBySpecialRightsType, "https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType", codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(PersonalDataContainmentType), PersonalDataContainmentType, "https://data.gov.sk/def/ontology/law/personalDataContainmentType", codelistProvider);
            results.ValidateUrl(nameof(DownloadUrl), DownloadUrl, true);
            results.ValidateUrl(nameof(AccessUrl), AccessUrl, true);
            await results.ValidateRequiredCodelistValue(nameof(Format), Format, "http://publications.europa.eu/resource/dataset/file-type", codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(MediaType), MediaType, "http://www.iana.org/assignments/media-types", codelistProvider);
            results.ValidateUrl(nameof(ConformsTo), ConformsTo, false);
            await results.ValidateCodelistValue(nameof(CompressFormat), CompressFormat, "http://www.iana.org/assignments/media-types", codelistProvider);
            await results.ValidateCodelistValue(nameof(PackageFormat), PackageFormat, "http://www.iana.org/assignments/media-types", codelistProvider);
            results.ValidateLanguageTexts(nameof(Title), Title, null, false);

            return results;
        }

        public void MapToRdf(DcatDistribution distribution)
        {
            distribution.SetTermsOfUse(AuthorsWorkType.AsUri(), OriginalDatabaseType.AsUri(), DatabaseProtectedBySpecialRightsType.AsUri(), PersonalDataContainmentType.AsUri());
            distribution.DownloadUrl = DownloadUrl.AsUri();
            distribution.AccessUrl = AccessUrl.AsUri();
            distribution.Format = Format.AsUri();
            distribution.MediaType = MediaType.AsUri();
            distribution.ConformsTo = ConformsTo.AsUri();
            distribution.CompressFormat = CompressFormat.AsUri();
            distribution.PackageFormat = PackageFormat.AsUri();
            distribution.SetTitle(Title);
        }
    }
}
