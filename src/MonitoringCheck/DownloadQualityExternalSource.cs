using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class DownloadQualityExternalSource : IExternalSourceHarvestionResult
    {
        public required Uri Dataset { get; init; }

        public required string Distribution { get; init; }

        public required Uri Publisher { get; init; }

        public string Name => Distribution;

        public bool Success { get; set; }

        public TimeSpan Duration { get; set; }

        public string? Note { get; set; }

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
            Assert("dataset", triples.CreateUriNode(Dataset));
            Assert("publisher", triples.CreateUriNode(Publisher));
        }
    }
}
