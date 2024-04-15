using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class StaticHttpContextValueAccessor : IHttpContextValueAccessor
    {
        private readonly string? role;

        public StaticHttpContextValueAccessor(string? publisher, string? token, string? role, string? id)
        {
            Publisher = publisher;
            Token = token;
            UserId = id;
            this.role = role;
        }

        public string? Publisher { get; }

        public string? Token { get; }

        public string? UserId { get; }

        public string? InvitationToken { get; set; }

        public bool HasRole(string role)
        {
            return string.Equals(this.role, role);
        }
    }
}
