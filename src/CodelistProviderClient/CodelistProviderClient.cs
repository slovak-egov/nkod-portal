using Abstractions;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using System.Web;
using static Lucene.Net.Search.FieldCache;

namespace CodelistProviderClient
{
    public class CodelistProviderClient : ICodelistProviderClient
    {
        private readonly IHttpClientFactory httpClientFactory;

        private readonly IHttpContextValueAccessor httpContextAccessor;

        private readonly Dictionary<string, Codelist> codelists = new Dictionary<string, Codelist>();

        public const string HttpClientName = "CodelistProvider";

        public CodelistProviderClient(IHttpClientFactory httpClientFactory, IHttpContextValueAccessor httpContextAccessor)
        {
            this.httpClientFactory = httpClientFactory;
            this.httpContextAccessor = httpContextAccessor;
        }

        private HttpClient CreateClient()
        {
            HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            string? token = httpContextAccessor.Token;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        public async Task<List<Codelist>> GetCodelists()
        {
            HttpClient client = CreateClient();
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

            HttpClient client = CreateClient();
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

        // Item is verified by the codelist provider with one request, codelists may be too large to download for a single value
        public async Task<CodelistItem?> GetCodelistItem(string codelistId, string itemId)
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"codelists/item?key={HttpUtility.UrlEncode(codelistId)}&id={HttpUtility.UrlEncode(itemId)}");
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<CodelistItem>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            throw new Exception("Invalid response");
        }

        public async Task<bool> UpdateCodelist(Stream stream)
        {
            codelists.Clear();

            HttpClient client = CreateClient();

            using MultipartFormDataContent requestContent = new MultipartFormDataContent
            {
                { new StreamContent(stream), "file", "codelist.ttl" }
            };

            using HttpResponseMessage response = await client.PutAsync("codelists", requestContent);
            response.EnsureSuccessStatusCode();
            return true;
        }
    }
}