using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class PasswordRecoveryInput
    {
        public string? Email { get; set; }

        public ValidationResults Validate()
        {
            ValidationResults results = new ValidationResults();

            results.ValidateEmail(nameof(Email), Email, true);

            return results;
        }
    }
}
