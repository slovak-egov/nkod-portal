using Microsoft.AspNetCore.Mvc.Formatters;
using NkodSk.Abstractions;
using System.Data;
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

        public async Task<ValidationResults> Validate(string publisher, IDocumentStorageClient documentStorage, ICodelistProviderClient codelistProvider)
        {
            ValidationResults results = new ValidationResults();

            if (!string.IsNullOrEmpty(AuthorsWorkType))
            {
                await results.ValidateRequiredCodelistValue(nameof(AuthorsWorkType), AuthorsWorkType, DcatDistribution.LicenseCodelist, codelistProvider);
            }

            if (!string.IsNullOrEmpty(OriginalDatabaseType))
            {
                await results.ValidateRequiredCodelistValue(nameof(OriginalDatabaseType), OriginalDatabaseType, DcatDistribution.LicenseCodelist, codelistProvider);
            }

            if (!string.IsNullOrEmpty(DatabaseProtectedBySpecialRightsType))
            {
                await results.ValidateRequiredCodelistValue(nameof(DatabaseProtectedBySpecialRightsType), DatabaseProtectedBySpecialRightsType, DcatDistribution.LicenseCodelist, codelistProvider);
            }

            if (!string.IsNullOrEmpty(PersonalDataContainmentType))
            {
                await results.ValidateRequiredCodelistValue(nameof(PersonalDataContainmentType), PersonalDataContainmentType, DcatDistribution.PersonalDataContainmentTypeCodelist, codelistProvider);
            }

            if (PersonalDataContainmentType == "https://data.gov.sk/def/personal-data-occurence-type/3")
            {
                results.Add("personaldatacontainmenttype", "Typ výskytu osobných údajov nemôže ostať nešpecifikovaný");
            }

            return results;
        }

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
