using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class HarvestationResult : HarvestationResultValues
    {
        private readonly Harvestation harvestation;

        private readonly Uri? context;

        private readonly string contextType;

        public HarvestationResult(Harvestation harvestation, Uri context, string contextType)
        {
            this.harvestation = harvestation;
            this.context = context;
            this.contextType = contextType;
            IdentifierNode = Harvestation.DefaultGraph.CreateUriNode(new Uri(harvestation.IdentifierNode.Uri.OriginalString + "/" + Guid.NewGuid().ToString()));
        }

        public IUriNode IdentifierNode { get; }

        public void WriteTriples(IGraph triples)
        {
            WriteTriples(triples, IdentifierNode, context, contextType);
        }
    }
}
