using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace IAM.Test
{
    public class AdminNotificationTests
    {
        private static async Task CreateUsers(WebApiApplicationFactory applicationFactory)
        {
            using IServiceScope scope = applicationFactory.Services.CreateScope();
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            context.Users.Add(new UserRecord { Id = Guid.NewGuid().ToString(), Email = "admin1@example.com", Role = "Superadmin", IsActive = true });
            context.Users.Add(new UserRecord { Id = Guid.NewGuid().ToString(), Email = "admin2@example.com", Role = "Superadmin", IsActive = true });
            context.Users.Add(new UserRecord { Id = Guid.NewGuid().ToString(), Email = "inactive@example.com", Role = "Superadmin", IsActive = false });
            context.Users.Add(new UserRecord { Id = Guid.NewGuid().ToString(), Email = "publisher@example.com", Role = "PublisherAdmin", IsActive = true });
            await context.SaveChangesAsync();
        }

        private static AdminNotificationInput CreateInput() => new AdminNotificationInput { Subject = "Predmet", Body = "Text <b>správy</b>" };

        [Fact]
        public async Task NotificationShouldNotBeAvailableWithoutAuthorization()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            await CreateUsers(applicationFactory);

            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.PostAsync("/notify-superadmins", JsonContent.Create(CreateInput()));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Empty(applicationFactory.Services.GetRequiredService<TestEmailService>().Emails);
        }

        [Theory]
        [InlineData("Publisher")]
        [InlineData("PublisherAdmin")]
        [InlineData("CommunityUser")]
        public async Task NotificationShouldBeForbiddenForOtherRoles(string role)
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            await CreateUsers(applicationFactory);

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role, "http://example.com/publisher"));
            using HttpResponseMessage response = await client.PostAsync("/notify-superadmins", JsonContent.Create(CreateInput()));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(applicationFactory.Services.GetRequiredService<TestEmailService>().Emails);
        }

        [Theory]
        [InlineData("Harvester")]
        [InlineData("Superadmin")]
        public async Task NotificationShouldBeSentToActiveSuperadmins(string role)
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            await CreateUsers(applicationFactory);

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken(role));
            AdminNotificationInput input = CreateInput();
            using HttpResponseMessage response = await client.PostAsync("/notify-superadmins", JsonContent.Create(input));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.True(result.Success);

            IReadOnlyList<(string, string, string)> emails = applicationFactory.Services.GetRequiredService<TestEmailService>().Emails;
            Assert.Equal(new[] { "admin1@example.com", "admin2@example.com" }, emails.Select(e => e.Item1).OrderBy(e => e));
            Assert.All(emails, e =>
            {
                Assert.Equal(input.Subject, e.Item2);
                Assert.Equal(input.Body, e.Item3);
            });
        }

        [Fact]
        public async Task NotificationShouldNotBeSentWithoutSubjectAndBody()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            await CreateUsers(applicationFactory);

            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Harvester"));
            using HttpResponseMessage response = await client.PostAsync("/notify-superadmins", JsonContent.Create(new AdminNotificationInput()));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.Errors);
            Assert.Contains("subject", result.Errors.Keys);
            Assert.Contains("body", result.Errors.Keys);
            Assert.Empty(applicationFactory.Services.GetRequiredService<TestEmailService>().Emails);
        }
    }
}
