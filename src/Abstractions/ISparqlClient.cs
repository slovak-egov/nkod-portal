using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public interface ISparqlClient
    {
        Task<List<DcatDataset>> GetDatasets(Uri catalogId, bool trace = false);

        Task<List<DcatDistribution>> GetDistributions(Uri datasetId, bool trace = false);

        Task<Dictionary<string, bool>> GetDownloadQuality();
    }
}
