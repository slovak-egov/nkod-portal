using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace MonitoringCheck
{
    public class PipelineLog
    {
        public PipelineLog(string dir)
        {
            Dir = dir;

            MessagesExecution = new MessagesExecutionTrigFile(Path.Combine(dir, "messages", "execution.trig"));
            
            ExecutionTrigFile executionTrigFile = new ExecutionTrigFile(GetAbsolutePath("execution.trig"));
            TrigFile definitionFile = new TrigFile(GetAbsolutePath("pipeline.trig"));
            foreach (IGraph graph in definitionFile.Graphs)
            {
                executionTrigFile.Graph.Merge(graph);
            }
            Execution = executionTrigFile.Execution;
        }

        public static List<PipelineLog> Load(string dir)
        {
            List<PipelineLog> logs = [];
            foreach (string path in Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly))
            {
                if (IsValidDir(path))
                {
                    logs.Add(new PipelineLog(path));
                }
            }
            return logs;
        }

        public static bool IsValidDir(string dir) => File.Exists(Path.Combine(dir, "pipeline.trig"));

        public string GetAbsolutePath(string localPath) => Path.Combine(Dir, localPath);

        public static List<string> GetReferencedPath(string dir)
        {
            List<string> fullPaths = [];
            string dataJsonDir = Path.Combine(dir, "data.json");
            if (File.Exists(dataJsonDir))
            {
                string[]? refDirs = JsonSerializer.Deserialize<string[]>(File.ReadAllText(dataJsonDir));
                foreach (string refDir in refDirs ?? [])
                {
                    string path = Path.Combine(dir, refDir);
                    fullPaths.Add(path);
                }
            }
            return fullPaths;
        }

        public static List<string> GetReferencedPath(string dir, string localPath)
        {
            List<string> fullPaths = [];
            string dataJsonDir = Path.Combine(dir, "data.json");
            if (File.Exists(dataJsonDir))
            {
                string[]? refDirs = JsonSerializer.Deserialize<string[]>(File.ReadAllText(dataJsonDir));
                foreach (string refDir in refDirs ?? [])
                {
                    string path = Path.Combine(dir, refDir, localPath);
                    fullPaths.Add(path);
                }
            }
            return fullPaths;
        }

        public string Dir { get; }

        public MessagesExecutionTrigFile MessagesExecution { get; }

        public Execution Execution { get; }

        public DateTimeOffset? Started => MessagesExecution.Started;

        public DateTimeOffset? Ended => MessagesExecution.End;

        public bool IsDone => Execution.IsDone;

        public TimeSpan? Duration => Started.HasValue && Ended.HasValue ? Ended.Value - Started.Value : null;

        public static Graph? GetGraphFromDataJson(string path)
        {
            if (Directory.Exists(path))
            {
                return GetGraphFromDataJson(Path.Combine(path, "data.json"));
            } 
            else if (File.Exists(path))
            {
                string[]? localPaths = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path));
                if (localPaths is not null)
                {
                    Graph graph = new Graph();
                    foreach (string localPath in localPaths)
                    {
                        Graph? g = GetGraphFromFiles(Path.Combine(Path.GetDirectoryName(path)!, localPath));
                        if (g is not null)
                        {
                            graph.Merge(g);
                        }
                    }
                    return graph;
                }
            }
            return null;
        }

        public static Graph? GetGraphFromFiles(string path)
        {
            if (Directory.Exists(path))
            {
                Graph graph = new Graph();
                foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    graph.Merge(GetGraphFromFiles(file));
                }
                return graph;
            } 
            else if (File.Exists(path))
            {
                Graph graph = new Graph();
                
                switch (Path.GetExtension(path).ToLowerInvariant())
                {
                    case ".ttl":
                        graph.LoadFromFile(path, new TurtleParser());
                        break;
                    case ".trig":
                        TripleStore store = new TripleStore();
                        store.LoadFromFile(path, new TriGParser());
                        foreach (IGraph g in store.Graphs)
                        {
                            graph.Merge(g);
                        }
                        break;
                }

                return graph;
            }
            else
            {
                return null;
            }
        }

        public Component? GetComponentById(Uri id) => Execution.GetComponent(id);

        public Component? GetComponentByName(Pipeline pipline, string name)
        {
            Component? component = pipline.GetComponentByName(name);
            if (component?.SubjectId is not null)
            {
                return Execution.GetComponent(component.SubjectId);
            }
            return null;
        }

        public Component? GetComponentByDescription(Pipeline pipline, string description)
        {
            Component? component = pipline.GetComponentByDescription(description);
            if (component?.SubjectId is not null)
            {
                return Execution.GetComponent(component.SubjectId);
            }
            return null;
        }
    }
}
