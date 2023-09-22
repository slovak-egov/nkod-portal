using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace IAM.Test
{
    public class AccountTests
    {
        private const string PublisherId = "http://example.com/publisher";

        private async Task<UserRecord> CreateDefaultUser(ApplicationDbContext context)
        {
            UserRecord record = new UserRecord
            {
                Id = Guid.NewGuid().ToString(),
                Publisher = PublisherId,
                Email = "test@example.com",
                Role = "Publisher",
                FirstName = "Meno",
                LastName = "Priezvisko"
            };
            context.Users.Add(record);
            await context.SaveChangesAsync();
            return record;
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

            using HttpClient client = applicationFactory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync("/user-info");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", PublisherId, id: record.Id));

            using HttpResponseMessage response = await client.GetAsync("/user-info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            UserInfo? result = await response.Content.ReadFromJsonAsync<UserInfo>();
            Assert.NotNull(result);
            Assert.Equal(record.Id, result.Id);
            Assert.Equal(record.Email, result.Email);
            Assert.Equal(record.Role, result.Role);
            Assert.Equal(record.Publisher, result.Publisher);
            Assert.Equal(record.FirstName, result.FirstName);
            Assert.Equal(record.LastName, result.LastName);
        }

        [Fact]
        public async Task LogoutShouldClearRefreshToken()
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", PublisherId, id: record.Id));

            using HttpResponseMessage response = await client.GetAsync("/logout");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                {
                    UserRecord? changedRecord = await context.Users.FindAsync(record.Id);
                    Assert.NotNull(changedRecord);
                    Assert.Null(changedRecord.RefreshToken);
                    Assert.Null(changedRecord.RefreshTokenExpiryTime);
                }
            }
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

            using HttpClient client = applicationFactory.CreateClient();
            string accessToken = applicationFactory.CreateToken("Publisher", PublisherId, id: record.Id);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, accessToken);

            RefreshTokenRequest request = new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = record.RefreshToken
            };
            using JsonContent requestContent = JsonContent.Create(request);
            using HttpResponseMessage response = await client.PostAsync("/refresh", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            TokenResult? result = await response.Content.ReadFromJsonAsync<TokenResult>();
            Assert.NotNull(result);
            Assert.Equal(record.RefreshToken, result.RefreshToken);
            Assert.NotEqual(accessToken, result.Token);

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

            using HttpClient client = applicationFactory.CreateClient();
            string accessToken = applicationFactory.CreateToken("Publisher", PublisherId, id: record.Id);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, accessToken);

            RefreshTokenRequest request = new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = "1235"
            };
            using JsonContent requestContent = JsonContent.Create(request);
            using HttpResponseMessage response = await client.PostAsync("/refresh", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

            using HttpClient client = applicationFactory.CreateClient();
            string accessToken = applicationFactory.CreateToken("Publisher", PublisherId, id: record.Id);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, accessToken);

            RefreshTokenRequest request = new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = record.RefreshToken
            };
            using JsonContent requestContent = JsonContent.Create(request);
            using HttpResponseMessage response = await client.PostAsync("/refresh", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

            using HttpClient client = applicationFactory.CreateClient();
            string accessToken = applicationFactory.CreateToken("Superadmin", id: record.Id);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, accessToken);

            using JsonContent requestContent = JsonContent.Create(new { });
            using HttpResponseMessage response = await client.PostAsync($"/delegate-publisher?publisher={HttpUtility.UrlEncode(PublisherId)}", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            TokenResult? result = await response.Content.ReadFromJsonAsync<TokenResult>();
            Assert.NotNull(result);
            Assert.NotEqual(accessToken, result.Token);
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

            using HttpClient client = applicationFactory.CreateClient();
            string accessToken = applicationFactory.CreateToken("Publisher", PublisherId, id: record.Id);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, accessToken);

            using JsonContent requestContent = JsonContent.Create(new { });
            using HttpResponseMessage response = await client.PostAsync($"/delegate-publisher?publisher={HttpUtility.UrlEncode(PublisherId)}", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
