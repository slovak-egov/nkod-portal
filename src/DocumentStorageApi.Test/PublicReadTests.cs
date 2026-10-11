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
using TestBase;

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
            string content = await response.Content.ReadAsStringAsync();
            Assert.Equal(Encoding.UTF8.GetByteCount(content), response.Content.Headers.ContentLength);
            Assert.Equal(expected.Content, content);
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
            string c = await response.Content.ReadAsStringAsync();
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

        [Fact]
        public async Task PublishersWithQueryTextShouldBeOrderedAndPaged()
        {
            using Storage storage = new Storage(fixture.GetStoragePath(true, false));

            Dictionary<string, Guid> idsByName = new Dictionary<string, Guid>();
            void CreatePublisher(int index, string name)
            {
                FoafAgent publisher = FoafAgent.Create(new Uri($"http://data.gov.sk/publisher/{index}"));
                publisher.SetNames(new Dictionary<string, string> { { "sk", name } });
                FileMetadata metadata = publisher.UpdateMetadata() with { IsPublic = true };
                storage.InsertFile(publisher.ToString(), metadata, true, new AllAccessFilePolicy());
                idsByName[name] = metadata.Id;
            }

            for (int i = 0; i <= 10; i++)
            {
                CreatePublisher(i, $"Test {i:00}");
            }
            CreatePublisher(11, "Ministerstvo");

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            async Task<FileStorageGroupResponse> Search(string queryText, int skip, int? max, bool reverseOrder = false, List<Guid>? onlyIds = null)
            {
                FileStorageQuery query = new FileStorageQuery
                {
                    QueryText = queryText,
                    Language = "sk",
                    SkipResults = skip,
                    MaxResults = max,
                    OnlyIds = onlyIds,
                    OrderDefinitions = new List<FileStorageOrderDefinition> { new FileStorageOrderDefinition(FileStorageOrderProperty.Name, reverseOrder) }
                };

                using HttpResponseMessage response = await client.PostAsync("/files/by-publisher", JsonContent.Create(query));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                FileStorageGroupResponse? groupResponse = JsonConvert.DeserializeObject<FileStorageGroupResponse>(await response.Content.ReadAsStringAsync());
                Assert.NotNull(groupResponse);
                return groupResponse;
            }

            static IEnumerable<string> Names(FileStorageGroupResponse response) => response.Groups.Select(g => g.PublisherFileState!.Metadata.Name.GetText("sk")!);

            FileStorageGroupResponse result = await Search("Test", 0, 10);
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(Enumerable.Range(0, 10).Select(i => $"Test {i:00}"), Names(result));

            result = await Search("Test", 10, 10);
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(new[] { "Test 10" }, Names(result));

            result = await Search("Test", 0, 3, reverseOrder: true);
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(new[] { "Test 10", "Test 09", "Test 08" }, Names(result));

            result = await Search(" Test ", 0, null);
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(11, result.Groups.Count);

            result = await Search("Ministerstvo", 0, 10);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(new[] { "Ministerstvo" }, Names(result));

            result = await Search("Neexistuje", 0, 10);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Groups);

            result = await Search("Test", 0, 10, onlyIds: new List<Guid> { idsByName["Test 05"], idsByName["Ministerstvo"] });
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(new[] { "Test 05" }, Names(result));

            result = await Search("Test", 0, 10, onlyIds: new List<Guid> { idsByName["Ministerstvo"] });
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Groups);
        }
    }
}
