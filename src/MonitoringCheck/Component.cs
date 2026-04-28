using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class Component : RdfObject
    {
        public static readonly Uri StatusPredicate = Extensions.GetPredicate("status");

        public static readonly Uri DataUnitPredicate = Extensions.GetPredicate("dataUnit");

        public static readonly Uri BindingPredicate = Extensions.GetPredicate("binding");

        public static readonly Uri DataPathPredicate = Extensions.GetPredicate("dataPath");
        
        public static readonly Uri ExectionStartPredicate = Extensions.GetPredicate("executionStart");

        public static readonly Uri ExectionEndPredicate = Extensions.GetPredicate("executionEnd");

        public static readonly Uri LabelPredicate = new Uri("http://www.w3.org/2004/02/skos/core#prefLabel");

        public static readonly Uri ComponentType = new Uri("http://linkedpipes.com/ontology/Component");

        public Component(IGraph graph, IUriNode objectNode) : base(graph, objectNode)
        {
            foreach (IUriNode n in this.GetUriNodes(DataUnitPredicate))
            {
                string? binding = Extensions.GetString(graph, n.Uri, BindingPredicate);
                string? dataPath = Extensions.GetString(graph, n.Uri, DataPathPredicate);
                if (binding is not null && dataPath is not null)
                {
                    BindingPaths[binding] = dataPath;
                }
            }
        }

        public Uri? Status => this.GetUri(StatusPredicate);

        public Dictionary<string, string> BindingPaths { get; } = [];

        public string? Label => this.GetString(LabelPredicate);

        public DateTimeOffset? Started => this.GetDateTimeOffset(ExectionStartPredicate);

        public DateTimeOffset? Ended => this.GetDateTimeOffset(ExectionEndPredicate);

        public TimeSpan? Duration => Started.HasValue && Ended.HasValue ? Ended.Value - Started.Value : null;

        private static string TranslatePath(string path)
        {
            return string.Join(Path.DirectorySeparatorChar, path.Split(['/', '\\']));
        }

        public string? GetAbsolutePath(PipelineLog log, string bindingName)
        {
            if (BindingPaths.TryGetValue(bindingName, out string? localPath))
            {
                return log.GetAbsolutePath(TranslatePath(localPath));
            }
            return null;
        }

        public Graph GetGraph(PipelineLog log, string bindingName)
        {
            string? path = GetAbsolutePath(log, bindingName);
            if (Directory.Exists(path))
            {
                Graph? g = PipelineLog.GetGraphFromDataJson(path);
                if (g is not null)
                {
                    return g;
                }
            }
            return new Graph();
        }
    }
}
