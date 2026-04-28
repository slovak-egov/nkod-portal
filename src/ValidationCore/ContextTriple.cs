using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace ValidationCore
{
    public record ContextTriple(INode? Subject = null, INode? Predicate = null, INode? Object = null)
    {
    }
}
