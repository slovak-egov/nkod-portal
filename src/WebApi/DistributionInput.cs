﻿using NkodSk.Abstractions;
using System.Linq;
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

        public string? HvdCategory { get; set; }

        public string? CompressFormat { get; set; }

        public string? PackageFormat { get; set; }

        public Dictionary<string, string>? Title { get; set; }

        public string? EndpointUrl { get; set; }

        public List<string>? ApplicableLegislations { get; set; }

        public string? Documentation { get; set; }

        public bool IsDataService { get; set; }

        public string? FileId { get; set; }

        public Dictionary<string, string>? ContactName { get; set; }

        public string? ContactEmail { get; set; }

        public string? EndpointDescription { get; set; }

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
            await results.ValidateRequiredCodelistValue(nameof(MediaType), MediaType, DcatDistribution.MediaTypeCodelist, codelistProvider);
            await results.ValidateCodelistValue(nameof(HvdCategory), HvdCategory, DcatDataset.HvdCategoryCodelist, codelistProvider);
            results.ValidateUrl(nameof(ConformsTo), ConformsTo, false);
            await results.ValidateCodelistValue(nameof(CompressFormat), CompressFormat, DcatDistribution.MediaTypeCodelist, codelistProvider);
            await results.ValidateCodelistValue(nameof(PackageFormat), PackageFormat, DcatDistribution.MediaTypeCodelist, codelistProvider);
            results.ValidateApplicableLegislations(nameof(ApplicableLegislations), ApplicableLegislations);

            List<string> languages = Title?.Keys.ToList() ?? new List<string>();
            if (!languages.Contains("sk"))
            {
                languages.Add("sk");
            }

            results.ValidateLanguageTexts(nameof(Title), Title, languages, IsDataService);

            bool isDatasetHvd = dataset is not null && dataset.IsHvd;

            if (IsDataService)
            {
                languages = ContactName?.Keys.ToList() ?? new List<string>();
                if (!languages.Contains("sk"))
                {
                    languages.Add("sk");
                }

                results.ValidateLanguageTexts(nameof(ContactName), ContactName, languages, isDatasetHvd);
                results.ValidateEmail(nameof(ContactEmail), ContactEmail, isDatasetHvd);

                results.ValidateUrl(nameof(EndpointUrl), EndpointUrl, true);
                results.ValidateUrl(nameof(Documentation), Documentation, isDatasetHvd);
                results.ValidateUrl(nameof(EndpointDescription), EndpointDescription, false);
            }
            else
            {
                results.ValidateUrl(nameof(DownloadUrl), DownloadUrl, true);
                await results.ValidateRequiredCodelistValue(nameof(Format), Format, DcatDistribution.FormatCodelist, codelistProvider);
            }

            if (PersonalDataContainmentType == "https://data.gov.sk/def/personal-data-occurence-type/3")
            {
                results.Add("personaldatacontainmenttype", "Typ výskytu osobných údajov nemôže ostať nešpecifikovaný");
            }

            return results;
        }

        public void MapToRdf(DcatDistribution distribution, DcatDataset dataset)
        {
            string? authorName = !string.IsNullOrWhiteSpace(AuthorName) ? AuthorName : null;
            string? originalDatabaseAuthorName = !string.IsNullOrWhiteSpace(OriginalDatabaseAuthorName) ? OriginalDatabaseAuthorName : null;

            distribution.SetTermsOfUse(AuthorsWorkType.AsUri(), OriginalDatabaseType.AsUri(), DatabaseProtectedBySpecialRightsType.AsUri(), PersonalDataContainmentType.AsUri(), authorName, originalDatabaseAuthorName);
            distribution.Format = Format.AsUri();
            distribution.MediaType = MediaType.AsUri();
            distribution.ConformsTo = ConformsTo.AsUri();
            distribution.CompressFormat = CompressFormat.AsUri();
            distribution.PackageFormat = PackageFormat.AsUri();

            bool isDatasetHvd = dataset is not null && dataset.IsHvd;

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

            if (isDatasetHvd)
            {
                Uri hvdLegislation = new Uri(DcatDataset.HvdLegislation);
                if (!applicableLegislations.Any(u => string.Equals(u.OriginalString, StringComparer.OrdinalIgnoreCase)))
                {
                    applicableLegislations.Add(hvdLegislation);
                }
            }
            else
            {
                applicableLegislations.RemoveAll(u => string.Equals(u.OriginalString, DcatDataset.HvdLegislation, StringComparison.OrdinalIgnoreCase));
            }

            distribution.ApplicableLegislations = applicableLegislations;

            if (IsDataService)
            {
                distribution.AccessUrl = EndpointUrl.AsUri();
                DcatDataService dataService = distribution.GetOrCreateDataSerice();
                dataService.EndpointUrl = EndpointUrl.AsUri();
                dataService.Documentation = Documentation.AsUri();
                dataService.ConformsTo = ConformsTo.AsUri();
                dataService.SetTitle(Title ?? new Dictionary<string, string>());
                dataService.EndpointDescription = EndpointDescription.AsUri();
                dataService.HvdCategory = HvdCategory.AsUri();
                dataService.SetContactPoint(ContactName is not null ? new LanguageDependedTexts(ContactName) : null, ContactEmail);

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
