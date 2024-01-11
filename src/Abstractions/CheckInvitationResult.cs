using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class CheckInvitationResult
    {
        public bool IsValid { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public string? Publisher { get; set; }

        public string? Role { get; set; }
    }
}
