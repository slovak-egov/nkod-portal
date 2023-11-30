using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NkodSk.Abstractions
{
    public class SparqlClient
    {
        private readonly HttpClient client;

        public SparqlClient(HttpClient client)
        {
            this.client = client;
        }

        private async Task<string> GetResponse(string query)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"?query={HttpUtility.UrlEncode(query)}");
            request.Headers.Add("Accept", "application/sparql-results+json,*/*;q=0.9");
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<DcatDataset>> GetDatasets(Uri catalogId)
        {
            RdfDocument document = RdfDocument.Load(await GetResponse(""));
            return document.Datasets;
        }

        public async Task<List<DcatDistribution>> GetDistributions(Uri datasetId)
        {
            RdfDocument document = RdfDocument.Load(await GetResponse(""));
            return document.Distributions;
        }
    }
}
