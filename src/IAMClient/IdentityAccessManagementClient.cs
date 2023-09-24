using Newtonsoft.Json;
using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace IAMClient
{
    public class IdentityAccessManagementClient : IIdentityAccessManagementClient
    {
        private readonly IHttpClientFactory httpClientFactory;

        private readonly IHttpContextValueAccessor httpContextAccessor;

        public const string HttpClientName = "IAM";

        public IdentityAccessManagementClient(IHttpClientFactory httpClientFactory, IHttpContextValueAccessor httpContextAccessor)
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

        public async Task<SaveResult> CreateUser(NewUserInput input)
        {
            HttpClient client = CreateClient();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync($"/users", requestContent);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<SaveResult>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                ?? throw new HttpRequestException("Invalid response");
        }

        public async Task DeleteUser(string id)
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.DeleteAsync($"/users?id={HttpUtility.UrlEncode(id)}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<UserInfoResult> GetUsers(UserInfoQuery query)
        {
            int? limit = query.PageSize;
            int? offset = query.Page.HasValue && query.PageSize.HasValue ? (query.Page.Value - 1) * query.PageSize.Value : 0;
            string? id = query.Id;

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (limit.HasValue)
            {
                parameters["limit"] = limit.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (offset.HasValue)
            {
                parameters["offset"] = offset.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (id is not null)
            {
                parameters["id"] = id;
            }

            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/users?{(string.Join("&", parameters.Select(k => $"{HttpUtility.UrlEncode(k.Key)}={HttpUtility.UrlEncode(k.Value)}")))}");
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<UserInfoResult>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                ?? throw new HttpRequestException("Invalid response");
        }

        public async Task<SaveResult> UpdateUser(EditUserInput input)
        {
            HttpClient client = CreateClient();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync($"/users", requestContent);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<SaveResult>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                ?? throw new HttpRequestException("Invalid response");
        }

        public async Task<TokenResult> RefreshToken(string token, string refreshToken)
        {
            HttpClient client = CreateClient();
            using JsonContent requestContent = JsonContent.Create(new { AccessToken = token, RefreshToken = refreshToken });
            using HttpResponseMessage response = await client.PostAsync($"/refresh", requestContent);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<TokenResult>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                ?? throw new HttpRequestException("Invalid response");
        }

        public async Task Logout()
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync("/logout");
            response.EnsureSuccessStatusCode();
        }

        public async Task<TokenResult> DelegatePublisher(string publisherId)
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.PostAsync($"/delegate-publisher?publisher={HttpUtility.HtmlEncode(publisherId)}", null);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<TokenResult>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                ?? throw new HttpRequestException("Invalid response");
        }

        public async Task<UserInfo> GetUserInfo()
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/user-info");
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<UserInfo>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                ?? throw new HttpRequestException("Invalid response");
        }

        public async Task<DelegationAuthorizationResult> GetLogin()
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/login");
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<DelegationAuthorizationResult>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                ?? throw new HttpRequestException("Invalid response");
        }

        public async Task<TokenResult> Consume(string content)
        {
            HttpClient client = CreateClient();
            using StringContent requestContent = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded");
            using HttpResponseMessage response = await client.PostAsync($"/consume", requestContent);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<TokenResult>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                ?? throw new HttpRequestException("Invalid response");
        }
    }
}
