using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public record class HarvestationProblem(string Name, string? Note)
    {
        public void WriteTriples(IGraph triples, IUriNode parentNode, string parentPredicateName)
        {
            IUriNode identifierNode = triples.CreateUriNode(new Uri(parentNode.Uri.OriginalString + "/" + Guid.NewGuid().ToString()));

            void Assert(string predicateName, INode objectNode)
            {
                triples.Assert(identifierNode, triples.CreateUriNode(Harvestation.GetPredicate(predicateName)), objectNode);
            }

            triples.Assert(identifierNode, triples.CreateRdfTypeNode(), triples.CreateUriNode(Harvestation.GetPredicate("HarvestationProblem")));
            triples.Assert(parentNode, triples.CreateUriNode(Harvestation.GetPredicate(parentPredicateName)), identifierNode);

            Assert("name", Name.ToLiteral(triples));
            Assert("note", (Note ?? string.Empty).ToLiteral(triples));
        }
    }
}
