using Microsoft.IdentityModel.Tokens;
using NkodSk.Abstractions;

namespace WebApi
{
    public class DatasetInput
    {
        public string? Id { get; set; }

        public bool IsPublic { get; set; }

        public Dictionary<string, string>? Name { get; set; }

        public Dictionary<string, string>? Description { get; set; }

        public List<string>? Themes { get; set; }

        public string? AccrualPeriodicity { get; set; }

        public Dictionary<string, List<string>>? Keywords { get; set; }

        public List<string>? Type { get; set; }

        public List<string>? Spatial { get; set; }

        public string? HvdCategory { get; set; }

        public List<string>? ApplicableLegislations { get; set; }

        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public Dictionary<string, string>? ContactName { get; set; }

        public string? ContactEmail { get; set; }

        public string? LandingPage { get; set; }

        public string? Specification { get; set; }

        public List<string>? EuroVocThemes { get; set; }

        public string? SpatialResolutionInMeters { get; set; }

        public string? TemporalResolution { get; set; }

        public string? IsPartOf { get; set; }

        public bool IsSerie { get; set; }

        private Dictionary<string, List<string>>? loadedEuroVocLabels = new Dictionary<string, List<string>>();

        public async Task<ValidationResults> Validate(string publisher, IDocumentStorageClient documentStorage, ICodelistProviderClient codelistProvider)
        {
            ValidationResults results = new ValidationResults();

            List<string> languages = Name?.Keys.ToList() ?? new List<string>();
            if (!languages.Contains("sk"))
            {
                languages.Add("sk");
            }

            results.ValidateLanguageTexts(nameof(Name), Name, languages, true);
            results.ValidateLanguageTexts(nameof(Description), Description, languages, true);
            await results.ValidateRequiredCodelistValues(nameof(Themes), Themes, DcatDataset.ThemeCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValue(nameof(AccrualPeriodicity), AccrualPeriodicity, DcatDataset.AccrualPeriodicityCodelist, codelistProvider);
            results.ValidateKeywords(nameof(Keywords), Keywords, languages);
            await results.ValidateCodelistValues(nameof(Type), Type, DcatDataset.TypeCodelist, codelistProvider);
            await results.ValidateRequiredCodelistValues(nameof(Spatial), Spatial, DcatDataset.SpatialCodelist, codelistProvider);
            results.ValidateDate(nameof(StartDate), StartDate);
            results.ValidateDate(nameof(EndDate), EndDate);
            results.ValidateLanguageTexts(nameof(ContactName), ContactName, languages, false);
            results.ValidateEmail(nameof(ContactEmail), ContactEmail, false);
            results.ValidateUrl(nameof(LandingPage), LandingPage, false);
            results.ValidateUrl(nameof(Specification), Specification, false);
            results.ValidateApplicableLegislations(nameof(ApplicableLegislations), ApplicableLegislations);
            
            if (Type is not null && Type.Any(t => string.Equals(t.ToString(), DcatDataset.HvdType, StringComparison.OrdinalIgnoreCase)))
            {
                await results.ValidateRequiredCodelistValue(nameof(HvdCategory), HvdCategory, DcatDataset.HvdCategoryCodelist, codelistProvider);
            }
            else
            {
                await results.ValidateCodelistValue(nameof(HvdCategory), HvdCategory, DcatDataset.HvdCategoryCodelist, codelistProvider);
            }

            loadedEuroVocLabels ??= new Dictionary<string, List<string>>();
            loadedEuroVocLabels.Clear();
            await results.ValidateEuroVocValues(nameof(EuroVocThemes), EuroVocThemes, loadedEuroVocLabels);
            
            results.ValidateNumber(nameof(SpatialResolutionInMeters), SpatialResolutionInMeters);
            results.ValidateTemporalResolution(nameof(TemporalResolution), TemporalResolution);
            await results.ValidateDataset(nameof(IsPartOf), IsPartOf, Id, publisher, documentStorage);

            bool datasetGuidIsValid = Guid.TryParse(Id, out Guid datasetId);

            if (datasetGuidIsValid && !IsSerie)
            {
                if (await documentStorage.GetDatasetParts(datasetId).ConfigureAwait(false) is { Count: > 0 })
                {
                    results.AddError(nameof(IsSerie), "Dataset je séria");
                }
            }

            if (!string.IsNullOrEmpty(LandingPage))
            {
                FileStorageQuery query = new FileStorageQuery
                {
                    OnlyTypes = new List<FileType> { FileType.DatasetRegistration },
                    AdditionalFilters = new Dictionary<string, string[]>
                    {
                        { "landingPage", new[]{ LandingPage } }
                    }
                };
                FileStorageResponse response = await documentStorage.GetFileStates(query).ConfigureAwait(false);
                foreach (FileState state in response.Files)
                {
                    if (state.Metadata.Id != datasetId)
                    {
                        results.AddError(nameof(LandingPage), "Nastavenú domovskú stránku používa iný dataset");
                        break;
                    }
                }
            }

            return results;
        }

        public void MapToRdf(Uri publisher, DcatDataset dataset)
        {
            dataset.SetTitle(Name ?? new Dictionary<string, string>());
            dataset.SetDescription(Description ?? new Dictionary<string, string>());
            dataset.Publisher = publisher;
            dataset.SetKeywords(Keywords ?? new Dictionary<string, List<string>>());
            dataset.AccrualPeriodicity = AccrualPeriodicity is not null ? new Uri(AccrualPeriodicity) : null;
            dataset.ShouldBePublic = IsPublic;
            dataset.Themes = (Themes ?? new List<string>()).Union(EuroVocThemes ?? new List<string>()).Select(t => new Uri(t, UriKind.Absolute));
            dataset.Type = (Type ?? new List<string>()).Select(s => new Uri(s, UriKind.Absolute));
            dataset.Spatial = (Spatial ?? new List<string>()).Select(s => new Uri(s, UriKind.Absolute));
            dataset.SetTemporal(
                !string.IsNullOrEmpty(StartDate) ? DateOnly.Parse(StartDate, System.Globalization.CultureInfo.CurrentCulture) : null,
                !string.IsNullOrEmpty(EndDate) ? DateOnly.Parse(EndDate, System.Globalization.CultureInfo.CurrentCulture) : null);
            dataset.SetContactPoint(
                ContactName is not null ? new LanguageDependedTexts(ContactName) : null,
                ContactEmail);
            dataset.LandingPage = LandingPage.AsUri();
            dataset.Specification = Specification.AsUri();
            dataset.SpatialResolutionInMeters = SpatialResolutionInMeters is not null ? decimal.Parse(SpatialResolutionInMeters, System.Globalization.CultureInfo.CurrentCulture) : null;
            dataset.TemporalResolution = TemporalResolution;
            dataset.IsSerie = IsSerie;
            dataset.IsPartOfInternalId = IsPartOf;

            loadedEuroVocLabels ??= new Dictionary<string, List<string>>();
            dataset.SetEuroVocLabelThemes(loadedEuroVocLabels);

            bool isHvd = Type is not null && Type.Any(t => string.Equals(t.ToString(), DcatDataset.HvdType, StringComparison.OrdinalIgnoreCase));

            dataset.HvdCategory = HvdCategory.AsUri();

            List<string> applicableLegislations = ApplicableLegislations ?? new List<string>();

            if (isHvd)
            {
                if (!applicableLegislations.Contains(DcatDataset.HvdLegislation, StringComparer.OrdinalIgnoreCase))
                {
                    applicableLegislations.Add(DcatDataset.HvdLegislation);
                }
            }
            else
            {
                applicableLegislations.RemoveAll(s => string.Equals(s, DcatDataset.HvdLegislation, StringComparison.OrdinalIgnoreCase));
            }

            dataset.ApplicableLegislations = applicableLegislations.Select(s => new Uri(s));

            dataset.SetRootTypes(new Uri(RdfDocument.DcatPrefix + (IsSerie ? "DatasetSeries" : "Dataset")));
        }
    }
}
