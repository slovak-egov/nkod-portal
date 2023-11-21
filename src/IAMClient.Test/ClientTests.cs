using IAM;
using IAM.Test;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NkodSk.Abstractions;
using System.Net.Http.Json;
using System.Net;
using System.Web;
using TestBase;
using MySqlX.XDevAPI;
using System.Security.Claims;

namespace IAMClient.Test
{
    public class ClientTests
    { 
        private const string PublisherId = "http://example.com/publisher";

        private async Task<UserRecord> CreateDefaultUser(ApplicationDbContext context)
        {
            UserRecord record = new UserRecord
            {
                Id = Guid.NewGuid().ToString(),
                Publisher = PublisherId,
                Email = "test@example.com",
                Role = "Publisher"
            };
            context.Users.Add(record);
            await context.SaveChangesAsync();
            return record;
        }

        [Fact]
        public async Task GetListShouldNotBeAllowedWithoutAuthorization()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor();
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.GetUsers(new UserInfoQuery()));
            Assert.Equal(HttpStatusCode.Unauthorized, e.StatusCode);
        }

        [Fact]
        public async Task CreateUserShouldNotBeAllowedWithoutAuthorization()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            NewUserInput input = new NewUserInput
            {
                Email = "test@example.com",
                IdentificationNumber = "1234567890",
                FirstName = "Meno",
                LastName = "Priezvisko",
                Role = "Publisher",
            };
            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor();
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.CreateUser(input));
            Assert.Equal(HttpStatusCode.Unauthorized, e.StatusCode);
        }

        [Fact]
        public async Task EditUserShouldNotBeAllowedWithoutAuthorization()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            EditUserInput input = new EditUserInput
            {
                Id = record.Id,
                Email = "test@example.com",
                Role = "Publisher",
            };

            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor();
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.UpdateUser(input));
            Assert.Equal(HttpStatusCode.Unauthorized, e.StatusCode);
        }

        [Fact]
        public async Task DeleteUserShouldNotBeAllowedWithoutAuthorization()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor();
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.DeleteUser(record.Id));
            Assert.Equal(HttpStatusCode.Unauthorized, e.StatusCode);
        }

        [Fact]
        public async Task GetListShouldNotBeAllowedForPublisher()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Publisher", PublisherId);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.GetUsers(new UserInfoQuery()));
            Assert.Equal(HttpStatusCode.Forbidden, e.StatusCode);
        }

        [Fact]
        public async Task CreateUserShouldNotBeAllowedForPublisher()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            NewUserInput input = new NewUserInput
            {
                Email = "test@example.com",
                IdentificationNumber = "1234567890",
                FirstName = "Meno",
                LastName = "Priezvisko",
                Role = "Publisher",
            };

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Publisher", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.CreateUser(input));
            Assert.Equal(HttpStatusCode.Forbidden, e.StatusCode);
        }

        [Fact]
        public async Task EditUserShouldNotBeAllowedForPublisher()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            EditUserInput input = new EditUserInput
            {
                Id = record.Id,
                Email = "test@example.com",
                Role = "Publisher",
            };

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Publisher", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.UpdateUser(input));
            Assert.Equal(HttpStatusCode.Forbidden, e.StatusCode);
        }

        [Fact]
        public async Task DeleteUserShouldNotBeAllowedForPublisher()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Publisher", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.DeleteUser(record.Id));
            Assert.Equal(HttpStatusCode.Forbidden, e.StatusCode);
        }

        [Fact]
        public async Task GetListShouldBeAllowedForPublisherAdmin()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("PublisherAdmin", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            UserInfoResult? result = await iamClient.GetUsers(new UserInfoQuery());
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task CreateUserShouldBeAllowedForPublisherAdmin()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            NewUserInput input = new NewUserInput
            {
                Email = "test@example.com",
                IdentificationNumber = "1234567890",
                FirstName = "Meno",
                LastName = "Priezvisko",
                Role = "Publisher",
            };


            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("PublisherAdmin", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            SaveResult result = await iamClient.CreateUser(input);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.Null(result.Errors);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(2, await context.Users.CountAsync());

                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
                Assert.NotNull(user);
                Assert.Equal(PublisherId, user.Publisher);
                Assert.Equal(input.Email, user.Email);
                Assert.Equal(input.Role, user.Role);
                Assert.Null(user.RefreshToken);
                Assert.Null(user.RefreshTokenExpiryTime);
            }
        }

        [Fact]
        public async Task EditUserShouldBeAllowedForPublisherAdmin()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            EditUserInput input = new EditUserInput
            {
                Id = record.Id,
                Email = "test@example.com",
                Role = "Publisher",
                FirstName = "Meno",
                LastName = "Priezvisko"
            };

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("PublisherAdmin", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            SaveResult result = await iamClient.UpdateUser(input);
            Assert.NotNull(result);
            Assert.Equal(record.Id, result.Id);
            Assert.True(result.Success);
            Assert.Null(result.Errors);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(1, await context.Users.CountAsync());

                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
                Assert.NotNull(user);
                Assert.Equal(PublisherId, user.Publisher);
                Assert.Equal(input.Email, user.Email);
                Assert.Equal(input.Role, user.Role);
                Assert.Null(user.RefreshToken);
                Assert.Null(user.RefreshTokenExpiryTime);
            }
        }

        [Fact]
        public async Task DeleteUserShouldBeAllowedForPublisherAdmin()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("PublisherAdmin", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            await iamClient.DeleteUser(record.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(0, await context.Users.CountAsync());
            }
        }

        [Fact]
        public async Task GetListShouldBeAllowedForSuperadmin()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Superadmin", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            UserInfoResult result = await iamClient.GetUsers(new UserInfoQuery());
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task CreateUserShouldBeAllowedForSuperadmin()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            NewUserInput input = new NewUserInput
            {
                Email = "test@example.com",
                IdentificationNumber = "1234567890",
                FirstName = "Meno",
                LastName = "Priezvisko",
                Role = "Publisher",
            };


            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Superadmin", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            SaveResult result = await iamClient.CreateUser(input);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.Null(result.Errors);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(2, await context.Users.CountAsync());

                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
                Assert.NotNull(user);
                Assert.Equal(PublisherId, user.Publisher);
                Assert.Equal(input.Email, user.Email);
                Assert.Equal(input.Role, user.Role);
                Assert.Null(user.RefreshToken);
                Assert.Null(user.RefreshTokenExpiryTime);
            }
        }

        [Fact]
        public async Task EditUserShouldBeAllowedForSuperadmin()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            EditUserInput input = new EditUserInput
            {
                Id = record.Id,
                Email = "test@example.com",
                Role = "Publisher",
                FirstName = "Meno",
                LastName = "Priezvisko"
            };

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Superadmin", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            SaveResult result = await iamClient.UpdateUser(input);
            Assert.NotNull(result);
            Assert.Equal(record.Id, result.Id);
            Assert.True(result.Success);
            Assert.Null(result.Errors);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(1, await context.Users.CountAsync());

                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
                Assert.NotNull(user);
                Assert.Equal(PublisherId, user.Publisher);
                Assert.Equal(input.Email, user.Email);
                Assert.Equal(input.Role, user.Role);
                Assert.Null(user.RefreshToken);
                Assert.Null(user.RefreshTokenExpiryTime);
            }
        }

        [Fact]
        public async Task DeleteUserShouldBeAllowedForSuperadmin()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Superadmin", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            await iamClient.DeleteUser(record.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(0, await context.Users.CountAsync());
            }
        }

        [Fact]
        public async Task UserInfoShouldNotBeAvailableWithoutAuthorization()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor();
            HttpClient client = applicationFactory.CreateClient();
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.GetUserInfo());
            Assert.Equal(HttpStatusCode.Unauthorized, e.StatusCode);
        }

        [Fact]
        public async Task UserInfoShouldBeAvailableWithAuthorization()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Publisher", PublisherId, id: record.Id);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            UserInfo result = await iamClient.GetUserInfo();
            Assert.NotNull(result);
            Assert.Equal(record.Id, result.Id);
            Assert.Equal(record.Email, result.Email);
            Assert.Equal(record.Role, result.Role);
            Assert.Equal(record.Publisher, result.Publisher);
            Assert.Equal(record.FirstName, result.FirstName);
            Assert.Equal(record.LastName, result.LastName);
        }

        [Fact]
        public async Task TokenShouldBeRefreshed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
                record.RefreshToken = "1234";
                record.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1);
                await context.SaveChangesAsync();
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Publisher", PublisherId, id: record.Id);

            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            TokenResult? result = await iamClient.RefreshToken(contextValueAccessor.Token!, record.RefreshToken);
            Assert.NotNull(result);
            Assert.Equal(record.RefreshToken, result.RefreshToken);
            Assert.NotEqual(contextValueAccessor.Token, result.Token);

            ClaimsPrincipal claims = applicationFactory.ValidateToken(result.Token);
            Assert.Equal(record.Id, claims.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.Equal(record.Email, claims.FindFirstValue(ClaimTypes.Email));
            Assert.Equal(record.FirstName, claims.FindFirstValue(ClaimTypes.GivenName));
            Assert.Equal(record.LastName, claims.FindFirstValue(ClaimTypes.Surname));
            Assert.Equal(record.Role, claims.FindFirstValue(ClaimTypes.Role));
            Assert.Equal(record.Publisher, claims.FindFirstValue("Publisher"));

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                {
                    UserRecord? changedRecord = await context.Users.FindAsync(record.Id);
                    Assert.NotNull(changedRecord);
                    Assert.Equal(record.RefreshToken, changedRecord.RefreshToken);
                    Assert.Equal(record.RefreshTokenExpiryTime, changedRecord.RefreshTokenExpiryTime);
                }
            }
        }

        [Fact]
        public async Task TokenShouldNotBeRefreshedIfInvalid()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
                record.RefreshToken = "1234";
                record.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1);
                await context.SaveChangesAsync();
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Publisher", PublisherId, id: record.Id);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.RefreshToken(contextValueAccessor.Token!, "1235"));
            Assert.Equal(HttpStatusCode.Forbidden, e.StatusCode);
        }

        [Fact]
        public async Task TokenShouldNotBeRefreshedIfExpired()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
                record.RefreshToken = "1234";
                record.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddSeconds(-1);
                await context.SaveChangesAsync();
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Publisher", PublisherId, id: record.Id);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.RefreshToken(contextValueAccessor.Token!, record.RefreshToken));
            Assert.Equal(HttpStatusCode.Forbidden, e.StatusCode);
        }

        [Fact]
        public async Task DelegationTokenShouldBeIssuedToSuperadmin()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
                record.Publisher = null;
                record.Role = "Superadmin";
                await context.SaveChangesAsync();
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Superadmin", id: record.Id);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            TokenResult? result = await iamClient.DelegatePublisher(PublisherId);
            Assert.NotNull(result);
            Assert.NotEqual(contextValueAccessor.Token, result.Token);
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));

            ClaimsPrincipal claims = applicationFactory.ValidateToken(result.Token);
            Assert.Equal(record.Id, claims.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.Equal(record.Email, claims.FindFirstValue(ClaimTypes.Email));
            Assert.Equal(record.FirstName, claims.FindFirstValue(ClaimTypes.GivenName));
            Assert.Equal(record.LastName, claims.FindFirstValue(ClaimTypes.Surname));
            Assert.Equal(record.Role, claims.FindFirstValue(ClaimTypes.Role));
            Assert.Equal(PublisherId, claims.FindFirstValue("Publisher"));

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                {
                    UserRecord? changedRecord = await context.Users.FindAsync(record.Id);
                    Assert.NotNull(changedRecord);
                    Assert.Equal(result.RefreshToken, changedRecord.RefreshToken);
                    Assert.NotNull(changedRecord.RefreshTokenExpiryTime);
                    Assert.True(changedRecord.RefreshTokenExpiryTime.Value > DateTimeOffset.UtcNow);
                }
            }
        }

        [Fact]
        public async Task DelegationTokenShouldNotBeIssuedToPublisher()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord record;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                record = await CreateDefaultUser(context);
            }

            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor("Superadmin", PublisherId);
            HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, contextValueAccessor.Token);
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(async () => await iamClient.DelegatePublisher(PublisherId));
            Assert.Equal(HttpStatusCode.Forbidden, e.StatusCode);
        }
    }
}
