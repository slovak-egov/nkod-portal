using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class SparqlTask : RdfObject
    {
        private readonly string prefix;

        public SparqlTask(IGraph graph, INode subjectNode, string prefix) : base(graph, subjectNode)
        {
            this.prefix = prefix;
        }

        public Uri GetPredicate(string name) => new Uri($"{prefix}{name}");

        public string? Endpoint => this.GetString(GetPredicate("endpoint"));

        public string? FileName => this.GetString(GetPredicate("fileName"));
    }
}
