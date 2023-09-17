using CodelistProviderClient;
using NkodSk.Abstractions;

namespace WebApi
{
    public class DatasetView
    {
        public Guid Id { get; set; }

        public bool IsPublic { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? PublisherId { get; set; }

        public PublisherView? Publisher { get; set; }

        public Uri[] Themes { get; set; } = Array.Empty<Uri>();

        public CodelistItemView[] ThemeValues { get; set; } = Array.Empty<CodelistItemView>();

        public Uri? AccrualPeriodicity { get; set; }

        public CodelistItemView? AccrualPeriodicityValue { get; set; }

        public string[] Keywords { get; set; } = Array.Empty<string>();

        public CodelistItemView[] KeywordValues { get; set; } = Array.Empty<CodelistItemView>();

        public Uri? Type { get; set; }

        public CodelistItemView? TypeValue { get; set; }

        public Uri[] Spatial { get; set; } = Array.Empty<Uri>();

        public CodelistItemView[] SpatialValues { get; set; } = Array.Empty<CodelistItemView>();

        public TemporalView? Temporal { get; set; }

        public CardView? ContactPoint { get; set; }

        public Uri? Documentation { get; set; }

        public Uri? Specification { get; set; }

        public Uri[] EuroVocThemes { get; set; } = Array.Empty<Uri>();

        public CodelistItemView[] EuroVocThemeValues { get; set; } = Array.Empty<CodelistItemView>();

        public decimal? SpatialResolutionInMeters { get; set; }

        public string? TemporalResolution { get; set; }

        public Uri? IsPartOf { get; set; }

        public List<DistributionView> Distributions { get; } = new List<DistributionView>();

        public static async Task<DatasetView> MapFromRdf(FileMetadata metadata, DcatDataset datasetRdf, ICodelistProviderClient codelistProviderClient, string language)
        {
            VcardKind? contactPoint = datasetRdf.ContactPoint;
            DctTemporal? temporal = datasetRdf.Temporal;

            List<Uri> eurovocThemes = new List<Uri>();
            List<Uri> nonEurovocThemes = new List<Uri>();
            foreach (Uri theme in datasetRdf.Themes)
            {
                if (theme.ToString().StartsWith("http://publications.europa.eu/resource/dataset/eurovoc/"))
                {
                    eurovocThemes.Add(theme);
                }
                else
                {
                    nonEurovocThemes.Add(theme);
                }
            }

            DatasetView view = new DatasetView
            {
                Id = metadata.Id,
                IsPublic = metadata.IsPublic,
                Name = datasetRdf.GetTitle(language),
                Description = datasetRdf.GetDescription(language),
                PublisherId = metadata.Publisher,
                Themes = nonEurovocThemes.ToArray(),
                AccrualPeriodicity = datasetRdf.AccrualPeriodicity,
                Keywords = datasetRdf.GetKeywords(language).ToArray(),
                Type = datasetRdf.Type,
                Spatial = datasetRdf.Spatial.ToArray(),
                Temporal = temporal is not null ? new TemporalView { StartDate = temporal.StartDate, EndDate = temporal.EndDate } : null,
                ContactPoint = contactPoint is not null ? new CardView { Name = contactPoint.GetName(language), Email = contactPoint.Email } : null,
                Documentation = datasetRdf.Documentation,
                Specification = datasetRdf.Specification,
                EuroVocThemes = eurovocThemes.ToArray(),
                SpatialResolutionInMeters = datasetRdf.SpatialResolutionInMeters,
                TemporalResolution = datasetRdf.TemporalResolution,
                IsPartOf = datasetRdf.IsPartOf
            };

            view.ThemeValues = await codelistProviderClient.MapCodelistValues("http://publications.europa.eu/resource/dataset/data-theme", view.Themes.Select(u => u.ToString()), language);
            view.AccrualPeriodicityValue = await codelistProviderClient.MapCodelistValue("http://publications.europa.eu/resource/dataset/frequency", view.AccrualPeriodicity?.ToString(), language);
            view.TypeValue = await codelistProviderClient.MapCodelistValue("https://data.gov.sk/set/codelist/dataset-type", view.Type?.ToString(), language);
            view.SpatialValues = await codelistProviderClient.MapCodelistValues("http://publications.europa.eu/resource/dataset/country", view.Spatial.Select(u => u.ToString()), language);
            view.EuroVocThemeValues = await codelistProviderClient.MapCodelistValues("http://publications.europa.eu/resource/dataset/eurovoc", view.EuroVocThemes.Select(u => u.ToString()), language);

            return view;
        }
    }
}
