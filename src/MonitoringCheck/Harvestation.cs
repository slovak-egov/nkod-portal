using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;

namespace MonitoringCheck
{
    public class Harvestation : HarvestationResultValues
    {
        private readonly Dictionary<Pipeline, PipelineLog> pipelines = [];

        private static readonly Uri defaultNamespace = new Uri("https://data.gov.sk/custom/monitoring/");

        public Harvestation(string id)
        {
            IdentifierNode = DefaultGraph.CreateUriNode(GetPredicate(id));
        }

        public void Add(Pipeline pipeline, PipelineLog log)
        {
            pipelines[pipeline] = log;
        }

        public DateTimeOffset? Started => pipelines.Values.Where(l => l.Started.HasValue).Select(l => l.Started).First();

        public DateTimeOffset? Ended => pipelines.Values.Where(l => l.Ended.HasValue).Select(l => l.Ended).Last();

        public TimeSpan? Duration => Started.HasValue && Ended.HasValue ? Ended.Value - Started.Value : null;

        public PipelineLog? GetLog(Pipeline pipeline) => pipelines.TryGetValue(pipeline, out PipelineLog? log) ? log : null;

        public List<IExternalSourceHarvestionResult> CatalogSources { get; } = [];

        public int PublisherCount { get; set; }

        public int CatalogCount { get; set; }

        public int CorrectlyHarvestedCatalogCount { get; set; }

        public int CatalogSourcesCount => CatalogSources.Count;

        public int UnavaiableCatalogSourcesCount => CatalogSources.Count(s => !s.Success);

        public float CatalogSuccessRatio => CatalogSources.Count > 0 ? CatalogSources.Count(s => s.Success) / (float)CatalogSources.Count : 1;

        public TimeSpan CatalogSourcesDuration => TimeSpan.FromMilliseconds(CatalogSources.Sum(s => s.Duration.TotalMilliseconds));

        public TimeSpan CatalogSourcesAverageDuration => CatalogSources.Count > 0 ? TimeSpan.FromMilliseconds(CatalogSources.Average(s => s.Duration.TotalMilliseconds)) : TimeSpan.Zero;

        public List<IExternalSourceHarvestionResult> DistributionSources { get; } = [];

        public int DistributionSourcesCount => DistributionSources.Count;

        public int UnavailableDistributionSourcesCount => DistributionSources.Count(s => !s.Success);

        public static Graph DefaultGraph { get;  } = new Graph();

        public IUriNode IdentifierNode { get; }

        public static Uri GetPredicate(string name) => new Uri(defaultNamespace + WebUtility.UrlEncode(name));

        public void WriteTriples(IGraph triples)
        {
            WriteTriples(triples, IdentifierNode, GetPredicate("all"), "all");

            void Assert(string predicateName, INode objectNode)
            {
                triples.Assert(IdentifierNode, triples.CreateUriNode(GetPredicate(predicateName)), objectNode);
            }

            if (Started.HasValue)
            {
                Assert("started", Started.Value.ToLiteral(triples));
            }
            if (Ended.HasValue)
            {
                Assert("ended", Ended.Value.ToLiteral(triples));
            }
            if (Duration.HasValue)
            {
                Assert("duration", Duration.Value.ToLiteral(triples));
            }

            Assert("publisherCount", PublisherCount.ToLiteral(triples));
            Assert("catalogCount", CatalogCount.ToLiteral(triples));
            Assert("correctlyHarvestedCatalogCount", CorrectlyHarvestedCatalogCount.ToLiteral(triples));
            Assert("catalogSourcesCount", CatalogSourcesCount.ToLiteral(triples));
            Assert("unavaiableCatalogSourcesCount", UnavaiableCatalogSourcesCount.ToLiteral(triples));
            Assert("catalogSuccessRatio", CatalogSuccessRatio.ToLiteral(triples));
            Assert("catalogSourcesDuration", CatalogSourcesDuration.ToLiteral(triples));
            Assert("catalogSourcesAverageDuration", CatalogSourcesAverageDuration.ToLiteral(triples));
            Assert("distributionSourcesCount", DistributionSourcesCount.ToLiteral(triples));
            Assert("unavailableDistributionSourcesCount", UnavailableDistributionSourcesCount.ToLiteral(triples));

            foreach (IExternalSourceHarvestionResult externalSourceResult in CatalogSources)
            {
                externalSourceResult.WriteTriples(triples, IdentifierNode, "hasCatalogSource");
            }

            foreach (IExternalSourceHarvestionResult externalSourceResult in DistributionSources)
            {
                externalSourceResult.WriteTriples(triples, IdentifierNode, "hasDistributionSource");
            }
        }
    }
}
