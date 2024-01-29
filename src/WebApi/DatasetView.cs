using CodelistProviderClient;
using NkodSk.Abstractions;
using System.Globalization;

namespace WebApi
{
    public class DatasetView
    {
        public Guid Id { get; set; }

        public string? Key { get; set; }

        public bool IsPublic { get; set; }

        public string? Name { get; set; }

        public Dictionary<string, string>? NameAll { get; set; }

        public string? Description { get; set; }

        public Dictionary<string, string>? DescriptionAll { get; set; }

        public string? PublisherId { get; set; }

        public PublisherView? Publisher { get; set; }

        public Uri[] Themes { get; set; } = Array.Empty<Uri>();

        public CodelistItemView[] ThemeValues { get; set; } = Array.Empty<CodelistItemView>();

        public Uri? AccrualPeriodicity { get; set; }

        public CodelistItemView? AccrualPeriodicityValue { get; set; }

        public string[] Keywords { get; set; } = Array.Empty<string>();

        public Dictionary<string, List<string>>? KeywordsAll { get; set; }

        public Uri[] Type { get; set; } = Array.Empty<Uri>();

        public CodelistItemView[] TypeValues { get; set; } = Array.Empty<CodelistItemView>();

        public Uri[] Spatial { get; set; } = Array.Empty<Uri>();

        public CodelistItemView[] SpatialValues { get; set; } = Array.Empty<CodelistItemView>();

        public TemporalView? Temporal { get; set; }

        public CardView? ContactPoint { get; set; }
                
        public Uri? LandingPage { get; set; }

        public Uri? Specification { get; set; }

        public Uri[] EuroVocThemes { get; set; } = Array.Empty<Uri>();

        public string[] EuroVocThemeValues { get; set; } = Array.Empty<string>();

        public decimal? SpatialResolutionInMeters { get; set; }

        public string? TemporalResolution { get; set; }

        public string? IsPartOf { get; set; }

        public bool IsSerie { get; set; }

        public List<DistributionView> Distributions { get; } = new List<DistributionView>();

        public static async Task<DatasetView> MapFromRdf(FileMetadata metadata, DcatDataset datasetRdf, ICodelistProviderClient codelistProviderClient, string language, bool fetchAllLanguages)
        {
            VcardKind? contactPoint = datasetRdf.ContactPoint;
            DctTemporal? temporal = datasetRdf.Temporal;

            DatasetView view = new DatasetView
            {
                Id = metadata.Id,
                Key = datasetRdf.Uri.ToString(),
                IsPublic = metadata.IsPublic,
                Name = datasetRdf.GetTitle(language),
                Description = datasetRdf.GetDescription(language),
                PublisherId = metadata.Publisher,
                Themes = datasetRdf.NonEuroVocThemes.ToArray(),
                AccrualPeriodicity = datasetRdf.AccrualPeriodicity,
                Keywords = datasetRdf.GetKeywords(language).ToArray(),
                KeywordsAll = datasetRdf.Keywords,
                Type = datasetRdf.Type.ToArray(),
                Spatial = datasetRdf.Spatial.ToArray(),
                Temporal = temporal is not null ? new TemporalView { StartDate = temporal.StartDate?.ToString(CultureInfo.CurrentCulture), EndDate = temporal.EndDate?.ToString(CultureInfo.CurrentCulture) } : null,
                ContactPoint = contactPoint is not null ? CardView.MapFromRdf(contactPoint, language, fetchAllLanguages) : null,
                LandingPage = datasetRdf.LandingPage,
                Specification = datasetRdf.Specification,
                EuroVocThemes = datasetRdf.EuroVocThemes.ToArray(),
                SpatialResolutionInMeters = datasetRdf.SpatialResolutionInMeters,
                TemporalResolution = datasetRdf.TemporalResolution,
                IsPartOf = datasetRdf.IsPartOfInternalId,
                IsSerie = datasetRdf.IsSerie
            };

            if (fetchAllLanguages)
            {
                view.NameAll = datasetRdf.Title;
                view.DescriptionAll = datasetRdf.Description;
            }

            view.ThemeValues = await codelistProviderClient.MapCodelistValues(DcatDataset.ThemeCodelist, view.Themes.Select(u => u.ToString()), language);
            view.AccrualPeriodicityValue = await codelistProviderClient.MapCodelistValue(DcatDataset.AccrualPeriodicityCodelist, view.AccrualPeriodicity?.ToString(), language);
            view.TypeValues = await codelistProviderClient.MapCodelistValues(DcatDataset.TypeCodelist, view.Type.Select(u => u.ToString()), language);
            view.SpatialValues = await codelistProviderClient.MapCodelistValues(DcatDataset.SpatialCodelist, view.Spatial.Select(u => u.ToString()), language);
            view.EuroVocThemeValues = datasetRdf.GetEuroVocLabelThemes(language).ToArray();

            return view;
        }
    }
}
