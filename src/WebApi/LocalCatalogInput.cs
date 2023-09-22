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

        public ValidationResults Validate()
        {
            ValidationResults results = new ValidationResults();

            List<string> languages = Name?.Keys.ToList() ?? new List<string>();
            if (!languages.Contains("sk"))
            {
                languages.Add("sk");
            }

            results.ValidateLanguageTexts(nameof(Name), Name, languages, true);
            results.ValidateLanguageTexts(nameof(Description), Description, languages, true);
            results.ValidateLanguageTexts(nameof(ContactName), ContactName, languages, false);
            results.ValidateEmail(nameof(ContactEmail), ContactEmail, false);
            results.ValidateUrl(nameof(HomePage), HomePage, false);

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
        }
    }
}
