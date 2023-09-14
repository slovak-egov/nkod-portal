using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace WebApi.Test
{
    public class DatasetSearchTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        public DatasetSearchTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task TestEmptySearch()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using JsonContent requestContent = JsonContent.Create(new { });
            using HttpResponseMessage response = await client.PostAsync("/datasets/search", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            AbstractResponse<DatasetView>? result = JsonConvert.DeserializeObject<AbstractResponse<DatasetView>>(content);
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task TestOneResult()
        {
            string path = fixture.GetStoragePath();
            Guid id = fixture.CreateDataset("Test", PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using JsonContent requestContent = JsonContent.Create(new { });
            using HttpResponseMessage response = await client.PostAsync("/datasets/search", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            AbstractResponse<DatasetView>? result = JsonConvert.DeserializeObject<AbstractResponse<DatasetView>>(content);
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);
            DatasetView view = result.Items[0];
            Assert.Equal(id, view.Id);
            Assert.True(view.IsPublic);
            Assert.Equal("Test", view.Name);
            Assert.Equal(PublisherId, view.PublisherId);
        }

        [Fact]
        public async Task TestCreateUnauthorized()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using JsonContent requestContent = JsonContent.Create(new
            {
                Name = new { sk = "Test" }
            });
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestCreate()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            using JsonContent requestContent = JsonContent.Create(new {
                Name = new { sk = "Test"}
            });
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);

        }
    }
}
