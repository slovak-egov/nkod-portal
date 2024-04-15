using Newtonsoft.Json;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TestBase;
using System.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using NkodSk.Abstractions;

namespace WebApi.Test
{
    public class FileTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        public FileTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task PublicDownloadFileTest()
        {
            string path = fixture.GetStoragePath();

            (Guid datasetId, _, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            Guid id = fixture.CreateDistributionFile("test.txt", "content", true, distributions[0]);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/download?id={HttpUtility.UrlEncode(id.ToString())}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("test.txt", response.Content.Headers.ContentDisposition?.FileName);
            Assert.Equal(Encoding.UTF8.GetByteCount("content"), response.Content.Headers.ContentLength);
            string content = await response.Content.ReadAsStringAsync();
            Assert.Equal("content", content);
        }

        [Fact]
        public async Task PublicHeadFileTest()
        {
            string path = fixture.GetStoragePath();

            (Guid datasetId, _, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            Guid id = fixture.CreateDistributionFile("test.txt", "content", true, distributions[0]);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, $"/download?id={HttpUtility.UrlEncode(id.ToString())}");
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("test.txt", response.Content.Headers.ContentDisposition?.FileName);
            Assert.Equal(Encoding.UTF8.GetByteCount("content"), response.Content.Headers.ContentLength);
        }

        [Fact]
        public async Task PublicDatasetFileShouldNotBeDownloaded()
        {
            string path = fixture.GetStoragePath();
            Guid id = fixture.CreateDataset("test", PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/download?id={HttpUtility.UrlEncode(id.ToString())}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task NonPublicDownloadFileTest()
        {
            string path = fixture.GetStoragePath();
            Guid id = fixture.CreateDistributionFile("test.txt", "content", false);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"/download?id={HttpUtility.UrlEncode(id.ToString())}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FileShouldBeUploaded()
        {
            string path = fixture.GetStoragePath();
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            string name = "test.txt";
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            using MultipartFormDataContent requestContent = new MultipartFormDataContent();
            requestContent.Add(new ByteArrayContent(bytes, 0, bytes.Length), "file", name);
            using HttpResponseMessage response = await client.PostAsync("/upload", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string responseContent = await response.Content.ReadAsStringAsync();
            FileUploadResult? result = JsonConvert.DeserializeObject<FileUploadResult>(responseContent);
            Assert.NotNull(result);
            Assert.NotEmpty(result.Id);
            Assert.NotEmpty(result.Url);

            FileState? state = storage.GetFileState(Guid.Parse(result.Id), AnonymousAccessPolicy.Default);
            Assert.NotNull(state);
            Assert.Equal(name, state.Metadata.Name);
            Assert.Equal(name, state.Metadata.OriginalFileName);
            Assert.Equal(PublisherId, state.Metadata.Publisher);
            Assert.Equal(FileType.DistributionFile, state.Metadata.Type);
            Assert.Null(state.Metadata.ParentFile);
            Assert.Null(state.Metadata.AdditionalValues);
            Assert.True(state.Metadata.IsPublic);

            Assert.Equal(content, state.Content);
        }

        [Fact]
        public async Task LargeFileShouldBeUploaded()
        {
            string path = fixture.GetStoragePath();

            byte[] bytes = new byte[1024 * 1024  * 30];
            const byte fillByte = (byte)'a';
            Array.Fill(bytes, fillByte);

            string name = "test.txt";
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            using MultipartFormDataContent requestContent = new MultipartFormDataContent();
            requestContent.Add(new ByteArrayContent(bytes, 0, bytes.Length), "file", name);
            using HttpResponseMessage response = await client.PostAsync("/upload", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string responseContent = await response.Content.ReadAsStringAsync();
            FileUploadResult? result = JsonConvert.DeserializeObject<FileUploadResult>(responseContent);
            Assert.NotNull(result);
            Assert.NotEmpty(result.Id);
            Assert.NotEmpty(result.Url);

            Guid id = Guid.Parse(result.Id);
            FileState? state = storage.GetFileState(id, AnonymousAccessPolicy.Default);
            Assert.NotNull(state);
            Assert.Equal(name, state.Metadata.Name);
            Assert.Equal(name, state.Metadata.OriginalFileName);
            Assert.Equal(PublisherId, state.Metadata.Publisher);
            Assert.Equal(FileType.DistributionFile, state.Metadata.Type);
            Assert.Null(state.Metadata.ParentFile);
            Assert.Null(state.Metadata.AdditionalValues);
            Assert.True(state.Metadata.IsPublic);

            using Stream? stream = storage.OpenReadStream(id, AnonymousAccessPolicy.Default);
            Assert.NotNull(stream);
            byte[] buffer = new byte[4096];
            long totalRead = 0;
            while (true)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0) break;
                for (int i = 0; i < read; i++)
                {
                    if (buffer[i] != fillByte)
                    {
                        Assert.Fail("Invalid byte at position " + (totalRead + i));
                    }
                }
                totalRead += read;
            }
            Assert.Equal(bytes.Length, totalRead);
        }

        [Fact]
        public async Task LargeFileOverLimitShouldNotBeUploaded()
        {
            string path = fixture.GetStoragePath();

            byte[] bytes = new byte[1024 * 1024 * 300 + 1];
            Array.Fill(bytes, (byte)'a');

            string name = "test.txt";
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            using MultipartFormDataContent requestContent = new MultipartFormDataContent();
            requestContent.Add(new ByteArrayContent(bytes, 0, bytes.Length), "file", name);
            using HttpResponseMessage response = await client.PostAsync("/upload", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
