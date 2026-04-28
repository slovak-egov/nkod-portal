using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class HttpTask : RdfObject
    {
        public static readonly Uri FileUriPredicate = GetPredicate("fileUri");

        public static readonly Uri FileNamePredicate = GetPredicate("fileName");

        public static readonly Uri ReferencePredicate = GetPredicate("reference");

        public static readonly Uri ReferenceType = GetPredicate("Reference");

        public HttpTask(IGraph graph, INode subjectNode) : base(graph, subjectNode)
        {
        }

        public static Uri GetPredicate(string name) => new Uri($"http://plugins.linkedpipes.com/ontology/e-httpGetFiles#{name}");

        public Uri? FileUri => this.GetUri(FileUriPredicate);

        public string? FileName => this.GetString(FileNamePredicate);
    }
}
