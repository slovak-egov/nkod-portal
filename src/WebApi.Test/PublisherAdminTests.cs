using IAMClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TestBase;

namespace WebApi.Test
{
    public class PublisherAdminTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        private IFileStorageAccessPolicy accessPolicy = new AllAccessFilePolicy();

        public PublisherAdminTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task PublisherShouldNotBeImpersonatedForAnonymous()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            using JsonContent requestContent = JsonContent.Create(new { });
            using HttpResponseMessage response = await client.PostAsync($"/publishers/impersonate?id={HttpUtility.UrlEncode(id.ToString())}", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PublisherShouldNotBeImpersonatedForPublisherAdmin()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new { });
            using HttpResponseMessage response = await client.PostAsync($"/publishers/impersonate?id={HttpUtility.UrlEncode(id.ToString())}", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PublisherShouldBeImpersonatedForSuperadmin()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));

            using JsonContent requestContent = JsonContent.Create(new { });
            using HttpResponseMessage response = await client.PostAsync($"/publishers/impersonate?id={HttpUtility.UrlEncode(id.ToString())}", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            TokenResult? result = JsonConvert.DeserializeObject<TokenResult>(content);
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.RefreshToken);
        }

        [Fact]
        public async Task PublisherShouldBeImpersonatedAgainForSuperadmin()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreatePublisher("Test", PublisherId, isPublic: true);
            Guid id2 = fixture.CreatePublisher("Test2", PublisherId + "!", isPublic: true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();
            UserSaveResult userSaveResult = identityClient.CreateUser(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                Role = "Superadmin",
            }, null);

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin", userId: userSaveResult.Id));

            async Task DoImpersonation(Guid id, string uri)
            {
                using (JsonContent requestContent = JsonContent.Create(new { }))
                {
                    using HttpResponseMessage response = await client.PostAsync($"/publishers/impersonate?id={HttpUtility.UrlEncode(id.ToString())}", requestContent);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string content = await response.Content.ReadAsStringAsync();
                    TokenResult? result = JsonConvert.DeserializeObject<TokenResult>(content);
                    Assert.NotNull(result);
                    Assert.NotNull(result.Token);
                    Assert.NotNull(result.RefreshToken);

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, result.Token);
                }

                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/user-info"))
                {
                    using HttpResponseMessage response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(await response.Content.ReadAsStringAsync())!;
                    Assert.Equal(uri, userInfo.Publisher);
                }                
            }

            await DoImpersonation(id1, PublisherId);
            await DoImpersonation(id2, PublisherId + "!");
        }

        private AdminPublisherInput CreateInput()
        {
            AdminPublisherInput input = new AdminPublisherInput
            {
                Name = new Dictionary<string, string>
                {
                    { "sk", "TestName" }
                },
                Website = "http://example.com/",
                Email = "test@example.com",
                Phone = "+421 123 456 789",
                LegalForm = "https://data.gov.sk/def/legal-form-type/331",
                Uri = PublisherId,
            };

            return input;
        }

        private void ValidateValues(Storage storage, string? id, AdminPublisherInput input)
        {
            Assert.NotNull(id);

            FileState? state = storage.GetFileState(Guid.Parse(id), accessPolicy);
            Assert.NotNull(state);
            Assert.NotNull(state.Content);

            Assert.Equal(input.IsEnabled, state.Metadata.IsPublic);
            Assert.Equal(input.Uri, state.Metadata.Publisher);
            Assert.Equal(FileType.PublisherRegistration, state.Metadata.Type);
            Assert.Null(state.Metadata.ParentFile);
            Assert.Equal(input.Name!["sk"], state.Metadata.Name["sk"]);
            Assert.True((DateTimeOffset.Now - state.Metadata.Created).Duration().TotalMinutes < 1);
            Assert.True((DateTimeOffset.Now - state.Metadata.LastModified).Duration().TotalMinutes < 1);

            FoafAgent? agent = FoafAgent.Parse(state.Content);
            Assert.NotNull(agent);

            Extensions.AssertTextsEqual(input.Name, agent.Name);
            Assert.Equal(input.Website, agent.HomePage?.ToString());
            Assert.Equal(input.Email, agent.EmailAddress);
            Assert.Equal(input.Phone, agent.Phone);
            Assert.Equal(input.LegalForm, agent.LegalForm?.ToString());
        }

        [Fact]
        public async Task TestCreateUnauthorized()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using JsonContent requestContent = JsonContent.Create(CreateInput());
            using HttpResponseMessage response = await client.PostAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestCreateAsPublisherAdmin()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            using JsonContent requestContent = JsonContent.Create(CreateInput());
            using HttpResponseMessage response = await client.PostAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TestCreateAsSuperadmin()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            AdminPublisherInput input = CreateInput();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input);
        }

        [Fact]
        public async Task TestCreateAsSuperadminWithoutPhone()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            AdminPublisherInput input = CreateInput();
            input.Phone = string.Empty;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input);
        }

        [Fact]
        public async Task TestCreateAsSuperadminWithExistingUri()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            const string uriText = "http://example.com/123";
            Guid id = fixture.CreatePublisher("Test", uriText);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            AdminPublisherInput input = CreateInput();
            input.Uri = uriText;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.Null(result.Id);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task TestModifyUnauthorized()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            Guid publisherId = fixture.CreatePublisher("Test");
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            AdminPublisherInput input = CreateInput();
            input.Id = publisherId.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestModifyAsPublisherAdmin()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            Guid publisherId = fixture.CreatePublisher("Test");
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            AdminPublisherInput input = CreateInput();
            input.Id = publisherId.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TestModify()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            Guid id = fixture.CreatePublisher("Test");
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            AdminPublisherInput input = CreateInput();
            input.Id = id.ToString();
            input.IsEnabled = true;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input);

            Assert.Equal(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.PublisherRegistration } }, accessPolicy).TotalCount);
        }

        [Fact]
        public async Task TestModifyWithExistingUri()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();
            const string uriText = "http://example.com/123";

            Guid id = fixture.CreatePublisher("Test");
            fixture.CreatePublisher("Test", uriText);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            AdminPublisherInput input = CreateInput();
            input.Id = id.ToString();
            input.Uri = uriText;
            input.IsEnabled = true;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.Null(result.Id);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task TestModifyToActivated()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            Guid id = fixture.CreatePublisher("Test", isPublic: false);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            AdminPublisherInput input = CreateInput();
            input.IsEnabled = true;
            input.Id = id.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input);
        }

        [Fact]
        public async Task TestModifyToDeativated()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            Guid id = fixture.CreatePublisher("Test", isPublic: true);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            AdminPublisherInput input = CreateInput();
            input.IsEnabled = false;
            input.Id = id.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input);
        }

        [Fact]
        public async Task DatasetLegalFormShouldBeUpdatedOnCreate()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisherCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            AdminPublisherInput input = CreateInput();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? state = storage.GetFileState(datasetId, accessPolicy);
            Assert.NotNull(state);
            Assert.Equal(new[] { "https://data.gov.sk/def/legal-form-type/331" }, state.Metadata.AdditionalValues?[FoafAgent.LegalFormCodelist] ?? Array.Empty<string>());
        }

        [Fact]
        public async Task DatasetLegalFormShouldBeUpdatedOnModify()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisherCodelists();
            Guid id = fixture.CreatePublisher("Test", PublisherId, isPublic: true);
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));
            AdminPublisherInput input = CreateInput();
            input.Id = id.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/publishers", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? state = storage.GetFileState(datasetId, accessPolicy);
            Assert.NotNull(state);
            Assert.Equal(new[] { "https://data.gov.sk/def/legal-form-type/331" }, state.Metadata.AdditionalValues?[FoafAgent.LegalFormCodelist] ?? Array.Empty<string>());
        }
    }
}
