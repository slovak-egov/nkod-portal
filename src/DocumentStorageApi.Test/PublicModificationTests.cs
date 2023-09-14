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
    public class PublicModificationTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public PublicModificationTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task FileShouldNotBeInserted()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            InsertModel model = new InsertModel("test", metadata, false);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            Assert.Null(storage.GetFileMetadata(metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public async Task FileContentShouldNotBeModified()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic);
            InsertModel model = new InsertModel("new-content", state.Metadata, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public async Task FileContentAndMetadataShouldNotBeModified()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic);
            InsertModel model = new InsertModel("new-content", state.Metadata with { Publisher = "new-publisher" }, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public async Task FileMetadataShouldNotBeModified()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic);
            FileMetadata metadata = state.Metadata with { Publisher = "new-publisher" };

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.PostAsync($"/files/metadata", JsonContent.Create(metadata));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public async Task FileShouldNotBeDeleted()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.DeleteAsync($"/files/{state.Metadata.Id}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }
    }
}
