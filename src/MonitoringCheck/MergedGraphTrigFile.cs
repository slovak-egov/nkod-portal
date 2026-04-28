using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class MergedGraphTrigFile : TrigFile
    {
        public MergedGraphTrigFile(string path) : base(path)
        {
            Graph = new Graph();
            foreach (IGraph g in Graphs) 
            {
                Graph.Merge(g);
            }
        }

        public IGraph Graph { get; }
    }
}
