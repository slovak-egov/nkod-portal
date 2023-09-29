using AngleSharp.Common;
using System.Xml.Linq;

namespace NkodSk.Abstractions
{
    public class NewUserInput
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Role { get; set; }

        public string? IdentificationNumber { get; set; }

        public ValidationResults Validate()
        {
            ValidationResults results = new ValidationResults();

            results.ValidateRequiredText(nameof(FirstName), FirstName);
            results.ValidateRequiredText(nameof(LastName), LastName);
            results.ValidateRequiredText(nameof(Email), Email);

            return results;
        }
    }
}
