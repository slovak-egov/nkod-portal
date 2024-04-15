using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class UserSaveResult : SaveResult
    {
        public string? InvitationToken { get; set; }
    }
}
