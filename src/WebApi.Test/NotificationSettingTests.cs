using Microsoft.AspNetCore.Authentication.JwtBearer;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TestBase;

namespace WebApi.Test
{
    public class NotificationSettingTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public NotificationSettingTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        private async Task Send<T>(HttpClient client, T input, string? auth)
        {
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync($"/notification-setting?auth={HttpUtility.UrlEncode(auth)}", requestContent);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<NotificationSetting?> GetCurrent(HttpClient client, string? auth)
        {
            using HttpResponseMessage response = await client.GetAsync($"/notification-setting?auth={HttpUtility.UrlEncode(auth)}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<NotificationSetting>();
        }

        [Fact]
        public async Task NotificationShouldBeDisabledWithAuthKey()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            string email = "test@test.sk";
            string authKey = applicationFactory.NotificationSettingService.GetAuthKey(email);

            await Send(client, new { IsDisabled = true }, authKey);
            Assert.True(applicationFactory.NotificationSettingService.IsDisabled(email));
        }

        [Fact]
        public async Task NotificationShouldBeDisabledWithToken()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            string email = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", email: email));
                        
            string authKey = applicationFactory.NotificationSettingService.GetAuthKey(email);

            await Send(client, new { IsDisabled = true }, null);
            Assert.True(applicationFactory.NotificationSettingService.IsDisabled(email));
        }

        [Fact]
        public async Task NotificationShouldBeEnabledWithAuthKey()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            string email = "test@test.sk";
            string authKey = applicationFactory.NotificationSettingService.GetAuthKey(email);

            await Send(client, new { IsDisabled = false }, authKey);
            Assert.False(applicationFactory.NotificationSettingService.IsDisabled(email));
        }

        [Fact]
        public async Task NotificationShouldBeEnabledWithToken()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            string email = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", email: email));

            string authKey = applicationFactory.NotificationSettingService.GetAuthKey(email);

            await Send(client, new { IsDisabled = false }, null);
            Assert.False(applicationFactory.NotificationSettingService.IsDisabled(email));
        }

        [Fact]
        public async Task NotificationShouldBeReadEnabledWithAuthKey()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            string email = "test@test.sk";
            string authKey = applicationFactory.NotificationSettingService.GetAuthKey(email);

            NotificationSetting? current = await GetCurrent(client, authKey);
            Assert.NotNull(current);
            Assert.False(current.IsDisabled);
        }

        [Fact]
        public async Task NotificationShouldBeReadEnabledWithEmail()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            string email = "test@test.sk";
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", email: email));

            NotificationSetting? current = await GetCurrent(client, null);
            Assert.NotNull(current);
            Assert.False(current.IsDisabled);
        }

        [Fact]
        public async Task NotificationShouldBeReadDisabledWithAuthKey()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            string email = "test@test.sk";
            string authKey = applicationFactory.NotificationSettingService.GetAuthKey(email);
            await applicationFactory.NotificationSettingService.UpdateSetting(email, true);

            NotificationSetting? current = await GetCurrent(client, authKey);
            Assert.NotNull(current);
            Assert.True(current.IsDisabled);
        }

        [Fact]
        public async Task NotificationShouldBeReadDisabledWithEmail()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            string email = "test@test.sk";
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", email: email));
            await applicationFactory.NotificationSettingService.UpdateSetting(email, true);

            NotificationSetting? current = await GetCurrent(client, null);
            Assert.NotNull(current);
            Assert.True(current.IsDisabled);
        }
    }
}
