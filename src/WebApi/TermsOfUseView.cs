using NkodSk.Abstractions;

namespace WebApi
{
    public class TermsOfUseView
    {
        public Uri? AuthorsWorkType { get; set; }

        public Uri? OriginalDatabaseType { get; set; }

        public Uri? DatabaseProtectedBySpecialRightsType { get; set; }

        public Uri? PersonalDataContainmentType { get; set; }

        public static Task<TermsOfUseView> MapFromRdf(LegTermsOfUse rdf, CodelistProviderClient.CodelistProviderClient codelistProviderClient, string language)
        {
            TermsOfUseView view = new TermsOfUseView
            {
                AuthorsWorkType = rdf.AuthorsWorkType,
                OriginalDatabaseType = rdf.OriginalDatabaseType,
                DatabaseProtectedBySpecialRightsType = rdf.DatabaseProtectedBySpecialRightsType,
                PersonalDataContainmentType = rdf.PersonalDataContainmentType
            };

            return Task.FromResult(view);
        }
    }
}
