using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class PasswordRecoveryConfirmationInput
    {
        public string? Id { get; set; }

        public string? Token { get; set; }

        public string? Password { get; set; }
    }
}
