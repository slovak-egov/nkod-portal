using Abstractions;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using System.Web;

namespace CodelistProviderClient
{
    public class CodelistProviderClient : ICodelistProviderClient
    {
        private readonly IHttpClientFactory httpClientFactory;

        private readonly Dictionary<string, Codelist> codelists = new Dictionary<string, Codelist>();

        public const string HttpClientName = "CodelistProvider";

        public CodelistProviderClient(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<List<Codelist>> GetCodelists()
        {
            HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.GetAsync("codelists");
            response.EnsureSuccessStatusCode();
            List<Codelist> codelists = JsonConvert.DeserializeObject<List<Codelist>>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                ?? throw new Exception("Invalid response");
            foreach (Codelist codelist in codelists)
            {
                this.codelists[codelist.Id] = codelist;
            }
            return codelists;
        }

        public async Task<Codelist?> GetCodelist(string id)
        {
            Codelist? codelist;
            if (codelists.TryGetValue(id, out codelist))
            {
                return codelist;
            }

            HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.GetAsync($"codelists/{HttpUtility.UrlEncode(id)}");
            if (response.IsSuccessStatusCode)
            {
                codelist = JsonConvert.DeserializeObject<Codelist>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                if (codelist is not null)
                {
                    codelists[id] = codelist;
                    return codelist;
                }
            } 
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            throw new Exception("Invalid response");
        }

        public async Task<CodelistItem?> GetCodelistItem(string codelistId, string itemId)
        {
            Codelist? codelist = await GetCodelist(codelistId).ConfigureAwait(false);
            if (codelist is not null)
            {
                if (codelist.Items.TryGetValue(itemId, out CodelistItem? codelistItem))
                {
                    return codelistItem;
                }
            }
            return null;
        }
    }
}