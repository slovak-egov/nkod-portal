using Microsoft.AspNetCore.Mvc.Formatters;
using NkodSk.Abstractions;
using static Lucene.Net.Util.Packed.PackedInt32s;

namespace WebApi
{
    public class DistributionLicenceInput
    {
        public string? Id { get; set; }

        public string? AuthorsWorkType { get; set; }

        public string? OriginalDatabaseType { get; set; }

        public string? DatabaseProtectedBySpecialRightsType { get; set; }

        public string? PersonalDataContainmentType { get; set; }

        public void MapToRdf(DcatDistribution distribution)
        {
            LegTermsOfUse? termsOfUse = distribution.TermsOfUse;

            distribution.SetTermsOfUse(
                AuthorsWorkType.AsUri() ?? termsOfUse?.AuthorsWorkType, 
                OriginalDatabaseType.AsUri() ?? termsOfUse?.OriginalDatabaseType, 
                DatabaseProtectedBySpecialRightsType.AsUri() ?? termsOfUse?.DatabaseProtectedBySpecialRightsType, 
                PersonalDataContainmentType.AsUri() ?? termsOfUse?.PersonalDataContainmentType, 
                termsOfUse?.AuthorName,
                termsOfUse?.OriginalDatabaseAuthorName);
        }
    }
}
