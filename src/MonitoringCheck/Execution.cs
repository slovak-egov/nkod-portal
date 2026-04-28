using Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class Execution : RdfObject
    {
        public static readonly Uri ComponentPredicate = new Uri("http://linkedpipes.com/ontology/component");

        public static readonly Uri PipelinePredicate = new Uri("http://etl.linkedpipes.com/ontology/pipeline");

        public static readonly Uri StatusPredicate = new Uri("http://etl.linkedpipes.com/ontology/status");

        public static readonly Uri StatusRunning = new Uri("http://etl.linkedpipes.com/resources/status/running");

        public static readonly Uri StatusFinished = new Uri("http://etl.linkedpipes.com/resources/status/finished");

        public static readonly Uri StatusFailed = new Uri("http://etl.linkedpipes.com/resources/status/failed");

        public static readonly Uri StatusQueued = new Uri("http://etl.linkedpipes.com/resources/status/queued");

        public static readonly Uri StatusCancelling = new Uri("http://etl.linkedpipes.com/resources/status/cancelling");

        public static readonly Uri StatusCancelled = new Uri("http://etl.linkedpipes.com/resources/status/cancelled");

        private readonly Dictionary<Uri, Component> componentsByUri = [];

        public Execution(IGraph graph, INode objectNode) : base(graph, objectNode)
        {
            IUriNode componentTypeNode = graph.CreateUriNode(Component.ComponentType);
            foreach (Triple t in graph.GetTriplesWithSubjectPredicate(objectNode, graph.CreateUriNode(ComponentPredicate)))
            {
                if (t.Object is IUriNode componentNode && componentNode.HasType(graph, componentTypeNode))
                {
                    Component component = new Component(graph, componentNode);
                    componentsByUri[componentNode.Uri] = component;
                }
            }
        }

        public Uri? Status => this.GetUri(StatusPredicate);

        public Uri? Pipeline => this.GetUri(PipelinePredicate);

        public Component? GetComponent(Uri componentUri) => componentsByUri.TryGetValue(componentUri, out Component? component) ? component : null;

        public bool IsRunnning => UriEqualityComparer.Instance.Equals(Status, StatusRunning);

        public bool IsFinished => UriEqualityComparer.Instance.Equals(Status, StatusFinished);

        public bool IsFailed => UriEqualityComparer.Instance.Equals(Status, StatusFailed);

        public bool IsQueued => UriEqualityComparer.Instance.Equals(Status, StatusQueued);

        public bool IsCancelling => UriEqualityComparer.Instance.Equals(Status, StatusCancelling);

        public bool IsCancelled => UriEqualityComparer.Instance.Equals(Status, StatusCancelled);

        public bool IsDone => IsFinished || IsCancelled || IsFailed;
    }
}
