using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportRegistrations
{
    public class HttpContextValueAccessor : IHttpContextValueAccessor
    {
        public string? Publisher { get; set; }

        public string? Token { get; set; }

        public string? UserId => null;

        public bool HasRole(string role)
        {
            return string.Equals(role, "Harvester", StringComparison.OrdinalIgnoreCase);
        }
    }
}
