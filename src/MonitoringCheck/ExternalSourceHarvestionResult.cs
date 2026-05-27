using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public record ExternalSourceHarvestionResult(string Name, bool Success, TimeSpan Duration, string? Note) : IExternalSourceHarvestionResult
    {
        public void WriteTriples(IGraph triples, IUriNode parentNode, string parentPredicateName)
        {
            IUriNode identifierNode = triples.CreateUriNode(new Uri(parentNode.Uri.OriginalString + "/" + Guid.NewGuid().ToString()));

            void Assert(string predicateName, INode objectNode)
            {
                triples.Assert(identifierNode, triples.CreateUriNode(Harvestation.GetPredicate(predicateName)), objectNode);
            }

            triples.Assert(identifierNode, triples.CreateRdfTypeNode(), triples.CreateUriNode(Harvestation.GetPredicate("HarvestationSource")));
            triples.Assert(parentNode, triples.CreateUriNode(Harvestation.GetPredicate(parentPredicateName)), identifierNode);

            //if (!Success)
            //{
            //    triples.Assert(identifierNode, triples.CreateRdfTypeNode(), triples.CreateUriNode(Harvestation.GetPredicate("HarvestationProblem")));
            //    triples.Assert(parentNode, triples.CreateUriNode(Harvestation.GetPredicate("hasProblem")), identifierNode);
            //}

            Assert("isSourceFor", parentNode);
            Assert("name", Name.ToLiteral(triples));
            Assert("duration", Duration.ToLiteral(triples));
            Assert("success", Success.ToLiteral(triples));
            Assert("note", (Note ?? string.Empty).ToLiteral(triples));
        }
    }
}
