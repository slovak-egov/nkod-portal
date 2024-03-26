using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class TestSparqlClient : ISparqlClient
    {
        private readonly Dictionary<Uri, List<DcatDataset>> allDatasets = new Dictionary<Uri, List<DcatDataset>>();

        private readonly Dictionary<Uri, List<DcatDistribution>> allDistributions = new Dictionary<Uri, List<DcatDistribution>>();

        public Task<List<DcatDataset>> GetDatasets(Uri catalogId, bool _)
        {
            allDatasets.TryGetValue(catalogId, out List<DcatDataset>? datasets);
            datasets ??= new List<DcatDataset>();
            return Task.FromResult(datasets);
        }

        public Task<List<DcatDistribution>> GetDistributions(Uri datasetId, bool _)
        {
            allDistributions.TryGetValue(datasetId, out List<DcatDistribution>? distributions);
            distributions ??= new List<DcatDistribution>();
            return Task.FromResult(distributions);
        }

        public void Add(Uri parent, DcatDataset dataset)
        {
            if (!allDatasets.TryGetValue(parent, out List<DcatDataset>? datasets))
            {
                datasets = new List<DcatDataset>();
                allDatasets[parent] = datasets;
            }
            datasets.Add(dataset);
        }

        public void Add(Uri parent, DcatDistribution distribution)
        {
            if (!allDistributions.TryGetValue(parent, out List<DcatDistribution>? distributions))
            {
                distributions = new List<DcatDistribution>();
                allDistributions[parent] = distributions;
            }
            distributions.Add(distribution);
        }

        public void Clear()
        {
            allDatasets.Clear();
            allDistributions.Clear();
        }
    }
}
