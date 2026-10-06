using Microsoft.AspNetCore.Authentication.JwtBearer;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System.Net;
using System.Net.Http.Json;
using TestBase;

namespace WebApi.Test
{
    public class AppSettingsTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public AppSettingsTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task AppSettingsShouldBeSavedBySuperadmin()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));

            AppSettings input = new AppSettings { AllowedIframeHosts = "example.com\ntest.sk" };
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/app-settings", requestContent);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(input.AllowedIframeHosts, applicationFactory.AppSettingsService.GetSettings().AllowedIframeHosts);
        }

        [Fact]
        public async Task AppSettingsShouldNotBeSavedByPublisher()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher"));

            using JsonContent requestContent = JsonContent.Create(new AppSettings { AllowedIframeHosts = "example.com" });
            using HttpResponseMessage response = await client.PutAsync("/app-settings", requestContent);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AppSettingsShouldNotBeSavedByAnonymous()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            using JsonContent requestContent = JsonContent.Create(new AppSettings { AllowedIframeHosts = "example.com" });
            using HttpResponseMessage response = await client.PutAsync("/app-settings", requestContent);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AppSettingsShouldBeReadBySuperadmin()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));

            string hosts = "example.com\ntest.sk";
            applicationFactory.AppSettingsService.SaveSettings(new AppSettings { AllowedIframeHosts = hosts });

            using HttpResponseMessage response = await client.GetAsync("/app-settings");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AppSettings? result = await response.Content.ReadFromJsonAsync<AppSettings>();
            Assert.NotNull(result);
            Assert.Equal(hosts, result.AllowedIframeHosts);
        }

        [Fact]
        public async Task AppSettingsShouldNotBeReadByPublisher()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher"));

            using HttpResponseMessage response = await client.GetAsync("/app-settings");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AppSettingsShouldNotBeReadByAnonymous()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync("/app-settings");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
