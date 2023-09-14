using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfFileStorage.Test
{
    public class StaticAccessPolicy : DynamicAccessPolicy
    {
        public static StaticAccessPolicy Allow { get; } = new StaticAccessPolicy(true);

        public static StaticAccessPolicy Deny { get; } = new StaticAccessPolicy(false);

        private StaticAccessPolicy(bool defaultResult) : base(_ => defaultResult)
        {
        }
    }
}
