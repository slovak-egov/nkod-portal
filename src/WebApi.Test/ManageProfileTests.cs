using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace WebApi.Test
{
    public class ManageProfileTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherAccessPolicy(PublisherId);

        public ManageProfileTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        private RegistrationInput CreateInput()
        {
            return new RegistrationInput
            {
                Website = "http://example.com/",
                Email = "info@example.sk",
                Phone = "+421 123 456 789"
            };
        }

        [Fact]
        public async Task UpdateProfileIsNotAllowedWithoutToken()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            RegistrationInput input = CreateInput();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/profile", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProfileIsNotAllowedWithoutPublisher()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(null, null));
            RegistrationInput input = CreateInput();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/profile", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProfileIsPerformed()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher("Test", PublisherId, isPublic: false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            RegistrationInput input = CreateInput();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/profile", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.Null(result.Errors);

            FileState? state = storage.GetFileState(Guid.Parse(result.Id), accessPolicy);
            Assert.NotNull(state);
            Assert.NotNull(state.Content);
            Assert.Equal(FileType.PublisherRegistration, state.Metadata.Type);
            Assert.False(state.Metadata.IsPublic);
            Assert.True((DateTimeOffset.Now - state.Metadata.Created).Duration().TotalMinutes < 1);
            Assert.True((DateTimeOffset.Now - state.Metadata.LastModified).Duration().TotalMinutes < 1);

            FoafAgent? agent = FoafAgent.Parse(state.Content);
            Assert.NotNull(agent);
            Assert.Equal(PublisherId, agent.Uri.ToString());
            Assert.Equal("Test", agent.GetName("sk"));
            Assert.Equal(input.Website, agent.HomePage?.ToString());
            Assert.Equal(input.Email, agent.EmailAddress);
            Assert.Equal(input.Phone, agent.Phone);
        }

        [Fact]
        public async Task UpdateProfileWebsiteIsRequired()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            RegistrationInput input = CreateInput();
            input.Website = string.Empty;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/profile", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.True(string.IsNullOrEmpty(result.Id));
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.False(string.IsNullOrEmpty(result.Errors["website"]));
        }

        [Fact]
        public async Task UpdateProfileEmailIsRequired()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            RegistrationInput input = CreateInput();
            input.Email = string.Empty;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/profile", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.True(string.IsNullOrEmpty(result.Id));
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.False(string.IsNullOrEmpty(result.Errors["email"]));
        }

        [Fact]
        public async Task UpdateProfilePhoneIsRequired()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            RegistrationInput input = CreateInput();
            input.Phone = string.Empty;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/profile", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.True(string.IsNullOrEmpty(result.Id));
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.False(string.IsNullOrEmpty(result.Errors["phone"]));
        }
    }
}
