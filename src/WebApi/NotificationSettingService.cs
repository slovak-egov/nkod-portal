using NkodSk.Abstractions;

namespace WebApi
{
    public class NotificationSettingService : INotificationSettingService
    {
        private readonly IHttpClientFactory httpClientFactory;

        public const string HttpClientName = "NotificationSetting";

        public NotificationSettingService(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<NotificationSetting?> Read<T>(T data)
        {
            using HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.PostAsJsonAsync("/notification/get", data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<NotificationSetting>();
        }

        public async Task Send<T>(T data)
        {
            using HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.PostAsJsonAsync("/notification/set", data);
            response.EnsureSuccessStatusCode();
        }

        public Task UpdateSetting(string email, bool isDisabled)
        {
            return Send(new { Email = email, IsDisabled = isDisabled });
        }

        public Task UpdateSettingWithAuthKey(string authKey, bool isDisabled)
        {
            return Send(new { AuthKey = authKey, IsDisabled = isDisabled });
        }

        public Task<NotificationSetting?> GetCurrent(string email)
        {
            return Read(new { Email = email });
        }

        public Task<NotificationSetting?> GetCurrentWithAuthKey(string authKey)
        {
            return Read(new { AuthKey = authKey });
        }
    }
}
