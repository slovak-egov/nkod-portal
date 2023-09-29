using IAMClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TestBase;

namespace WebApi.Test
{
    public class PublisherAdminTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        private IFileStorageAccessPolicy accessPolicy = new AllAccessFilePolicy();

        public PublisherAdminTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task PublisherActivateIsNotEnabledForAnonymousUser()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            
            PublisherInput input = new PublisherInput
            {
                PublisherId = id.ToString(),
                IsEnabled = true
            };

            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            FileState? state = storage.GetFileState(id, accessPolicy);
            Assert.NotNull(state);
            Assert.False(state.Metadata.IsPublic);
        }

        [Fact]
        public async Task PublisherActivateIsNotEnabledForPublisherAdmin()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            PublisherInput input = new PublisherInput
            {
                PublisherId = id.ToString(),
                IsEnabled = true
            };

            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            FileState? state = storage.GetFileState(id, accessPolicy);
            Assert.NotNull(state);
            Assert.False(state.Metadata.IsPublic);
        }

        [Fact]
        public async Task PublisherActivateIsEnabledForSuperadmin()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));

            PublisherInput input = new PublisherInput
            {
                PublisherId = id.ToString(),
                IsEnabled = true
            };

            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? state = storage.GetFileState(id, accessPolicy);
            Assert.NotNull(state);
            Assert.True(state.Metadata.IsPublic);
        }

        [Fact]
        public async Task PublisherDeactivateIsEnabledForSuperadmin()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: true);

            Guid datasetId = fixture.CreateDataset("Test", PublisherId);
            Guid catalogId = fixture.CreateLocalCatalog("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));

            PublisherInput input = new PublisherInput
            {
                PublisherId = id.ToString(),
                IsEnabled = false
            };

            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? state = storage.GetFileState(id, accessPolicy);
            Assert.NotNull(state);
            Assert.False(state.Metadata.IsPublic);

            state = storage.GetFileState(datasetId, accessPolicy);
            Assert.NotNull(state);
            Assert.False(state.Metadata.IsPublic);

            state = storage.GetFileState(catalogId, accessPolicy);
            Assert.NotNull(state);
            Assert.False(state.Metadata.IsPublic);
        }

        [Fact]
        public async Task PublisherShouldNotBeImpersonatedForAnonymous()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            using JsonContent requestContent = JsonContent.Create(new { });
            using HttpResponseMessage response = await client.PostAsync($"/publishers/impersonate?id={HttpUtility.UrlEncode(id.ToString())}", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PublisherShouldNotBeImpersonatedForPublisherAdmin()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new { });
            using HttpResponseMessage response = await client.PostAsync($"/publishers/impersonate?id={HttpUtility.UrlEncode(id.ToString())}", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PublisherShouldBeImpersonatedForSuperadmin()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));

            using JsonContent requestContent = JsonContent.Create(new { });
            using HttpResponseMessage response = await client.PostAsync($"/publishers/impersonate?id={HttpUtility.UrlEncode(id.ToString())}", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            TokenResult? result = JsonConvert.DeserializeObject<TokenResult>(content);
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.RefreshToken);
        }
    }
}
