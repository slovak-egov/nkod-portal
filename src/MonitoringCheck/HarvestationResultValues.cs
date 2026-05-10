using Lucene.Net.Documents;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public abstract class HarvestationResultValues
    {
        public int ExternalSourcesCount => ExternalSources.Count;

        public float SuccessRatio => ExternalSources.Count > 0 ? ExternalSources.Count(s => s.Success) / (float)ExternalSources.Count : 1;

        public int InvalidExternalSourcesCount => ExternalSources.Count(s => !s.Success);

        public TimeSpan ExternalSourcesDuration => TimeSpan.FromMilliseconds(ExternalSources.Sum(s => s.Duration.TotalMilliseconds));

        public TimeSpan ExternalSourcesAverageDuration => ExternalSources.Count > 0 ? TimeSpan.FromMilliseconds(ExternalSources.Average(s => s.Duration.TotalMilliseconds)) : TimeSpan.Zero;

        public int DatasetsCount { get; set; }

        public int NewDatasets { get; set; }

        public int ModifiedDatasets { get; set; }

        public int RemovedDatasets { get; set; }

        public bool HarvestationFailed { get; set; }

        public List<IExternalSourceHarvestionResult> ExternalSources { get; } = [];

        public List<HarvestationProblem> Problems { get; } = [];

        public IEnumerable<HarvestationProblem> AllDetectedProblems
        {
            get
            {
                foreach (HarvestationProblem problem in Problems)
                {
                    yield return problem;
                }

                foreach (IExternalSourceHarvestionResult result in ExternalSources)
                {
                    if (!result.Success)
                    {
                        yield return new HarvestationProblem(result.Name, result.Note);
                    }
                }
            }
        }

        public void Add(HarvestationResultValues result)
        {
            ExternalSources.AddRange(result.ExternalSources);
            Problems.AddRange(result.Problems);
            DatasetsCount += result.DatasetsCount;
            NewDatasets += result.NewDatasets;
            ModifiedDatasets += result.ModifiedDatasets;
            RemovedDatasets += result.RemovedDatasets;
        }

        public void AddDatasetStatus(DatasetModificationStatus status)
        {
            if (status != DatasetModificationStatus.Empty)
            {
                DatasetsCount++;
            }

            switch (status)
            {
                case DatasetModificationStatus.Added:
                    NewDatasets++;
                    break;
                case DatasetModificationStatus.Modified:
                    ModifiedDatasets++;
                    break;
                case DatasetModificationStatus.Removed:
                    RemovedDatasets++;
                    break;
            }
        }

        protected void WriteTriples(IGraph triples, IUriNode identifierNode, Uri? context, string? contextType)
        {
            void Assert(string predicateName, INode objectNode)
            {
                triples.Assert(identifierNode, triples.CreateUriNode(Harvestation.GetPredicate(predicateName)), objectNode);
            }

            triples.Assert(identifierNode, triples.CreateRdfTypeNode(), triples.CreateUriNode(Harvestation.GetPredicate("HarvestationResult")));
            if (contextType is not null)
            {
                Assert("contextType", contextType.ToLiteral(triples));
            }
            if (context is not null)
            {
                Assert("context", triples.CreateUriNode(context));
            }

            Assert("externalSourcesCount", ExternalSourcesCount.ToLiteral(triples));
            Assert("sucessRatio", SuccessRatio.ToLiteral(triples));
            Assert("invalidExternalSourcesCount", InvalidExternalSourcesCount.ToLiteral(triples));
            Assert("externalSourcesDuration", ExternalSourcesDuration.ToLiteral(triples));
            Assert("externalSourcesAverageDuration", ExternalSourcesAverageDuration.ToLiteral(triples));
            Assert("datasetsCount", DatasetsCount.ToLiteral(triples));
            Assert("newDatasetsCount", NewDatasets.ToLiteral(triples));
            Assert("modifiedDatasetsCount", ModifiedDatasets.ToLiteral(triples));
            Assert("removedDatasetsCount", RemovedDatasets.ToLiteral(triples));

            int problemsCount = Problems.Count + ExternalSources.Count(e => !e.Success);
            Assert("problemsCount", problemsCount.ToLiteral(triples));

            foreach (IExternalSourceHarvestionResult externalSourceResult in ExternalSources)
            {
                externalSourceResult.WriteTriples(triples, identifierNode, "hasSource");
            }

            foreach (HarvestationProblem harvestationProblem in Problems)
            {
                harvestationProblem.WriteTriples(triples, identifierNode, "hasProblem");
            }
        }

    }
}
