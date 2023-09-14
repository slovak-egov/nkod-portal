using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class Facet
    {
        public Facet(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public Dictionary<string, int> Values { get; } = new Dictionary<string, int>(10);
    }
}
