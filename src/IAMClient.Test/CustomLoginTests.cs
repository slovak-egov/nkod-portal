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
using AngleSharp.Io;

namespace IAMClient.Test
{
    public class CustomLoginTests
    {
        private async Task<UserRecord> CreateDefaultUser(ApplicationDbContext context, bool isActive)
        {
            UserRecord record = new UserRecord
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test@example.com",
                Role = "CommunityUser",
                FirstName = "Test",
                LastName = "User",
                IsActive = isActive
            };

            record.SetPassword("password");

            if (!isActive)
            {
                record.ActivationToken = Guid.NewGuid().ToString();
                record.ActivatedAt = DateTimeOffset.UtcNow;
                record.ActivationTokenExpiryTime = DateTimeOffset.UtcNow.AddHours(1);
            }

            context.Users.Add(record);
            await context.SaveChangesAsync();
            return record;
        }

        [Fact]
        public async Task RegistrationShouldBePerformed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRegistrationInput input = new UserRegistrationInput
            {
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Password = "password",
            };

            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor();
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            SaveResult result = await iamClient.Register(input);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                UserRecord? record = await context.Users.FirstOrDefaultAsync(x => x.Id == result.Id);
                Assert.NotNull(record);
                Assert.Equal("CommunityUser", record.Role);
                Assert.Equal("Test", record.FirstName);
                Assert.Equal("User", record.LastName);
                Assert.False(record.IsActive);
                Assert.NotNull(record.ActivationToken);
                Assert.Null(record.ActivatedAt);
                Assert.NotNull(record.ActivationTokenExpiryTime);
            }
        }

        [Fact]
        public async Task ActivationShouldBePerformed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord existingUser;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                existingUser = await CreateDefaultUser(context, false);
            }

            ActivationInput input = new ActivationInput
            {
                Id = existingUser.Id,
                Token = existingUser.ActivationToken
            };

            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor();
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            SaveResult result = await iamClient.ActivateAccount(input);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                UserRecord? record = await context.Users.FirstOrDefaultAsync(x => x.Id == result.Id);
                Assert.NotNull(record);
                Assert.Equal("CommunityUser", record.Role);
                Assert.True(record.IsActive);
                Assert.Null(record.ActivationToken);
                Assert.NotNull(record.ActivatedAt);
                Assert.Null(record.ActivationTokenExpiryTime);
            }
        }

        [Fact]
        public async Task LoginShouldBePerformed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord existingUser;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                existingUser = await CreateDefaultUser(context, true);
            }

            LoginInput input = new LoginInput
            {
                Email = existingUser.Email,
                Password = "password"
            };

            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor();
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            TokenResult result = await iamClient.Login(input);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Token));
        }

        [Fact]
        public async Task PasswordRecoveryShouldBePerformed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord existingUser;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                existingUser = await CreateDefaultUser(context, true);
            }

            PasswordRecoveryInput input = new PasswordRecoveryInput
            {
                Email = existingUser.Email
            };

            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor();
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            SaveResult result = await iamClient.RequestPasswordRecovery(input);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                UserRecord? record = await context.Users.FirstOrDefaultAsync(x => x.Id == result.Id);
                Assert.NotNull(record);
                Assert.Equal("CommunityUser", record.Role);
                Assert.True(record.IsActive);
                Assert.NotNull(record.RecoveryToken);
                Assert.NotNull(record.RecoveryTokenExpiryTime);
            }
        }

        [Fact]
        public async Task PasswordRecoveryConfirmShouldBePerformed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord existingUser;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                existingUser = await CreateDefaultUser(context, true);

                existingUser.RecoveryToken = Guid.NewGuid().ToString();
                existingUser.RecoveryTokenExpiryTime = DateTimeOffset.UtcNow.AddHours(1);
                await context.SaveChangesAsync();
            }

            PasswordRecoveryConfirmationInput input = new PasswordRecoveryConfirmationInput
            {
                Id = existingUser.Id,
                Token = existingUser.RecoveryToken,
                Password = "newpassword"
            };

            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = applicationFactory.CreateAccessor();
            IdentityAccessManagementClient iamClient = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            SaveResult result = await iamClient.ConfirmPasswordRecovery(input);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                UserRecord? record = await context.Users.FirstOrDefaultAsync(x => x.Id == result.Id);
                Assert.NotNull(record);
                Assert.Equal("CommunityUser", record.Role);
                Assert.True(record.IsActive);
                Assert.Null(record.RecoveryToken);
                Assert.Null(record.RecoveryTokenExpiryTime);

                Assert.True(record.VerifyPassword("newpassword"));
            }
        }

        [Fact]
        public async Task PasswordShouldBeChanged()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();

            UserRecord existingUser;
            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                existingUser = await CreateDefaultUser(context, true);
            }

            PasswordChangeInput input = new PasswordChangeInput
            {
                OldPassword = "password",
                NewPassword = "newpassword"
            };

            using HttpClient client = applicationFactory.CreateClient();
            IHttpContextValueAccessor contextValueAccessor = new StaticHttpContextValueAccessor(null, null, "CommunityUser", existingUser.Id);
            IdentityAccessManagementClient iamClient1 = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);

            TokenResult tokenResult = await iamClient1.Login(new LoginInput { Email = existingUser.Email, Password = "password" });

            contextValueAccessor = new StaticHttpContextValueAccessor(null, tokenResult.Token, "CommunityUser", existingUser.Id);
            IdentityAccessManagementClient iamClient2 = new IdentityAccessManagementClient(new DefaultHttpClientFactory(client), contextValueAccessor);
            SaveResult result = await iamClient2.ChangePassword(input);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                UserRecord? record = await context.Users.FirstOrDefaultAsync(x => x.Id == result.Id);
                Assert.NotNull(record);
                Assert.Equal("CommunityUser", record.Role);
                Assert.True(record.IsActive);
                Assert.Null(record.RecoveryToken);
                Assert.Null(record.RecoveryTokenExpiryTime);

                Assert.True(record.VerifyPassword("newpassword"));
            }
        }
    }
}
