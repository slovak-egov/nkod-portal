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
using System.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Test
{
    public class UsersAdminTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherFileAccessPolicy(PublisherId);

        public UsersAdminTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task UserListShouldNotBeAvailableForAnonymousUser()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            using JsonContent requestContent = JsonContent.Create(new { Page = 1, PageSize = 10 });
            using HttpResponseMessage response = await client.PostAsync("/users/search", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateUserShouldNotBeAvailableForAnonymousUser()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            using JsonContent requestContent = JsonContent.Create(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            });
            using HttpResponseMessage response = await client.PostAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task EditUserShouldNotBeAvailableForAnonymousUser()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();
            string userId = (identityClient.CreateUser(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            }, PublisherId)).Id!;

            using HttpClient client = applicationFactory.CreateClient();

            using JsonContent requestContent = JsonContent.Create(new EditUserInput
            {
                Id = userId,
                Role = "PublisherAdmin",
                Email = "test@example.com"
            });
            using HttpResponseMessage response = await client.PutAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUserShouldNotBeAvailableForAnonymousUser()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();
            string userId = (identityClient.CreateUser(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            }, PublisherId)).Id!;

            using HttpClient client = applicationFactory.CreateClient();

            using HttpResponseMessage response = await client.DeleteAsync($"/users?id={HttpUtility.UrlEncode(userId)}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UserListShouldNotBeAvailableForPublisher()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new { Page = 1, PageSize = 10 });
            using HttpResponseMessage response = await client.PostAsync("/users/search", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateUserShouldNotBeAvailableForPublisher()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            });
            using HttpResponseMessage response = await client.PostAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task EditUserShouldNotBeAvailableForPublisher()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();
            string userId = ( identityClient.CreateUser(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            }, PublisherId)).Id!;

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new EditUserInput
            {
                Id = userId,
                Role = "PublisherAdmin",
                Email = "test@example.com"
            });
            using HttpResponseMessage response = await client.PutAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUserShouldNotBeAvailableForPublisher()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();
            string userId = (identityClient.CreateUser(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            }, PublisherId)).Id!;

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", PublisherId));

            using HttpResponseMessage response = await client.DeleteAsync($"/users?id={HttpUtility.UrlEncode(userId)}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UserListShouldBeAvailableForPublisherAdmin()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new { Page = 1, PageSize = 10 });
            using HttpResponseMessage response = await client.PostAsync("/users/search", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            UserInfoResult? result = await response.Content.ReadFromJsonAsync<UserInfoResult>();
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task CreateUserShouldBeAvailableForPublisherAdmin()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();

            using JsonContent requestContent = JsonContent.Create(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            });
            using HttpResponseMessage response = await client.PostAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.Null(result.Errors);

            PersistentUserInfo? userInfo = (await identityClient.GetUsers(new UserInfoQuery { Page = 1, PageSize = 10 }, PublisherId)).Items.FirstOrDefault(u => u.Id == result.Id);

            Assert.NotNull(userInfo);
            Assert.Equal("test@example.com", userInfo.Email);
            Assert.Equal("PublisherAdmin", userInfo.Role);
        }

        [Fact]
        public async Task EditUserShouldBeAvailableForPublisherAdmin()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();
            string userId = (identityClient.CreateUser(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            }, PublisherId)).Id!;

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new EditUserInput
            {
                Id = userId,
                Role = "Publisher",
                Email = "test2@example.com"
            });
            using HttpResponseMessage response = await client.PutAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.True(result.Success);
            Assert.Null(result.Errors);

            PersistentUserInfo? userInfo = (await identityClient.GetUsers(new UserInfoQuery { Page = 1, PageSize = 10 }, PublisherId)).Items.FirstOrDefault(u => u.Id == userId);
            Assert.NotNull(userInfo);
            Assert.Equal("test2@example.com", userInfo.Email);
            Assert.Equal("Publisher", userInfo.Role);
        }

        [Fact]
        public async Task DeleteUserShouldBeAvailableForPublisherAdmin()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();
            string userId = (identityClient.CreateUser(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            }, PublisherId)).Id!;

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            using HttpResponseMessage response = await client.DeleteAsync($"/users?id={userId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Null((await identityClient.GetUsers(new UserInfoQuery { Page = 1, PageSize = 10 }, PublisherId)).Items.FirstOrDefault(u => u.Id == userId));
        }

        [Fact]
        public async Task UserListShouldBeAvailableForSuperadmin()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new { Page = 1, PageSize = 10 });
            using HttpResponseMessage response = await client.PostAsync("/users/search", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            UserInfoResult? result = await response.Content.ReadFromJsonAsync<UserInfoResult>();
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task CreateUserShouldBeAvailableForSuperadmin()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin", PublisherId));

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();

            using JsonContent requestContent = JsonContent.Create(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            });
            using HttpResponseMessage response = await client.PostAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.Null(result.Errors);

            PersistentUserInfo? userInfo = (await identityClient.GetUsers(new UserInfoQuery { Page = 1, PageSize = 10 }, PublisherId)).Items.FirstOrDefault(u => u.Id == result.Id);

            Assert.NotNull(userInfo);
            Assert.Equal("test@example.com", userInfo.Email);
            Assert.Equal("PublisherAdmin", userInfo.Role);
        }

        [Fact]
        public async Task EditUserShouldBeAvailableForSuperadmin()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();
            string userId = (identityClient.CreateUser(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            }, PublisherId)).Id!;

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new EditUserInput
            {
                Id = userId,
                Role = "Publisher",
                Email = "test2@example.com"
            });
            using HttpResponseMessage response = await client.PutAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.True(result.Success);
            Assert.Null(result.Errors);

            PersistentUserInfo? userInfo = (await identityClient.GetUsers(new UserInfoQuery { Page = 1, PageSize = 10 }, PublisherId)).Items.FirstOrDefault(u => u.Id == userId);
            Assert.NotNull(userInfo);
            Assert.Equal("test2@example.com", userInfo.Email);
            Assert.Equal("Publisher", userInfo.Role);
        }

        [Fact]
        public async Task DeleteUserShouldBeAvailableForSuperadmin()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);

            TestIdentityAccessManagementClient identityClient = applicationFactory.Services.GetRequiredService<TestIdentityAccessManagementClient>();
            string userId = (identityClient.CreateUser(new NewUserInput
            {
                FirstName = "Meno",
                LastName = "Priezvisko",
                Email = "test@example.com",
                IdentificationNumber = "12345",
                Role = "PublisherAdmin",
            }, PublisherId)).Id!;

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin", PublisherId));

            using HttpResponseMessage response = await client.DeleteAsync($"/users?id={userId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Null((await identityClient.GetUsers(new UserInfoQuery { Page = 1, PageSize = 10 }, PublisherId)).Items.FirstOrDefault(u => u.Id == userId));
        }
    }
}
