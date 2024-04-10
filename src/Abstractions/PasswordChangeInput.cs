using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class PasswordChangeInput
    {
        public string? OldPassword { get; set; }

        public string? NewPassword { get; set; }

        public ValidationResults Validate()
        {
            ValidationResults results = new ValidationResults();

            results.ValidatePassword(nameof(NewPassword), NewPassword);

            return results;
        }
    }
}
