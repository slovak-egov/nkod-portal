using Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class Report : RdfObject
    {
        public static readonly Uri TaskPredicate = GetPredicate("task");

        public static readonly Uri StatusPredicate = GetPredicate("status");

        public static readonly Uri DurationPredicate = GetPredicate("duration");

        public static readonly Uri ExceptionPredicate = GetPredicate("exception");

        public static readonly Uri MessagePredicate = GetPredicate("message");

        public static readonly Uri ReportType = GetPredicate("Report");

        public static readonly Uri StatusSuccess = GetPredicate("Success");

        public static readonly Uri StatusFailed = GetPredicate("Failed");

        public Report(IGraph graph, INode subjectNode) : base(graph, subjectNode)
        {
        }

        public static Uri GetPredicate(string name) => new Uri($"https://vocabulary.etl.linkedpipes.com/report/{name}");

        public TimeSpan? Duration
        {
            get
            {
                long? milliseconds = this.GetLong(DurationPredicate);
                return milliseconds.HasValue ? TimeSpan.FromMilliseconds(milliseconds.Value) : null;
            }
        }

        public Uri? Status => this.GetUri(StatusPredicate);

        public Uri? Task => this.GetUri(TaskPredicate);

        public bool IsSuccess => UriEqualityComparer.Instance.Equals(Status, StatusSuccess);

        public RdfObject? Error
        {
            get
            {
                IUriNode? errorUri = this.GetUriNode(ExceptionPredicate);
                if (errorUri is not null)
                {
                    return new RdfObject(Graph, errorUri);
                }
                return null;
            }
        }

        public ExternalSourceHarvestionResult CreateResult(string name)
        {
            RdfObject? error = Error;
            return new ExternalSourceHarvestionResult(name, IsSuccess, Duration ?? TimeSpan.Zero, error?.GetString(MessagePredicate));
        }
    }
}
