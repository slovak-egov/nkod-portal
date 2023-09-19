using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    public class LocalCatalogModificationTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherAccessPolicy(PublisherId);

        public LocalCatalogModificationTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        private LocalCatalogInput CreateInput(bool withOptionalProperties = false)
        {
            LocalCatalogInput input = new LocalCatalogInput
            {
                Name = new Dictionary<string, string>
                {
                    { "sk", "TestName" }
                },
                Description = new Dictionary<string, string>
                {
                    { "sk", "TestDescription" }
                },
                IsPublic = true
            };

            if (withOptionalProperties)
            {
                input.Name["en"] = "TestNameEn";
                input.Description["en"] = "TestNameEn";
                input.ContactName = new Dictionary<string, string>
                {
                    { "sk", "TestContentName" },
                    { "en", "TestContentNameEn" },
                };
                input.ContactEmail = "contact@example.com";
                input.HomePage = "https://data.gov.sk/";
            }

            return input;
        }

        private void ValidateValues(Storage storage, string? id, LocalCatalogInput input, string publisher)
        {
            Assert.NotNull(id);
            FileState? state = storage.GetFileState(Guid.Parse(id), accessPolicy);
            Assert.NotNull(state);
            Assert.NotNull(state.Content);
            Assert.Equal(input.IsPublic, state.Metadata.IsPublic);
            Assert.Equal(publisher, state.Metadata.Publisher);
            Assert.Equal(FileType.LocalCatalogRegistration, state.Metadata.Type);
            Assert.Null(state.Metadata.ParentFile);
            Assert.Equal("TestName", state.Metadata.Name["sk"]);
            Assert.True((DateTimeOffset.Now - state.Metadata.Created).Duration().TotalMinutes < 1);
            Assert.True((DateTimeOffset.Now - state.Metadata.LastModified).Duration().TotalMinutes < 1);

            DcatCatalog? catalog = DcatCatalog.Parse(state.Content);
            Assert.NotNull(catalog);

            Extensions.AssertTextsEqual(input.Name, catalog.Title);
            Extensions.AssertTextsEqual(input.Description, catalog.Description);
            Extensions.AssertTextsEqual(input.ContactName, catalog.ContactPoint?.Name);
            Assert.Equal(input.ContactEmail, catalog.ContactPoint?.Email);
            Assert.Equal(input.HomePage, catalog.HomePage?.ToString());
        }

        [Fact]
        public async Task TestCreateUnauthorized()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using JsonContent requestContent = JsonContent.Create(CreateInput());
            using HttpResponseMessage response = await client.PostAsync("/local-catalogs", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestCreateMinimal()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            LocalCatalogInput input = CreateInput();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/local-catalogs", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, PublisherId);
        }

        [Fact]
        public async Task TestCreateMinimalNonPublic()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            LocalCatalogInput input = CreateInput();
            input.IsPublic = false;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/local-catalogs", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, PublisherId);
        }

        [Fact]
        public async Task TestCreateExtended()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            LocalCatalogInput input = CreateInput(true);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/local-catalogs", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, PublisherId);
        }

        [Fact]
        public async Task TestModifyUnauthorized()
        {
            string path = fixture.GetStoragePath();
            (Guid catalogId, Guid publisherId) = fixture.CreateFullLocalCatalog(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            LocalCatalogInput input = CreateInput();
            input.Id = catalogId.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/local-catalogs", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestModify()
        {
            string path = fixture.GetStoragePath();
            (Guid catalogId, Guid publisherId) = fixture.CreateFullLocalCatalog(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            LocalCatalogInput input = CreateInput(true);
            input.Id = catalogId.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/local-catalogs", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, PublisherId);
        }

        [Fact]
        public async Task TestModifyNonPublic()
        {
            string path = fixture.GetStoragePath();
            (Guid catalogId, Guid publisherId) = fixture.CreateFullLocalCatalog(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            LocalCatalogInput input = CreateInput(true);
            input.Id = catalogId.ToString();
            input.IsPublic = false;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/local-catalogs", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, PublisherId);
        }

        [Fact]
        public async Task TestModifyOtherPublisher()
        {
            string path = fixture.GetStoragePath();
            (Guid catalogId, Guid publisherId) = fixture.CreateFullLocalCatalog(PublisherId + "1");
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            LocalCatalogInput input = CreateInput();
            input.Id = catalogId.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/local-catalogs", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TestDeleteUnauthorized()
        {
            string path = fixture.GetStoragePath();
            (Guid catalogId, Guid publisherId) = fixture.CreateFullLocalCatalog(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.DeleteAsync($"/local-catalogs?id={HttpUtility.UrlEncode(catalogId.ToString())}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            Assert.NotNull(storage.GetFileMetadata(catalogId, accessPolicy));
        }

        [Fact]
        public async Task TestDelete()
        {
            string path = fixture.GetStoragePath();
            (Guid catalogId, Guid publisherId) = fixture.CreateFullLocalCatalog(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            using HttpResponseMessage response = await client.DeleteAsync($"/local-catalogs?id={HttpUtility.UrlEncode(catalogId.ToString())}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Null(storage.GetFileMetadata(catalogId, accessPolicy));
        }

        [Fact]
        public async Task TestDeleteOtherPublisher()
        {
            string path = fixture.GetStoragePath();
            (Guid catalogId, Guid publisherId) = fixture.CreateFullLocalCatalog(PublisherId + "1");
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            using HttpResponseMessage response = await client.DeleteAsync($"/local-catalogs?id={HttpUtility.UrlEncode(catalogId.ToString())}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.NotNull(storage.GetFileMetadata(catalogId, accessPolicy));
        }
    }
}
