using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;

namespace MonitoringCheck
{
    public static class SparqlHelper
    {
        public static object Query(IGraph graph, string query)
        {
            using TripleStore store = new TripleStore();
            store.Add(graph);
            SparqlQueryParser parser = new SparqlQueryParser();
            InMemoryDataset ds = new InMemoryDataset(store, true);
            SparqlQuery q = parser.ParseFromString(query);
            using LeviathanQueryProcessor processor = new LeviathanQueryProcessor(ds);
            return processor.ProcessQuery(q);
        }
    }
}
