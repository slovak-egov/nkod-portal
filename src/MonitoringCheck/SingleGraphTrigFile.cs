using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class SingleGraphTrigFile : TrigFile
    {
        public SingleGraphTrigFile(string path) : base(path)
        {
            if (Graphs.Count() != 1)
            {
                throw new Exception("Trig file must contain at least one graph.");
            }

            Graph = Graphs.First();
            PrimaryId = Graph.GetGraphId() ?? throw new Exception("Trig graph does not have name");
            PrimaryNode = Graph.CreateUriNode(PrimaryId);
        }

        public IGraph Graph { get; }

        public Uri PrimaryId { get; }

        public INode PrimaryNode { get; }
    }
}
