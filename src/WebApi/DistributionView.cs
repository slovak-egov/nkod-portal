using Microsoft.AspNetCore.Mvc;
using NkodSk.Abstractions;
using System.Security.Policy;

namespace WebApi
{
    public class DistributionView
    {
        public Guid Id { get; set; }

        public Guid? DatasetId { get; set; }

        public TermsOfUseView? TermsOfUse { get; set; }

        public Uri? DownloadUrl { get; set; }

        public Uri? AccessUrl { get; set; }

        public Uri? Format { get; set; }

        public CodelistItemView? FormatValue { get; set; }

        public Uri? MediaType { get; set; }

        public CodelistItemView? MediaTypeValue { get; set; }

        public Uri? ConformsTo { get; set; }

        public Uri? CompressFormat { get; set; }

        public CodelistItemView? CompressFormatValue { get; set; }

        public Uri? PackageFormat { get; set; }

        public CodelistItemView? PackageFormatValue { get; set; }

        public string? Title { get; set; }

        public Dictionary<string, string>? TitleAll { get; set; }

        public Uri? AccessService { get; set; }

        public bool IsHarvested { get; set; }

        public Uri? EndpointDescription { get; set; }
                
        public Uri? EndpointUrl { get; set; }

        public bool IsDataService { get; set; }

        public Uri? Documentation { get; set; }
        
        public Uri[] ApplicableLegislations { get; set; } = Array.Empty<Uri>();

        public bool? DownloadStatus { get; set; }

        public bool LicenseStatus { get; set; }

        public Uri? HvdCategory { get; set; }

        public CodelistItemView? HvdCategoryValue { get; set; }

        public CardView? ContactPoint { get; set; }

        private static Uri? TranslateToHttps(Uri? uri)
        {
            if (uri is not null && uri.Scheme == "http")
            {
                UriBuilder builder = new UriBuilder(uri);
                builder.Scheme = "https";
                builder.Port = -1;
                return builder.Uri;
            }
            return uri;
        }

        public static async Task<DistributionView> MapFromRdf(Guid id, Guid? datasetId, DcatDistribution distributionRdf, ICodelistProviderClient codelistProviderClient, string language, bool fetchAllLanguages, DownloadDataQualityService qualityService)
        {
            LegTermsOfUse? legTermsOfUse = distributionRdf.TermsOfUse;

            DcatDataService? dataService = distributionRdf.DataService;
            VcardKind? contactPoint = dataService?.ContactPoint;

            DistributionView view = new DistributionView
            {
                Id = id,
                DatasetId = datasetId,
                TermsOfUse = legTermsOfUse is not null ? await TermsOfUseView.MapFromRdf(legTermsOfUse, codelistProviderClient, language) : null,
                DownloadUrl = TranslateToHttps(distributionRdf.DownloadUrl),
                AccessUrl = TranslateToHttps(distributionRdf.AccessUrl),
                Format = distributionRdf.Format,
                MediaType = distributionRdf.MediaType,
                ConformsTo = distributionRdf.ConformsTo,
                CompressFormat = distributionRdf.CompressFormat,
                PackageFormat = distributionRdf.PackageFormat,
                Title = distributionRdf.GetTitle(language),
                IsHarvested = distributionRdf.IsHarvested,
                EndpointDescription = dataService?.EndpointDescription,
                EndpointUrl = dataService?.EndpointUrl,
                Documentation = dataService?.Documentation,
                ApplicableLegislations = dataService?.ApplicableLegislations.ToArray() ?? Array.Empty<Uri>(),
                HvdCategory = dataService?.HvdCategory,
                ContactPoint = contactPoint is not null ? CardView.MapFromRdf(contactPoint, language, fetchAllLanguages) : null,
                IsDataService = dataService is not null
            };

            view.LicenseStatus = legTermsOfUse is not null
                && legTermsOfUse.AuthorsWorkType is not null
                && legTermsOfUse.OriginalDatabaseType is not null
                && legTermsOfUse.DatabaseProtectedBySpecialRightsType is not null
                && legTermsOfUse.PersonalDataContainmentType is not null;

            view.DownloadStatus = distributionRdf.DownloadUrl is not null ? qualityService.IsDownloadQualityGood(distributionRdf.DownloadUrl.ToString()) : null;

            if (fetchAllLanguages)
            {
                view.TitleAll = distributionRdf.Title;
            }

            view.FormatValue = await codelistProviderClient.MapCodelistValue("http://publications.europa.eu/resource/authority/file-type", view.Format?.ToString(), language);
            view.MediaTypeValue = await codelistProviderClient.MapCodelistValue("http://www.iana.org/assignments/media-types", view.MediaType?.ToString(), language);
            view.CompressFormatValue = await codelistProviderClient.MapCodelistValue("http://www.iana.org/assignments/media-types", view.CompressFormat?.ToString(), language);
            view.PackageFormatValue = await codelistProviderClient.MapCodelistValue("http://www.iana.org/assignments/media-types", view.PackageFormat?.ToString(), language);
            view.HvdCategoryValue = await codelistProviderClient.MapCodelistValue(DcatDataset.HvdCategoryCodelist, view.HvdCategory?.ToString(), language);

            return view;
        }
    }
}
