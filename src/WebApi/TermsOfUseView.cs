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

        public static async Task<TermsOfUseView> MapFromRdf(LegTermsOfUse rdf, ICodelistProviderClient codelistProviderClient, string language)
        {
            TermsOfUseView view = new TermsOfUseView
            {
                AuthorsWorkType = rdf.AuthorsWorkType,
                OriginalDatabaseType = rdf.OriginalDatabaseType,
                DatabaseProtectedBySpecialRightsType = rdf.DatabaseProtectedBySpecialRightsType,
                PersonalDataContainmentType = rdf.PersonalDataContainmentType
            };

            view.AuthorsWorkTypeValue = await codelistProviderClient.MapCodelistValue(DcatDistribution.AuthorsWorkTypeCodelist, view.AuthorsWorkType?.ToString(), language);
            view.OriginalDatabaseTypeValue = await codelistProviderClient.MapCodelistValue(DcatDistribution.OriginalDatabaseTypeCodelist, view.OriginalDatabaseType?.ToString(), language);
            view.DatabaseProtectedBySpecialRightsTypeValue = await codelistProviderClient.MapCodelistValue(DcatDistribution.DatabaseProtectedBySpecialRightsTypeCodelist, view.DatabaseProtectedBySpecialRightsType?.ToString(), language);
            view.PersonalDataContainmentTypeValue = await codelistProviderClient.MapCodelistValue(DcatDistribution.PersonalDataContainmentTypeCodelist, view.PersonalDataContainmentType?.ToString(), language);

            return view;
        }
    }
}
