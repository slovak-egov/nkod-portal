using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class AnonymousHttpContextValueAccessor : IHttpContextValueAccessor
    {
        public string? Publisher => null;

        public string? Token => null;

        public bool HasRole(string role) => false;

        public string? InvitationToken => null;

        public string? UserId => null;
    }
}
