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

        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public Dictionary<string, string>? ContactName { get; set; }

        public string? ContactEmail { get; set; }

        public string? Documentation { get; set; }

        public string? Specification { get; set; }

        public List<string>? EuroVocThemes { get; set; }

        public string? SpatialResolutionInMeters { get; set; }

        public string? TemporalResolution { get; set; }

        public string? IsPartOf { get; set; }

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
            await results.ValidateCodelistValues(nameof(Spatial), Spatial, DcatDataset.SpatialCodelist, codelistProvider);
            results.ValidateDate(nameof(StartDate), StartDate);
            results.ValidateDate(nameof(EndDate), EndDate);
            results.ValidateLanguageTexts(nameof(ContactName), ContactName, languages, false);
            results.ValidateEmail(nameof(ContactEmail), ContactEmail);
            results.ValidateUrl(nameof(Documentation), Documentation, false);
            results.ValidateUrl(nameof(Specification), Specification, false);
            await results.ValidateCodelistValues(nameof(EuroVocThemes), EuroVocThemes, DcatDataset.EuroVocThemeCodelist, codelistProvider);
            results.ValidateNumber(nameof(SpatialResolutionInMeters), SpatialResolutionInMeters);
            results.ValidateTemporalResolution(nameof(TemporalResolution), TemporalResolution);
            await results.ValidateDataset(nameof(IsPartOf), IsPartOf, publisher, documentStorage);

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
                StartDate is not null ? DateOnly.Parse(StartDate, System.Globalization.CultureInfo.CurrentCulture) : null,
                EndDate is not null ? DateOnly.Parse(EndDate, System.Globalization.CultureInfo.CurrentCulture) : null);
            dataset.SetContactPoint(
                ContactName is not null ? new LanguageDependedTexts(ContactName) : null,
                ContactEmail);
            dataset.Documentation = Documentation is not null ? new Uri(Documentation) : null;
            dataset.Specification = Specification is not null ? new Uri(Specification) : null;
            dataset.SpatialResolutionInMeters = SpatialResolutionInMeters is not null ? decimal.Parse(SpatialResolutionInMeters, System.Globalization.CultureInfo.CurrentCulture) : null;
            dataset.TemporalResolution = TemporalResolution;
            dataset.IsPartOf = IsPartOf is not null ? new Uri(IsPartOf) : null;
        }
    }
}
