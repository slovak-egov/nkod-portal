using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public interface IExternalSourceHarvestionResult
    {
        string Name { get; }

        bool Success { get; }

        TimeSpan Duration { get; }

        string? Note { get; }

        void WriteTriples(IGraph triples, IUriNode parentNode, string parentPredicateName);
    }
}
