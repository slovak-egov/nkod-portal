using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace MonitoringCheck
{
    public class TrigFile
    {
        public TrigFile(string path)
        {
            Store = new TripleStore();
            Store.LoadFromFile(path, new TriGParser());
        }

        public TripleStore Store { get; }

        public IEnumerable<IGraph> Graphs => Store.Graphs;

        public IGraph? GetGraph(Uri id) => Store.Graphs.FirstOrDefault(g => g.Name is IUriNode uriNode && uriNode.Uri.Equals(id));
    }
}
