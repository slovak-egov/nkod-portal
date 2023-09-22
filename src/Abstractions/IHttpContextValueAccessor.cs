using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public interface IHttpContextValueAccessor
    {
        bool HasRole(string role);

        string? Publisher { get; }

        string? Token { get; }

        string? UserId { get; }
    }
}
