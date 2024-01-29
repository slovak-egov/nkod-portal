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

            DcatDataset? dataset = null;

            if (Guid.TryParse(DatasetId, out Guid datasetId))
            {
                FileState? datasetFileState = await documentStorage.GetFileState(datasetId);
                if (datasetFileState?.Content is not null)
                {
                    dataset = DcatDataset.Parse(datasetFileState.Content);
                }
            }

            if (dataset is null)
            {
                results["dataset"] = "Dataset nebol nájdený";
            } 
            else if (dataset.IsSerie)
            {
                results["dataset"] = "Dátová séria nemôže mať distribúcie";
            }

            await results.ValidateRequiredCodelistValue(nameof(AuthorsWorkType), AuthorsWorkType, DcatDistribution.AuthorsWorkTypeCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(OriginalDatabaseType), OriginalDatabaseType, DcatDistribution.OriginalDatabaseTypeCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(DatabaseProtectedBySpecialRightsType), DatabaseProtectedBySpecialRightsType, DcatDistribution.DatabaseProtectedBySpecialRightsTypeCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(PersonalDataContainmentType), PersonalDataContainmentType, DcatDistribution.PersonalDataContainmentTypeCodelist, codelistProvider);
            results.ValidateUrl(nameof(DownloadUrl), DownloadUrl, true);
            await results.ValidateRequiredCodelistValue(nameof(Format), Format, DcatDistribution.FormatCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(MediaType), MediaType, DcatDistribution.MediaTypeCodelist, codelistProvider);
            results.ValidateUrl(nameof(ConformsTo), ConformsTo, false);
            await results.ValidateCodelistValue(nameof(CompressFormat), CompressFormat, DcatDistribution.MediaTypeCodelist, codelistProvider);
            await results.ValidateCodelistValue(nameof(PackageFormat), PackageFormat, DcatDistribution.MediaTypeCodelist, codelistProvider);
            results.ValidateLanguageTexts(nameof(Title), Title, null, false);

            return results;
        }

        public void MapToRdf(DcatDistribution distribution)
        {
            distribution.SetTermsOfUse(AuthorsWorkType.AsUri(), OriginalDatabaseType.AsUri(), DatabaseProtectedBySpecialRightsType.AsUri(), PersonalDataContainmentType.AsUri());
            distribution.DownloadUrl = DownloadUrl.AsUri();
            distribution.AccessUrl = DownloadUrl.AsUri();
            distribution.Format = Format.AsUri();
            distribution.MediaType = MediaType.AsUri();
            distribution.ConformsTo = ConformsTo.AsUri();
            distribution.CompressFormat = CompressFormat.AsUri();
            distribution.PackageFormat = PackageFormat.AsUri();
            distribution.SetTitle(Title);
        }
    }
}
