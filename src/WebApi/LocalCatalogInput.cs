using NkodSk.Abstractions;
using System.Data;

namespace WebApi
{
    public class LocalCatalogInput
    {
        public string? Id { get; set; }

        public bool IsPublic { get; set; }

        public Dictionary<string, string>? Name { get; set; }

        public Dictionary<string, string>? Description { get; set; }

        public Dictionary<string, string>? ContactName { get; set; }

        public string? ContactEmail { get; set; }

        public string? HomePage { get; set; }

        public string? EndpointUrl { get; set; }

        public string? Type { get; set; }

        public async Task<ValidationResults> Validate(ICodelistProviderClient codelistProvider)
        {
            ValidationResults results = new ValidationResults();

            List<string> languages = Name?.Keys.ToList() ?? new List<string>();
            if (!languages.Contains("sk"))
            {
                languages.Add("sk");
            }

            results.ValidateLanguageTexts(nameof(Name), Name, languages, true);
            results.ValidateLanguageTexts(nameof(Description), Description, languages, true);
            results.ValidateLanguageTexts(nameof(ContactName), ContactName, languages, true);
            results.ValidateEmail(nameof(ContactEmail), ContactEmail, true);
            results.ValidateUrl(nameof(HomePage), HomePage, false);
            results.ValidateUrl(nameof(EndpointUrl), EndpointUrl, true);
            await results.ValidateRequiredCodelistValue(nameof(Type), Type, DcatCatalog.LocalCatalogTypeCodelist, codelistProvider);

            return results;
        }

        public void MapToRdf(Uri publisher, DcatCatalog catalog)
        {
            catalog.SetTitle(Name ?? new Dictionary<string, string>());
            catalog.SetDescription(Description ?? new Dictionary<string, string>());
            catalog.Publisher = publisher;
            catalog.SetContactPoint(
                ContactName is not null ? new LanguageDependedTexts(ContactName) : null,
                ContactEmail);
            catalog.HomePage = HomePage.AsUri();
            catalog.ShouldBePublic = IsPublic;
            catalog.EndpointUrl = EndpointUrl.AsUri();
            catalog.Type = Type.AsUri();
        }
    }
}
