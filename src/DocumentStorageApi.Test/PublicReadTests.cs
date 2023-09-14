using Lucene.Net.Search;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using RdfFileStorage.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace DocumentStorageApi.Test
{
    public class PublicReadTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public PublicReadTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task PublicFileShouldBeRead()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => s.Metadata.IsPublic);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            FileState? state = JsonConvert.DeserializeObject<FileState>(await response.Content.ReadAsStringAsync());
            Assert.Equal(expected, state);
        }

        [Fact]
        public async Task NonPublicFileShouldNotBeRead()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PublicFileContentShouldBeRead()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => s.Metadata.IsPublic);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/content");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected.Content, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task NonPublicFileContentShouldNotBeRead()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/content");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PublicFileMetadataShouldBeRead()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => s.Metadata.IsPublic);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/metadata");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected.Metadata, JsonConvert.DeserializeObject<FileMetadata>(await response.Content.ReadAsStringAsync()));
        }

        [Fact]
        public async Task NonPublicFileMetadataShouldNotBeRead()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/files/{expected.Metadata.Id}/metadata");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task OnlyPublicFilesHouldBeInListResultWithoutParameters()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            using HttpResponseMessage response = await client.PostAsync($"/files/query", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            FileStorageResponse? storageResponse = JsonConvert.DeserializeObject<FileStorageResponse>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(storageResponse);
            FileStorageResponse expected = fixture.ExistingStates.GetResponse(new FileStorageQuery(), StaticAccessPolicy.Deny);
            Assert.Equal(expected.TotalCount, storageResponse.TotalCount);
            Assert.Equal(expected.Files, storageResponse.Files);
        }

        [Fact]
        public async Task OnlyPublicFilesHouldBeInListResultWithQueryParameters()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

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
            FileStorageResponse expected = fixture.ExistingStates.GetResponse(query, StaticAccessPolicy.Deny);
            Assert.Equal(expected.TotalCount, storageResponse.TotalCount);
            Assert.Equal(expected.Files, storageResponse.Files);
            Assert.All(expected.Files, f => Assert.True(f.Metadata.IsPublic));
        }
    }
}
