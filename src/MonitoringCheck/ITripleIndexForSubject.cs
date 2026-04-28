using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public interface ITripleIndexForSubject
    {
        public IGraph Graph { get; }

        public INode SubjectNode { get; }
    }
}
