using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class MessagesExecutionTrigFile : TrigFile
    {
        public static readonly Uri EventPredicate = new Uri("http://etl.linkdpipes.com/ontology/event");

        public static readonly Uri EventType = new Uri("http://etl.linkdpipes.com/ontology/event");

        public static readonly Uri ExecutionBeginEventType = new Uri("http://linkedpipes.com/ontology/events/ExecutionBegin");

        public static readonly Uri ExecutionEndEventType = new Uri("http://linkedpipes.com/ontology/events/ExecutionEnd");

        public static readonly Uri EventCreatedPredicate = new Uri("http://linkedpipes.com/ontology/events/created");

        public MessagesExecutionTrigFile(string path) : base(path)
        {
            SubjectId = Graphs.FirstOrDefault()?.GetGraphId();
            Graph = SubjectId is not null ? GetGraph(SubjectId) : null;
            SubjectNode = SubjectId is not null ? Graph?.CreateUriNode(SubjectId) : null;
        }

        public Uri? SubjectId { get; }

        public IUriNode? SubjectNode { get; }

        public IGraph? Graph { get; }

        public Uri? GetEventId(Uri type)
        {
            if (Graph is null || SubjectId is null)
            {
                return null;
            }

            foreach (Triple t1 in Graph.GetTriplesWithSubjectPredicate(SubjectNode, Graph.CreateUriNode(EventPredicate)))
            {
                if (t1.Object is IUriNode objectNode && t1.Object.HasType(Graph, type))
                {
                    return objectNode.Uri;
                }
            }

            return null;
        }

        public DateTimeOffset? Started
        {
            get
            {
                Uri? eventId = GetEventId(ExecutionBeginEventType);
                return eventId is not null ? Graph?.GetDateTimeOffset(eventId, EventCreatedPredicate) : null;
            }
        }

        public DateTimeOffset? End
        {
            get
            {
                Uri? eventId = GetEventId(ExecutionEndEventType);
                return eventId is not null ? Graph?.GetDateTimeOffset(eventId, EventCreatedPredicate) : null;
            }
        }
    }
}
