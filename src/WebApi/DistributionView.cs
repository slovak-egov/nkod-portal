using NkodSk.Abstractions;

namespace WebApi
{
    public class DistributionView
    {
        public Guid Id { get; set; }

        public TermsOfUseView? TermsOfUse { get; set; }

        public Uri? DownloadUrl { get; set; }

        public Uri? AccessUrl { get; set; }

        public Uri? Format { get; set; }

        public CodelistItemView? FormatValue { get; set; }

        public Uri? MediaType { get; set; }

        public Uri? ConformsTo { get; set; }

        public Uri? CompressFormat { get; set; }

        public Uri? PackageFormat { get; set; }

        public string? Title { get; set; }

        public static async Task<DistributionView> MapFromRdf(Guid id, DcatDistribution distributionRdf, CodelistProviderClient.CodelistProviderClient codelistProviderClient, string language)
        {
            LegTermsOfUse? legTermsOfUse = distributionRdf.TermsOfUse;

            DistributionView view = new DistributionView
            {
                Id = id,
                TermsOfUse = legTermsOfUse is not null ? await TermsOfUseView.MapFromRdf(legTermsOfUse, codelistProviderClient, language) : null,
                DownloadUrl = distributionRdf.DownloadUrl,
                AccessUrl = distributionRdf.AccessUrl,
                Format = distributionRdf.Format,
                MediaType = distributionRdf.MediaType,
                ConformsTo = distributionRdf.ConformsTo,
                CompressFormat = distributionRdf.CompressFormat,
                PackageFormat = distributionRdf.PackageFormat,
                Title = distributionRdf.GetTitle(language)
            };

            view.FormatValue = await codelistProviderClient.MapCodelistValue("http://publications.europa.eu/resource/authority/file-type", view.Format?.ToString(), language);

            return view;
        }
    }
}
