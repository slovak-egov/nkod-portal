using NkodSk.Abstractions;

namespace WebApi
{
    public class TermsOfUseView
    {
        public Uri? AuthorsWorkType { get; set; }

        public CodelistItemView? AuthorsWorkTypeValue { get; set; }

        public Uri? OriginalDatabaseType { get; set; }

        public CodelistItemView? OriginalDatabaseTypeValue { get; set; }

        public Uri? DatabaseProtectedBySpecialRightsType { get; set; }

        public CodelistItemView? DatabaseProtectedBySpecialRightsTypeValue { get; set; }

        public Uri? PersonalDataContainmentType { get; set; }

        public CodelistItemView? PersonalDataContainmentTypeValue { get; set; }

        public string? AuthorName { get; set; }

        public string? OriginalDatabaseAuthorName { get; set; }

        public static async Task<TermsOfUseView> MapFromRdf(LegTermsOfUse rdf, ICodelistProviderClient codelistProviderClient, string language)
        {
            TermsOfUseView view = new TermsOfUseView
            {
                AuthorsWorkType = rdf.AuthorsWorkType,
                OriginalDatabaseType = rdf.OriginalDatabaseType,
                DatabaseProtectedBySpecialRightsType = rdf.DatabaseProtectedBySpecialRightsType,
                PersonalDataContainmentType = rdf.PersonalDataContainmentType,
                AuthorName = rdf.AuthorName,
                OriginalDatabaseAuthorName = rdf.OriginalDatabaseAuthorName,
            };

            view.AuthorsWorkTypeValue = await codelistProviderClient.MapCodelistValue(DcatDistribution.LicenseCodelist, view.AuthorsWorkType?.ToString(), language);
            view.OriginalDatabaseTypeValue = await codelistProviderClient.MapCodelistValue(DcatDistribution.LicenseCodelist, view.OriginalDatabaseType?.ToString(), language);
            view.DatabaseProtectedBySpecialRightsTypeValue = await codelistProviderClient.MapCodelistValue(DcatDistribution.LicenseCodelist, view.DatabaseProtectedBySpecialRightsType?.ToString(), language);
            view.PersonalDataContainmentTypeValue = await codelistProviderClient.MapCodelistValue(DcatDistribution.PersonalDataContainmentTypeCodelist, view.PersonalDataContainmentType?.ToString(), language);

            return view;
        }
    }
}
