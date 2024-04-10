using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class UserRegistrationInput
    {
        public const int MinimalPasswordLength = 6;

        public string? Email { get; set; }

        public string? Password { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public ValidationResults Validate()
        {
            ValidationResults results = new ValidationResults();

            results.ValidateEmail(nameof(Email), Email, true);
            results.ValidateRequiredText(nameof(FirstName), FirstName);
            results.ValidateRequiredText(nameof(LastName), LastName);
            results.ValidatePassword(nameof(Password), Password);

            return results;
        }
    }
}
