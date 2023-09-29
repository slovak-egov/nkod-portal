using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NkodSk.Abstractions;
using System.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Policy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace IAM.Test
{
    public class UserAdministrationTests
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

            using HttpResponseMessage response = await client.GetAsync("/users");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

            using HttpClient client = applicationFactory.CreateClient();

            using JsonContent requestContent = JsonContent.Create(new NewUserInput
            {
                Email = "test@example.com",
                IdentificationNumber = "1234567890",
                FirstName = "Meno",
                LastName = "Priezvisko",
                Role = "Publisher",
            });
            using HttpResponseMessage response = await client.PostAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

            using HttpClient client = applicationFactory.CreateClient();

            using JsonContent requestContent = JsonContent.Create(new EditUserInput
            {
                Id = record.Id,
                Email = "test@example.com",
                Role = "Publisher",
            });
            using HttpResponseMessage response = await client.PutAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

            using HttpResponseMessage response = await client.DeleteAsync($"/users?id={HttpUtility.UrlEncode(record.Id)}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", PublisherId));

            using HttpResponseMessage response = await client.GetAsync("/users");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new NewUserInput
            {
                Email = "test@example.com",
                IdentificationNumber = "1234567890",
                FirstName = "Meno",
                LastName = "Priezvisko",
                Role = "Publisher",
            });
            using HttpResponseMessage response = await client.PostAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", PublisherId));

            using JsonContent requestContent = JsonContent.Create(new EditUserInput
            {
                Id = record.Id,
                Email = "test@example.com",
                Role = "Publisher",
            });
            using HttpResponseMessage response = await client.PutAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Publisher", PublisherId));

            using HttpResponseMessage response = await client.DeleteAsync($"/users?id={HttpUtility.UrlEncode(record.Id)}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            using HttpResponseMessage response = await client.GetAsync("/users");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            UserInfoResult? result = await response.Content.ReadFromJsonAsync<UserInfoResult>();
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            NewUserInput input = new NewUserInput
            {
                Email = "test@example.com",
                IdentificationNumber = "1234567890",
                FirstName = "Meno",
                LastName = "Priezvisko",
                Role = "Publisher",
            };
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            EditUserInput input = new EditUserInput
            {
                Id = record.Id,
                Email = "test@example.com",
                Role = "Publisher",
                FirstName = "Meno",
                LastName = "Priezvisko"
            };
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            using HttpResponseMessage response = await client.DeleteAsync($"/users?id={HttpUtility.UrlEncode(record.Id)}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin", PublisherId));

            using HttpResponseMessage response = await client.GetAsync("/users");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            UserInfoResult? result = await response.Content.ReadFromJsonAsync<UserInfoResult>();
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin", PublisherId));

            NewUserInput input = new NewUserInput
            {
                Email = "test@example.com",
                IdentificationNumber = "1234567890",
                FirstName = "Meno",
                LastName = "Priezvisko",
                Role = "Publisher",
            };
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin", PublisherId));

            EditUserInput input = new EditUserInput
            {
                Id = record.Id,
                Email = "test@example.com",
                Role = "Publisher",
                FirstName = "Meno",
                LastName = "Priezvisko"
            };
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/users", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
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

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin", PublisherId));

            using HttpResponseMessage response = await client.DeleteAsync($"/users?id={HttpUtility.UrlEncode(record.Id)}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(0, await context.Users.CountAsync());
            }
        }
    }
}
