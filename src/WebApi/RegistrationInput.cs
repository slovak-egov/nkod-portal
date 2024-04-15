using Microsoft.AspNetCore.Mvc.Formatters;
using NkodSk.Abstractions;
using System.Xml.Linq;
using static Lucene.Net.Util.Packed.PackedInt32s;

namespace WebApi
{
    public class RegistrationInput
    {
        public string? Website { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? LegalForm { get; set; }

        public async Task<ValidationResults> Validate(ICodelistProviderClient codelistProvider)
        {
            ValidationResults results = new ValidationResults();

            results.ValidateUrl(nameof(Website), Website, true);
            results.ValidateEmail(nameof(Email), Email, true);
            await results.ValidateRequiredCodelistValue(nameof(LegalForm), LegalForm, FoafAgent.LegalFormCodelist, codelistProvider);

            return results;
        }

        public void MapToRdf(FoafAgent agent, string? name = null)
        {
            if (name is not null)
            {
                agent.SetNames(new Dictionary<string, string> { { "sk", name } });
            }
            agent.HomePage = Website.AsUri();
            agent.EmailAddress = Email;
            agent.Phone = Phone;
            agent.LegalForm = LegalForm.AsUri();
        }
    }
}
