using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringCheck
{
    public class StaticHttpContextAccessor : IHttpContextValueAccessor
    {
        private readonly HashSet<string> roles;

        public StaticHttpContextAccessor(string? token, IEnumerable<string>? roles = null) 
        {
            Token = token;
            this.roles = new HashSet<string>(roles ?? []);
        }

        public string? Publisher => null;

        public string? Token { get; }

        public string? UserId => null;

        public bool HasRole(string role) => roles.Contains(role);
    }
}
