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

        public string? AuthorName { get; set; }
        
        public string? OriginalDatabaseAuthorName { get; set; }

        public string? DownloadUrl { get; set; }

        public string? Format { get; set; }

        public string? MediaType { get; set; }

        public string? ConformsTo { get; set; }

        public string? CompressFormat { get; set; }

        public string? PackageFormat { get; set; }

        public Dictionary<string, string>? Title { get; set; }

        public Dictionary<string, string>? Description { get; set; }

        public string? EndpointUrl { get; set; }

        public List<string>? ApplicableLegislations { get; set; }

        public string? Documentation { get; set; }

        public bool IsDataService { get; set; }

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

            await results.ValidateRequiredCodelistValue(nameof(AuthorsWorkType), AuthorsWorkType, DcatDistribution.LicenseCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(OriginalDatabaseType), OriginalDatabaseType, DcatDistribution.LicenseCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(DatabaseProtectedBySpecialRightsType), DatabaseProtectedBySpecialRightsType, DcatDistribution.LicenseCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(PersonalDataContainmentType), PersonalDataContainmentType, DcatDistribution.PersonalDataContainmentTypeCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(Format), Format, DcatDistribution.FormatCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(MediaType), MediaType, DcatDistribution.MediaTypeCodelist, codelistProvider);
            results.ValidateUrl(nameof(ConformsTo), ConformsTo, false);
            await results.ValidateCodelistValue(nameof(CompressFormat), CompressFormat, DcatDistribution.MediaTypeCodelist, codelistProvider);
            await results.ValidateCodelistValue(nameof(PackageFormat), PackageFormat, DcatDistribution.MediaTypeCodelist, codelistProvider);

            List<string> languages = Title?.Keys.ToList() ?? new List<string>();
            if (!languages.Contains("sk"))
            {
                languages.Add("sk");
            }

            results.ValidateLanguageTexts(nameof(Title), Title, languages, IsDataService);

            if (IsDataService)
            {
                results.ValidateLanguageTexts(nameof(Description), Description, languages, false);
                results.ValidateUrl(nameof(EndpointUrl), EndpointUrl, true);
                results.ValidateUrl(nameof(Documentation), Documentation, false);

                if (ApplicableLegislations is not null)
                {
                    foreach (string url in ApplicableLegislations)
                    {
                        if (url is null || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || !uri.LocalPath.Contains("/eli/"))
                        {
                            results.Add(nameof(ApplicableLegislations), "URL musí byť platné a obsahovať /eli/");
                            break;
                        }
                    }
                }
            }
            else
            {
                results.ValidateUrl(nameof(DownloadUrl), DownloadUrl, true);
            }

            if (PersonalDataContainmentType == "https://data.gov.sk/def/personal-data-occurence-type/3")
            {
                results.Add("personaldatacontainmenttype", "Typ výskytu osobných údajov nemôže ostať nešpecifikovaný");
            }

            return results;
        }

        public void MapToRdf(DcatDistribution distribution)
        {
            string? authorName = !string.IsNullOrWhiteSpace(AuthorName) ? AuthorName : null;
            string? originalDatabaseAuthorName = !string.IsNullOrWhiteSpace(OriginalDatabaseAuthorName) ? OriginalDatabaseAuthorName : null;

            distribution.SetTermsOfUse(AuthorsWorkType.AsUri(), OriginalDatabaseType.AsUri(), DatabaseProtectedBySpecialRightsType.AsUri(), PersonalDataContainmentType.AsUri(), authorName, originalDatabaseAuthorName);
            distribution.Format = Format.AsUri();
            distribution.MediaType = MediaType.AsUri();
            distribution.ConformsTo = ConformsTo.AsUri();
            distribution.CompressFormat = CompressFormat.AsUri();
            distribution.PackageFormat = PackageFormat.AsUri();

            if (IsDataService)
            {
                distribution.AccessUrl = EndpointUrl.AsUri();
                DcatDataService dataService = distribution.GetOrCreateDataSerice();
                dataService.EndpointUrl = EndpointUrl.AsUri();
                dataService.Documentation = Documentation.AsUri();
                dataService.ConformsTo = ConformsTo.AsUri();
                dataService.SetTitle(Title ?? new Dictionary<string, string>());
                dataService.SetDescription(Description ?? new Dictionary<string, string>());

                List<Uri> applicableLegislations = new List<Uri>();
                if (ApplicableLegislations is not null)
                {
                    foreach (string applicableLegislation in ApplicableLegislations)
                    {
                        if (applicableLegislation.AsUri() is Uri uri)
                        {
                            applicableLegislations.Add(uri);
                        }
                    }
                }
                dataService.ApplicableLegislations = applicableLegislations;
                distribution.DownloadUrl = null;
            }
            else
            {
                distribution.DownloadUrl = DownloadUrl.AsUri();
                distribution.AccessUrl = DownloadUrl.AsUri();
                distribution.AccessService = null;
            }

            distribution.SetTitle(Title);
        }
    }
}
