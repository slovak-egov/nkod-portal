using Abstractions;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MonitoringSettingsClient
{
    public class MonitoringSettingsClient
    {
        private readonly IHttpClientFactory httpClientFactory;

        private readonly IHttpContextValueAccessor httpContextAccessor;

        public const string HttpClientName = "MonitoringSettings";

        public MonitoringSettingsClient(IHttpClientFactory httpClientFactory, IHttpContextValueAccessor httpContextAccessor)
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

        public async Task<MonitoringNotificationSettings?> GetSettingsForUser()
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync("settings");
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<MonitoringNotificationSettings?>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            return null;
        }

        public async Task<MonitoringNotificationSettings?> GetSettingsForLocalCatalog(string id)
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"settings?name={HttpUtility.UrlEncode(id)}");
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<MonitoringNotificationSettings?>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            return null;
        }

        public async Task SaveSettigs(MonitoringNotificationSettings? settings)
        {
            HttpClient client = CreateClient();
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "settings");
            if (settings is not null)
            {
                request.Content = JsonContent.Create(settings);
            }
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<MonitoringNotificationSettings>> GetSettings()
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync("all");
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<List<MonitoringNotificationSettings>>(await response.Content.ReadAsStringAsync().ConfigureAwait(false)) ?? [];
        }
    }
}
