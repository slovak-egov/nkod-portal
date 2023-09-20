using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace DocumentStorageClient
{
    public class DocumentStorageClient : IDocumentStorageClient
    {
        private readonly IHttpClientFactory httpClientFactory;

        private readonly IHttpContextValueAccessor httpContextAccessor;

        public const string HttpClientName = "DocumentStorage";

        public DocumentStorageClient(IHttpClientFactory httpClientFactory, IHttpContextValueAccessor httpContextAccessor)
        {
            this.httpClientFactory = httpClientFactory;
            this.httpContextAccessor = httpContextAccessor;
        }

        private HttpClient CreateClient()
        {
            HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            string? token = httpContextAccessor.Token;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        public async Task<FileState?> GetFileState(Guid id)
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"files/{id}");
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<FileState>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                throw new Exception($"Unable to get file {id} from DocumentStorage, StatusCode = {response.StatusCode}");
            }
        }

        public async Task<Stream?> DownloadStream(Guid id)
        {
            HttpClient client = CreateClient();
            HttpResponseMessage response = await client.GetAsync($"files/{id}/content");
            if (response.IsSuccessStatusCode)
            {
                return new DependentStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), response);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                throw new Exception($"Unable to get file {id} from DocumentStorage, StatusCode = {response.StatusCode}");
            }
        }

        public async Task<FileStorageResponse> GetFileStates(FileStorageQuery query)
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.PostAsync("files/query", JsonContent.Create(query)).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<FileStorageResponse>(await response.Content.ReadAsStringAsync())
                    ?? throw new Exception("Invalid result from POST /files/query");
            }
            else
            {
                throw new Exception($"Unable to get files from DocumentStorage, StatusCode = {response.StatusCode}");
            }
        }

        public async Task InsertFile(string content, bool enableOverwrite, FileMetadata metadata)
        {
            HttpClient client = CreateClient();
            HttpResponseMessage response = await client.PostAsync("files", JsonContent.Create(new
            {
                Content = content,
                EnableOverwrite = enableOverwrite,
                Metadata = metadata
            })).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteFile(Guid id)
        {
            HttpClient client = CreateClient();
            HttpResponseMessage response = await client.DeleteAsync($"files/{id}").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task<FileMetadata?> GetFileMetadata(Guid id)
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"files/{id}/metadata");
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<FileMetadata>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                throw new Exception($"Unable to get file {id} from DocumentStorage, StatusCode = {response.StatusCode}");
            }
        }

        public async Task<FileStorageGroupResponse> GetFileStatesByPublisher(FileStorageQuery query)
        {
            HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.PostAsync("/files/by-publisher", JsonContent.Create(query)).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<FileStorageGroupResponse>(await response.Content.ReadAsStringAsync())
                    ?? throw new Exception("Invalid result from POST /query");
            }
            else
            {
                throw new Exception($"Unable to get files from DocumentStorage, StatusCode = {response.StatusCode}");
            }
        }

        public async Task UploadStream(Stream source, FileMetadata metadata, bool enableOverwrite)
        {
            HttpClient client = CreateClient();
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"files/stream");
            request.Content = new MultipartFormDataContent
            {
                { JsonContent.Create(metadata), "metadata" },
                { new StreamContent(source), "file" },
                { new StringContent(enableOverwrite.ToString()), "enableOverwrite" }
            };
            using HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateMetadata(FileMetadata metadata)
        {
            HttpClient client = CreateClient();
            HttpResponseMessage response = await client.PostAsync($"files/metadata", JsonContent.Create(metadata)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }
}