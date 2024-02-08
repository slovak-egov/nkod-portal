using NkodSk.Abstractions;
using System.Data;

namespace WebApi
{
    public class AdminPublisherInput
    {
        public string? Id { get; set; }

        public string? Uri { get; set; }

        public Dictionary<string, string>? Name { get; set; }

        public string? Website { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? LegalForm { get; set; }

        public bool IsEnabled { get; set; }

        public async Task<ValidationResults> Validate(ICodelistProviderClient codelistProvider)
        {
            ValidationResults results = new ValidationResults();

            List<string> languages = Name?.Keys.ToList() ?? new List<string>();
            if (!languages.Contains("sk"))
            {
                languages.Add("sk");
            }

            results.ValidateUrl(nameof(Uri), Uri, true);
            results.ValidateLanguageTexts(nameof(Name), Name, languages, true);
            results.ValidateUrl(nameof(Website), Website, true);
            results.ValidateEmail(nameof(Email), Email, true);
            await results.ValidateRequiredCodelistValue(nameof(LegalForm), LegalForm, FoafAgent.LegalFormCodelist, codelistProvider);

            return results;
        }

        public void MapToRdf(FoafAgent agent)
        {
            agent.SetNames(Name ?? new Dictionary<string, string>());
            agent.HomePage = Website.AsUri();
            agent.EmailAddress = Email;
            agent.Phone = Phone;
            agent.LegalForm = LegalForm.AsUri();
        }
    }
}
