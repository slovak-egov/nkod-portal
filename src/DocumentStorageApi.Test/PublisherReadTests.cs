using Newtonsoft.Json;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using RdfFileStorage.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Data;
using TestBase;

namespace DocumentStorageApi.Test
{
    public class PublisherReadTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public PublisherReadTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task PublicFileShouldBeRead(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => s.Metadata.IsPublic);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, "http://localhost/publisher1"));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            FileState? state = JsonConvert.DeserializeObject<FileState>(await response.Content.ReadAsStringAsync());
            Assert.Equal(expected, state);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileShouldNotBeReadIfPublisherIsSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, expected.Metadata.Publisher));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            FileState? state = JsonConvert.DeserializeObject<FileState>(await response.Content.ReadAsStringAsync());
            Assert.Equal(expected, state);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileShouldNotBeReadIfPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, expected.Metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileShouldNotBeReadIfPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && s.Metadata.Publisher is null);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, expected.Metadata.Publisher));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task PublicFileContentShouldBeRead(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, "http://localhost/publisher1"));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/content");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected.Content, await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileContentShouldBeReadIfPublisherIsSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, expected.Metadata.Publisher));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/content");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected.Content, await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileContentShouldNotBeReadIfPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, expected.Metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/content");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileContentShouldNotBeReadIfPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && s.Metadata.Publisher is null);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, expected.Metadata.Publisher));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/content");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task PublicFileMetadataShouldBeRead(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, "http://localhost/publisher1"));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/metadata");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected.Metadata, JsonConvert.DeserializeObject<FileMetadata>(await response.Content.ReadAsStringAsync()));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileMetadataShouldNotBeReadIfPublisherIsSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, expected.Metadata.Publisher));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/metadata");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected.Metadata, JsonConvert.DeserializeObject<FileMetadata>(await response.Content.ReadAsStringAsync()));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileMetadataShouldNotBeReadIfPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, expected.Metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/metadata");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileMetadataShouldNotBeReadIfPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && s.Metadata.Publisher is null);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, expected.Metadata.Publisher));
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/metadata");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task OnlyPublicAndPublisherFileShouldBeInListResultWithoutParameters(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            string publisher = fixture.ExistingStates.Select(s => s.Metadata.Publisher).Where(s => !string.IsNullOrEmpty(s)).First()!;

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, publisher));

            using HttpResponseMessage response = await client.PostAsync($"/files/query", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            FileStorageResponse? storageResponse = JsonConvert.DeserializeObject<FileStorageResponse>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(storageResponse);
            FileStorageResponse expected = fixture.ExistingStates.GetResponse(new FileStorageQuery(), new PublisherFileAccessPolicy(publisher));
            Assert.Equal(expected.TotalCount, storageResponse.TotalCount);
            Assert.Equal(expected.Files, storageResponse.Files);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task OnlyPublicAndPublisherFileShouldBeInListResultWithQueryParameters(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            string publisher = fixture.ExistingStates.Select(s => s.Metadata.Publisher).Where(s => !string.IsNullOrEmpty(s)).First()!;

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, publisher));

            FileStorageQuery query = new FileStorageQuery
            {
                SkipResults = 4,
                MaxResults = 3,
                OnlyTypes = new List<FileType> { FileType.DatasetRegistration },
                OnlyIds = fixture.ExistingStates.Skip(10).Select(s => s.Metadata.Id).ToList(),
                ParentFile = fixture.ExistingStates[0].Metadata.Id,
                OnlyPublishers = new List<string> { fixture.ExistingStates[0].Metadata.Publisher! }
            };
            query.OrderDefinitions = new List<FileStorageOrderDefinition> { new FileStorageOrderDefinition(FileStorageOrderProperty.Created, false) };

            using HttpResponseMessage response = await client.PostAsync($"/files/query", JsonContent.Create(query));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            FileStorageResponse? storageResponse = JsonConvert.DeserializeObject<FileStorageResponse>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(storageResponse);
            FileStorageResponse expected = fixture.ExistingStates.GetResponse(query, new PublisherFileAccessPolicy(publisher));
            Assert.Equal(expected.TotalCount, storageResponse.TotalCount);
            Assert.Equal(expected.Files, storageResponse.Files);
            Assert.All(expected.Files, f => Assert.True(f.Metadata.IsPublic));
        }
    }
}
