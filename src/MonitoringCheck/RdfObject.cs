using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class RdfObject : ITripleIndexForSubject
    {
        public RdfObject(IGraph graph, INode subjectNode)
        {
            Graph = graph;
            SubjectNode = subjectNode;
        }

        public IGraph Graph { get; }

        public INode SubjectNode { get; }

        public Uri? SubjectId => SubjectNode is IUriNode uriNode ? uriNode.Uri : null;

        public IEnumerable<Triple> Triples => Graph.GetTriplesWithSubject(SubjectNode);
    }
}
