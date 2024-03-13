using Microsoft.AspNetCore.Authentication.JwtBearer;
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
using Microsoft.AspNetCore.Http.HttpResults;
using System.Reflection.Metadata;
using TestBase;
using Newtonsoft.Json;

namespace DocumentStorageApi.Test
{
    public class SuperadminModificationTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public SuperadminModificationTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task FileShouldBeInserted()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, "http://localhost/publisher1", true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string newContent = $"<http://example.com/> <http://example.com/title> \"\"@sk .";
            InsertModel model = new InsertModel(newContent, metadata, false);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(newContent, newState.Content);
            Assert.Equal(metadata, newState.Metadata);
        }

        [Fact]
        public async Task FileShouldBeInsertedIfPublisherIsNull()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string newContent = $"<http://example.com/> <http://example.com/title> \"\"@sk .";
            InsertModel model = new InsertModel(newContent, metadata, false);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(newContent, newState.Content);
            Assert.Equal(metadata, newState.Metadata);
        }

        [Fact]
        public async Task FileContentShouldBeModified()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => !s.Metadata.IsPublic);
            string newContent = $"<http://example.com/> <http://example.com/title> \"\"@sk .";
            InsertModel model = new InsertModel(newContent, state.Metadata, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(newContent, newState.Content);
            Assert.Equal(state.Metadata, newState.Metadata);
        }

        [Fact]
        public async Task FileContentShouldBeModifiedIfPublisherIsNull()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && s.Metadata.Publisher is null);
            string newContent = $"<http://example.com/> <http://example.com/title> \"\"@sk .";
            InsertModel model = new InsertModel(newContent, state.Metadata, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(newContent, newState.Content);
            Assert.Equal(state.Metadata, newState.Metadata);
        }

        [Fact]
        public async Task FileContentAndMetadataShouldBeModified()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => !s.Metadata.IsPublic);
            string newContent = $"<http://example.com/> <http://example.com/title> \"\"@sk .";
            FileMetadata newMetadata = state.Metadata with { Publisher = "new-publisher" };
            InsertModel model = new InsertModel(newContent, newMetadata, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(newContent, newState.Content);
            Assert.Equal(newMetadata, newState.Metadata);
        }

        [Fact]
        public async Task FileContentAndMetadataShouldBeModifiedIfPublisherIsNull()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && s.Metadata.Publisher is null);
            string newContent = $"<http://example.com/> <http://example.com/title> \"\"@sk .";
            FileMetadata newMetadata = state.Metadata with { Publisher = "new-publisher" };
            InsertModel model = new InsertModel(newContent, newMetadata, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(newContent, newState.Content);
            Assert.Equal(newMetadata, newState.Metadata);
        }

        [Fact]
        public async Task FileMetadataShouldBeModified()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => !s.Metadata.IsPublic);
            FileMetadata metadata = state.Metadata with { Publisher = "new-publisher" };

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            using HttpResponseMessage response = await client.PostAsync($"/files/metadata", JsonContent.Create(metadata));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(state.Content, newState.Content);
            Assert.Equal(metadata, newState.Metadata);
        }

        [Fact]
        public async Task FileMetadataShouldBeModifiedIfPublisherIsNull()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && s.Metadata.Publisher is null);
            FileMetadata metadata = state.Metadata with { Publisher = "new-publisher" };

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            using HttpResponseMessage response = await client.PostAsync($"/files/metadata", JsonContent.Create(metadata));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(state.Content, newState.Content);
            Assert.Equal(metadata, newState.Metadata);
        }

        [Fact]
        public async Task FileShouldBeDeleted()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => !s.Metadata.IsPublic);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            using HttpResponseMessage response = await client.DeleteAsync($"/files/{state.Metadata.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Null(storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public async Task FileShouldBeDeletedIfPublisherIsNull()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && s.Metadata.Publisher is null);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            using HttpResponseMessage response = await client.DeleteAsync($"/files/{state.Metadata.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Null(storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public async Task FileShoudBeReplacedInIndex()
        {
            using Storage storage = new Storage(fixture.GetStoragePath(true, false));

            FoafAgent publisher = FoafAgent.Create(new Uri("http://data.gov.sk/publisher"));
            publisher.SetNames(new Dictionary<string, string> { { "sk", "Test" }, { "en", "Test" } });
            FileMetadata meatada = publisher.UpdateMetadata() with {  IsPublic = true };
            storage.InsertFile(publisher.ToString(), meatada, true, new AllAccessFilePolicy());

            publisher.SetNames(new Dictionary<string, string> { { "sk", "Test" }, { "en", "Test" } });
            InsertModel model = new InsertModel(publisher.ToString(), meatada, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));

            async Task VerifySingleResult()
            {
                FileStorageQuery query = new FileStorageQuery
                {
                    QueryText = "test",
                    Language = "sk",
                    OnlyTypes = new List<FileType> { FileType.PublisherRegistration },
                    OrderDefinitions = new List<FileStorageOrderDefinition> { new FileStorageOrderDefinition { Property = FileStorageOrderProperty.Relevance } }
                };

                using (HttpResponseMessage response = await client.PostAsync($"/files/query", JsonContent.Create(query)))
                {
                    string c = await response.Content.ReadAsStringAsync();
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    FileStorageResponse? storageResponse = JsonConvert.DeserializeObject<FileStorageResponse>(await response.Content.ReadAsStringAsync());
                    Assert.NotNull(storageResponse);
                    Assert.Single(storageResponse.Files);
                    Assert.Equal(1, storageResponse.TotalCount);
                }
            }

            await VerifySingleResult();

            using (HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model)))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            await VerifySingleResult();
        }
    }
}
