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

namespace DocumentStorageApi.Test
{
    public class PublisherModificationTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public PublisherModificationTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileShouldBeInsertedIfPublisherIsSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, "http://localhost/publisher1", true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string newContent = $"<http://example.com/> <http://example.com/title> \"\"@sk .";
            InsertModel model = new InsertModel(newContent, metadata, false);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(newContent, newState.Content);
            Assert.Equal(metadata, newState.Metadata);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileShouldNotBeInsertedIfPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, "http://localhost/publisher1", true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            InsertModel model = new InsertModel("test", metadata, false);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Null(storage.GetFileMetadata(metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileShouldNotBeInsertedIfPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            InsertModel model = new InsertModel("test", metadata, false);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Null(storage.GetFileMetadata(metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileContentShouldBeModifiedIfPublisherIsSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            string newContent = $"<http://example.com/> <http://example.com/title> \"\"@sk .";
            InsertModel model = new InsertModel(newContent, state.Metadata, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(newContent, newState.Content);
            Assert.Equal(state.Metadata, newState.Metadata);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileContentShouldNotBeModifiedIfPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            InsertModel model = new InsertModel("new-content", state.Metadata, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileContentShouldNotBeModifiedIfPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && s.Metadata.Publisher is null);
            InsertModel model = new InsertModel("new-content", state.Metadata, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileContentAndMetadataShouldBeModifiedIfPublisherIsSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            string newContent = $"<http://example.com/> <http://example.com/title> \"\"@sk .";
            FileMetadata newMetadata = state.Metadata with { Name = "new-name" };
            InsertModel model = new InsertModel(newContent, newMetadata, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(newContent, newState.Content);
            Assert.Equal(newMetadata, newState.Metadata);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileContentAndMetadataShouldNotBeModifiedIfPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            InsertModel model = new InsertModel("new-content", state.Metadata with { Name = "new-name" }, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileContentAndMetadataShouldNotBeModifiedIfPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && s.Metadata.Publisher is null);
            InsertModel model = new InsertModel("new-content", state.Metadata with { Name = "new-name" }, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileContentAndMetadataPublisherShouldNotBeModifiedIfOldPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            InsertModel model = new InsertModel("new-content", state.Metadata with { Publisher = state.Metadata.Publisher + "x" }, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileContentAndMetadataPublisherShouldNotBeModifiedIfOldPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && s.Metadata.Publisher is null);
            InsertModel model = new InsertModel("new-content", state.Metadata with { Publisher = state.Metadata.Publisher }, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileContentAndMetadataPublisherShouldNotBeModifiedIfNewPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            InsertModel model = new InsertModel("new-content", state.Metadata with { Publisher = state.Metadata.Publisher + "x" }, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileContentAndMetadataPublisherShouldNotBeModifiedIfNewPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            InsertModel model = new InsertModel("new-content", state.Metadata with { Publisher = null }, true);

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files", JsonContent.Create(model));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileMetadataShouldBeModifiedIfPublisherIsSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            FileMetadata metadata = state.Metadata with { Name = "new-name" };

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files/metadata", JsonContent.Create(metadata));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? newState = storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(newState);
            Assert.Equal(state.Content, newState.Content);
            Assert.Equal(metadata, newState.Metadata);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileMetadataShouldNotBeModifiedIfPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            FileMetadata metadata = state.Metadata with { Name = "new-name" };

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.PostAsync($"/files/metadata", JsonContent.Create(metadata));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileMetadataPublisherShouldNotBeModifiedIfOldPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            FileMetadata metadata = state.Metadata with { Publisher = state.Metadata.Publisher + "x" };

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.PostAsync($"/files/metadata", JsonContent.Create(metadata));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileMetadataPublisherShouldNotBeModifiedIfOldPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && s.Metadata.Publisher is null);
            FileMetadata metadata = state.Metadata with { Publisher = state.Metadata.Publisher };

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files/metadata", JsonContent.Create(metadata));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileMetadataPublisherShouldNotBeModifiedIfNewPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            FileMetadata metadata = state.Metadata with { Publisher = state.Metadata.Publisher + "x" };

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files/metadata", JsonContent.Create(metadata));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileMetadataPublisherShouldNotBeModifiedIfNewPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));
            FileMetadata metadata = state.Metadata with { Publisher = null };

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.PostAsync($"/files/metadata", JsonContent.Create(metadata));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileShouldBeDeletedIfPublisherIsSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher) && !fixture.ExistingStates.Any(c => c.Metadata.ParentFile == s.Metadata.Id));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.DeleteAsync($"/files/{state.Metadata.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Null(storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileShouldNotBeDeletedIfPublisherIsNotSame(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher) && !fixture.ExistingStates.Any(c => c.Metadata.ParentFile == s.Metadata.Id));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher + "x"));
            using HttpResponseMessage response = await client.DeleteAsync($"/files/{state.Metadata.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileShouldNotBeDeletedIfPublisherIsNull(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState state = fixture.ExistingStates.First(s => s.Metadata.IsPublic && s.Metadata.Publisher is null && !fixture.ExistingStates.Any(c => c.Metadata.ParentFile == s.Metadata.Id));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, state.Metadata.Publisher));
            using HttpResponseMessage response = await client.DeleteAsync($"/files/{state.Metadata.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Equal(state, storage.GetFileState(state.Metadata.Id, StaticAccessPolicy.Allow));
        }
    }
}
