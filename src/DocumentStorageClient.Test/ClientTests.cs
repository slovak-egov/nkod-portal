using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentStorageApi.Test;
using Microsoft.AspNetCore.Http;
using NkodSk.Abstractions;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Policy;
using TestBase;

namespace DocumentStorageClient.Test
{
    public class ClientTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public ClientTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }
        
        [Fact]
        public async Task PublicFileStateShouldBeReturnedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic)!;
            FileState? fileState = await client.GetFileState(expected.Metadata.Id);
            Assert.NotNull(fileState);
            Assert.Equal(expected, fileState);
        }

        [Fact]
        public async Task NonPublicFileStateShouldNotBeReturnedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => !s.Metadata.IsPublic)!;
            FileState? fileState = await client.GetFileState(expected.Metadata.Id);
            Assert.Null(fileState);
        }

        [Fact]
        public async Task FileStatesShouldBeReturnedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            List<FileState> expected = fixture.ExistingStates.Where(s => s.Metadata.IsPublic && s.Metadata.Type == FileType.DatasetRegistration).ToList();
            FileStorageResponse response = await client.GetFileStates(new FileStorageQuery { MaxResults = 3, SkipResults = 2, OnlyTypes = new List<FileType> { FileType.DatasetRegistration } });
            Assert.NotNull(response);
            Assert.True(response.TotalCount > 0);
            Assert.Equal(3, response.Files.Count);
            Assert.Equal(expected.Count, response.TotalCount);
            Assert.True(new HashSet<Guid>(expected.Skip(2).Take(3).Select(f => f.Metadata.Id)).SetEquals(response.Files.Select(f => f.Metadata.Id)));
        }

        [Fact]
        public async Task PublicFileMetadataShouldBeReturnedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic)!;
            FileMetadata? fileMetadata = await client.GetFileMetadata(expected.Metadata.Id);
            Assert.NotNull(fileMetadata);
            Assert.Equal(expected.Metadata, fileMetadata);
        }

        [Fact]
        public async Task NonPublicFileMetadataShouldNotBeReturnedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => !s.Metadata.IsPublic)!;
            FileMetadata? fileMetadata = await client.GetFileMetadata(expected.Metadata.Id);
            Assert.Null(fileMetadata);
        }

        [Fact]
        public async Task PublicFileDownloadShouldBeReturnedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic)!;
            Stream? stream = await client.DownloadStream(expected.Metadata.Id);
            Assert.NotNull(stream);
            Assert.Equal(expected.Content, new StreamReader(stream).ReadToEnd());
        }

        [Fact]
        public async Task NonPublicFileDownloadShouldNotBeReturnedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => !s.Metadata.IsPublic)!;
            Stream? stream = await client.DownloadStream(expected.Metadata.Id);
            Assert.Null(stream);
        }

        [Fact]
        public async Task StreamShouldNotBeUploadedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            string content = "<http://example.com/> <http://example.com/title> \"\"@sk .";
            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionFile, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(() => client.UploadStream(stream, metadata, true));
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, e.StatusCode);
        }

        [Fact]
        public async Task FileShouldNotBeUploadedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            string content = "<http://example.com/> <http://example.com/title> \"\"@sk .";
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionFile, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);            
            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(() => client.InsertFile(content, true, metadata));
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, e.StatusCode);
        }

        [Fact]
        public async Task FileMetadataShouldNotBeUploadedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic)!;
            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(() => client.UpdateMetadata(expected.Metadata));
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, e.StatusCode);
        }

        [Fact]
        public async Task FilShouldNotBeDeletedForAnonymousUser()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor();
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic)!;
            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(() => client.DeleteFile(expected.Metadata.Id));
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, e.StatusCode);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task PublicFileStateShouldBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher))!;

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, expected.Metadata.Publisher);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState? fileState = await client.GetFileState(expected.Metadata.Id);
            Assert.NotNull(fileState);
            Assert.Equal(expected, fileState);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileStateShouldBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => !s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher))!;

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, expected.Metadata.Publisher);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState? fileState = await client.GetFileState(expected.Metadata.Id);
            Assert.NotNull(fileState);
            Assert.Equal(expected, fileState);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileStatesShouldBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            string publisher = fixture.ExistingStates.First(s => !string.IsNullOrEmpty(s.Metadata.Publisher)).Metadata.Publisher!;

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, publisher);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            List<FileState> expected = fixture.ExistingStates.Where(s => (s.Metadata.IsPublic || s.Metadata.Publisher == publisher) &&  s.Metadata.Type == FileType.DatasetRegistration).ToList();
            FileStorageResponse response = await client.GetFileStates(new FileStorageQuery { MaxResults = 3, SkipResults = 2, OnlyTypes = new List<FileType> { FileType.DatasetRegistration } });
            Assert.NotNull(response);
            Assert.True(response.TotalCount > 0);
            Assert.Equal(3, response.Files.Count);
            Assert.Equal(expected.Count, response.TotalCount);
            Assert.True(new HashSet<Guid>(expected.Skip(2).Take(3).Select(f => f.Metadata.Id)).SetEquals(response.Files.Select(f => f.Metadata.Id)));
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task PublicFileMetadataShouldBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState expected = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, expected.Metadata.Publisher);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileMetadata? fileMetadata = await client.GetFileMetadata(expected.Metadata.Id);
            Assert.NotNull(fileMetadata);
            Assert.Equal(expected.Metadata, fileMetadata);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileMetadataShouldBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, expected.Metadata.Publisher);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileMetadata? fileMetadata = await client.GetFileMetadata(expected.Metadata.Id);
            Assert.NotNull(fileMetadata);
            Assert.Equal(expected.Metadata, fileMetadata);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task PublicFileDownloadShouldBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState expected = fixture.ExistingStates.First(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, expected.Metadata.Publisher);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            Stream? stream = await client.DownloadStream(expected.Metadata.Id);
            Assert.NotNull(stream);
            Assert.Equal(expected.Content, new StreamReader(stream).ReadToEnd());
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task NonPublicFileDownloadShouldBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState expected = fixture.ExistingStates.First(s => !s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher));

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, expected.Metadata.Publisher);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            Stream? stream = await client.DownloadStream(expected.Metadata.Id);
            Assert.NotNull(stream);
            Assert.Equal(expected.Content, new StreamReader(stream).ReadToEnd());
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task StreamShouldNotBeUploadedBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, "http://example.com/");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            string content = "<http://example.com/> <http://example.com/title> \"\"@sk .";
            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionFile, null, "http://example.com/", true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            await client.UploadStream(stream, metadata, true);

            FileState? state = storage.GetFileState(metadata.Id, new AllAccessFilePolicy());
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileShouldBeUploadedBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, "http://example.com/");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            string content = "<http://example.com/> <http://example.com/title> \"\"@sk .";
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionFile, null, "http://example.com/", true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            await client.InsertFile(content, true, metadata);

            FileState? state = storage.GetFileState(metadata.Id, new AllAccessFilePolicy());
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FileMetadataShouldBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher))!;

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, expected.Metadata.Publisher);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            await client.UpdateMetadata(expected.Metadata);

            FileMetadata? metadata = storage.GetFileMetadata(expected.Metadata.Id, new AllAccessFilePolicy());
            Assert.NotNull(metadata);
            Assert.Equal(expected.Metadata, metadata);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        public async Task FilShouldBeDeletedBeReturnedForPublisher(string role)
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic && !string.IsNullOrEmpty(s.Metadata.Publisher) && !fixture.ExistingStates.Any(f => f.Metadata.ParentFile == s.Metadata.Id))!;

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor(role, expected.Metadata.Publisher);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            await client.DeleteFile(expected.Metadata.Id);
            Assert.Null(storage.GetFileState(expected.Metadata.Id, new AllAccessFilePolicy()));
        }

        [Fact]
        public async Task PublicFileStateShouldBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic)!;
            FileState? fileState = await client.GetFileState(expected.Metadata.Id);
            Assert.NotNull(fileState);
            Assert.Equal(expected, fileState);
        }

        [Fact]
        public async Task NonPublicFileStateShouldBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => !s.Metadata.IsPublic)!;
            FileState? fileState = await client.GetFileState(expected.Metadata.Id);
            Assert.NotNull(fileState);
            Assert.Equal(expected, fileState);
        }

        [Fact]
        public async Task FileStatesShouldBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            List<FileState> expected = fixture.ExistingStates.Where(s => s.Metadata.Type == FileType.DatasetRegistration).ToList();
            FileStorageResponse response = await client.GetFileStates(new FileStorageQuery { MaxResults = 3, SkipResults = 2, OnlyTypes = new List<FileType> { FileType.DatasetRegistration } });
            Assert.NotNull(response);
            Assert.True(response.TotalCount > 0);
            Assert.Equal(3, response.Files.Count);
            Assert.Equal(expected.Count, response.TotalCount);
            Assert.True(new HashSet<Guid>(expected.Skip(2).Take(3).Select(f => f.Metadata.Id)).SetEquals(response.Files.Select(f => f.Metadata.Id)));
        }

        [Fact]
        public async Task PublicFileMetadataShouldBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic)!;
            FileMetadata? fileMetadata = await client.GetFileMetadata(expected.Metadata.Id);
            Assert.NotNull(fileMetadata);
            Assert.Equal(expected.Metadata, fileMetadata);
        }

        [Fact]
        public async Task NonPublicFileMetadataShouldBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => !s.Metadata.IsPublic)!;
            FileMetadata? fileMetadata = await client.GetFileMetadata(expected.Metadata.Id);
            Assert.NotNull(fileMetadata);
            Assert.Equal(expected.Metadata, fileMetadata);
        }

        [Fact]
        public async Task PublicFileDownloadShouldBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic)!;
            Stream? stream = await client.DownloadStream(expected.Metadata.Id);
            Assert.NotNull(stream);
            Assert.Equal(expected.Content, new StreamReader(stream).ReadToEnd());
        }

        [Fact]
        public async Task NonPublicFileDownloadShouldBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => !s.Metadata.IsPublic)!;
            Stream? stream = await client.DownloadStream(expected.Metadata.Id);
            Assert.NotNull(stream);
            Assert.Equal(expected.Content, new StreamReader(stream).ReadToEnd());
        }

        [Fact]
        public async Task StreamShouldNotBeUploadedBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionFile, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "<http://example.com/> <http://example.com/title> \"\"@sk .";
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                await client.UploadStream(stream, metadata, true);
            }                

            FileState? state = storage.GetFileState(metadata.Id, new AllAccessFilePolicy());
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public async Task FileShouldBeUploadedBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            string content = "<http://example.com/> <http://example.com/title> \"\"@sk .";
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionFile, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            await client.InsertFile(content, true, metadata);

            FileState? state = storage.GetFileState(metadata.Id, new AllAccessFilePolicy());
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public async Task FileMetadataShouldBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic)!;
            await client.UpdateMetadata(expected.Metadata);

            FileMetadata? metadata = storage.GetFileMetadata(expected.Metadata.Id, new AllAccessFilePolicy());
            Assert.NotNull(metadata);
            Assert.Equal(expected.Metadata, metadata);
        }

        [Fact]
        public async Task FilShouldBeDeletedBeReturnedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileState expected = fixture.ExistingStates.FirstOrDefault(s => s.Metadata.IsPublic)!;
            await client.DeleteFile(expected.Metadata.Id);
            Assert.Null(storage.GetFileState(expected.Metadata.Id, new AllAccessFilePolicy()));
        }

        [Fact]
        public async Task LargeStreamShouldBeUploadedForSuperadmin()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());

            using DocumentStorageApplicationFactory applicationFactory = new DocumentStorageApplicationFactory(storage);
            using HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            IHttpContextValueAccessor httpContextAccessor = applicationFactory.CreateAccessor("Superadmin");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, httpContextAccessor.Token);
            DocumentStorageClient client = new DocumentStorageClient(httpClientFactory, httpContextAccessor);

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionFile, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            byte[] bytes = new byte[1024 * 1024 * 40];
            using (Stream stream = new MemoryStream(bytes))
            {
                await client.UploadStream(stream, metadata, true);
            }

            FileState? state = storage.GetFileState(metadata.Id, new AllAccessFilePolicy());
            Assert.NotNull(state);
            Assert.Null(state.Content);
            Assert.Equal(metadata, state.Metadata);
        }
    }
}
