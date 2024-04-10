using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlX.XDevAPI;
using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace IAM.Test
{
    public class CustomLoginTests
    {
        [Fact]
        public async Task RegistrationShouldBePerformed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/register");
            var input = new
            {
                Email = "test@test.sk",
                Password = "password",
                FirstName = "Meno",
                LastName = "Priezvisko"
            };
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(1, await context.Users.CountAsync());

                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
                Assert.NotNull(user);
                Assert.Null(user.Publisher);
                Assert.Equal(input.Email, user.Email);
                Assert.Equal(input.FirstName, user.FirstName);
                Assert.Equal(input.LastName, user.LastName);
                Assert.Equal("CommunityUser", user.Role);
                Assert.Null(user.RefreshToken);
                Assert.Null(user.RefreshTokenExpiryTime);
                Assert.True(string.IsNullOrEmpty(user.InvitationToken));
                Assert.Null(user.InvitedBy);
                Assert.Null(user.InvitedAt);
                Assert.Null(user.ActivatedAt);
                Assert.False(user.IsActive);
                Assert.False(string.IsNullOrEmpty(user.Password));
                Assert.False(string.IsNullOrEmpty(user.ActivationToken));
            }
        }

        private async Task TestInvalidRegistration(Func<UserRegistrationInput, WebApiApplicationFactory, Task> inputCallback)
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/register");
            UserRegistrationInput input = new UserRegistrationInput
            {
                Email = "test@test.sk",
                Password = "password",
                FirstName = "Meno",
                LastName = "Priezvisko"
            };

            await inputCallback.Invoke(input, applicationFactory);

            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Null(result.Id);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task RegistrationShouldNotBePerformedWithoutFirstName()
        {
            await TestInvalidRegistration((i, f) =>
            {
                i.FirstName = " ";
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task RegistrationShouldNotBePerformedWithoutLastName()
        {
            await TestInvalidRegistration((i, f) =>
            {
                i.LastName = " ";
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task RegistrationShouldNotBePerformedWithoutLongPassword()
        {
            await TestInvalidRegistration((i, f) =>
            {
                i.Password = "12345";
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task RegistrationShouldNotBePerformedWithoutEmail()
        {
            await TestInvalidRegistration((i, f) =>
            {
                i.Email = string.Empty;
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task RegistrationShouldNotBePerformedWithoutValidEmail()
        {
            await TestInvalidRegistration((i, f) =>
            {
                i.Email = "e.test";
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task RegistrationShouldNotBePerformedForRegisteredEmail()
        {
            await TestInvalidRegistration(async (i, f) =>
            {
                i.Email = "e.test";
                await f.CreateCommunityUser(i.Email, i.Password!);
            });
        }

        [Fact]
        public async Task CommunityUserShouldBeActivated()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Attach(existingUser);
                existingUser.IsActive = false;
                existingUser.ActivationToken = Guid.NewGuid().ToString();
                existingUser.ActivationTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1);
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/activation");
            var input = new
            {
                Id = existingUser.Id,
                Token = existingUser.ActivationToken,
            };
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(1, await context.Users.CountAsync());

                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
                Assert.NotNull(user);
                Assert.Null(user.Publisher);
                Assert.Equal("CommunityUser", user.Role);
                Assert.Null(user.RefreshToken);
                Assert.Null(user.RefreshTokenExpiryTime);
                Assert.True(string.IsNullOrEmpty(user.InvitationToken));
                Assert.NotNull(user.ActivatedAt);
                Assert.True(user.IsActive);
                Assert.False(string.IsNullOrEmpty(user.Password));
                Assert.True(string.IsNullOrEmpty(user.ActivationToken));
            }
        }

        private async Task TestInvalidActivation(Func<ActivationInput, WebApiApplicationFactory, UserRecord, Task> inputCallback)
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();
            
            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Attach(existingUser);
                existingUser.IsActive = false;
                existingUser.ActivationToken = Guid.NewGuid().ToString();
                existingUser.ActivationTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1);
                await context.SaveChangesAsync();
            }

            ActivationInput input = new ActivationInput
            {
                Id = existingUser.Id,
                Token = existingUser.ActivationToken,
            };

            await inputCallback.Invoke(input, applicationFactory, existingUser);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/activation");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(1, await context.Users.CountAsync());

                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.False(user.IsActive);
            }
        }

        [Fact]
        public async Task ActivationShouldNotBePerformedWithoutId()
        {
            await TestInvalidActivation((i, _, _) =>
            {
                i.Id = string.Empty;
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task ActivationShouldNotBePerformedWithoutToken()
        {
            await TestInvalidActivation((i, _, _) =>
            {
                i.Token = string.Empty;
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task ActivationShouldNotBePerformedAfterTime()
        {
            await TestInvalidActivation(async (i, f, existingUser) =>
            {
                using (IServiceScope scope = f.Services.CreateScope())
                {
                    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    context.Attach(existingUser);
                    existingUser.ActivationTokenExpiryTime = DateTimeOffset.Now.AddHours(-1);
                    await context.SaveChangesAsync();
                }
            });
        }

        [Fact]
        public async Task CommunityUserShouldNotBeActivatedWithoutToken()
        {
            await TestInvalidActivation(async (i, f, existingUser) =>
            {
                using (IServiceScope scope = f.Services.CreateScope())
                {
                    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    context.Attach(existingUser);
                    existingUser.IsActive = false;
                    existingUser.ActivationToken = null;
                    await context.SaveChangesAsync();
                }
            });
        }

        private async Task TestInvalidLogin(Func<LoginInput, WebApiApplicationFactory, UserRecord, Task> inputCallback)
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            LoginInput input = new LoginInput
            {
                Email = existingUser.Email,
                Password = "password",
            };

            await inputCallback.Invoke(input, applicationFactory, existingUser);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/login");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task LoginShouldNotBePerformedWithoutValidEmail()
        {
            await TestInvalidLogin((i, _, _) =>
            {
                i.Email = "test2@test.sk";
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task LoginShouldNotBePerformedWithoutValidPassword()
        {
            await TestInvalidLogin((i, _, _) => {
                i.Password = "password2";
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task LoginShouldNotBePerformedWithoutEmail()
        {
            await TestInvalidLogin((i, _, _) => {
                i.Email = string.Empty;
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task LoginShouldNotBePerformedWithoutPassword()
        {
            await TestInvalidLogin((i, _, _) => {
                i.Password = string.Empty;
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task LoginShouldNotBePerformedForInactiveAccount()
        {
            await TestInvalidLogin(async (i, f, existingUser) => {
                using (IServiceScope scope = f.Services.CreateScope())
                {
                    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    context.Attach(existingUser);
                    existingUser.IsActive = false;
                    await context.SaveChangesAsync();
                }
            });
        }

        [Fact]
        public async Task LoginShouldBePerformed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord user = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/login");
            var input = new
            {
                Email = user.Email,
                Password = "password",
            };
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            TokenResult? result = await response.Content.ReadFromJsonAsync<TokenResult>();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));

            ValidateToken(result.Token);
        }

        private async Task<TokenResult> Login(HttpClient client, UserRecord user)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/login");
            request.Content = JsonContent.Create(new
            {
                Email = user.Email,
                Password = "password",
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            TokenResult? result = await response.Content.ReadFromJsonAsync<TokenResult>();
            Assert.NotNull(result);
            return result;
        }

        [Fact]
        public async Task UserInfoShouldBeReturned()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord user = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            TokenResult tokenResult = await Login(client, user);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/user-info");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            UserInfo? result = await response.Content.ReadFromJsonAsync<UserInfo>();
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.FirstName, result.FirstName);
            Assert.Equal(user.LastName, result.LastName);
            Assert.Equal("CommunityUser", result.Role);
            Assert.Null(result.Publisher);
            Assert.Null(result.CompanyName);
            Assert.Equal("Native", result.AuthorizationMethod);
        }

        private List<Claim> GetClaims(string token)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken securityToken = tokenHandler.ReadJwtToken(token);
            return securityToken.Claims.ToList();
        }

        private void ValidateToken(string token)
        {
            List<Claim> claims = GetClaims(token);
            Assert.Equal(new[] { "CommunityUser" }, claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value));
            Assert.Empty(claims.Where(c => c.Type == "Publisher"));
        }

        [Fact]
        public async Task TokenShouldBeRefreshed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord user = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            TokenResult tokenResult = await Login(client, user);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/refresh");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
            request.Content = JsonContent.Create(new { AccessToken = tokenResult.Token, RefreshToken = tokenResult.RefreshToken });
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            TokenResult? result = await response.Content.ReadFromJsonAsync<TokenResult>();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));

            ValidateToken(result.Token);
        }

        [Fact]
        public async Task LogoutShouledBePerformed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            TokenResult tokenResult = await Login(client, existingUser);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(1, await context.Users.CountAsync());

                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.Null(user.RefreshToken);
                Assert.Null(user.RefreshTokenExpiryTime);
            }

            DelegationAuthorizationResult? result = await response.Content.ReadFromJsonAsync<DelegationAuthorizationResult>();
            Assert.NotNull(result);
            Assert.True(result.DoLogout);
            Assert.Null(result.RedirectUrl);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task PasswordRecoveryShouldBeSent()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            var input = new
            {
                Email = existingUser.Email,
            };

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/recovery");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.False(string.IsNullOrEmpty(user.RecoveryToken));
                Assert.Equal(1, user.RecoveryTokenSentTimes);
            }
        }

        private async Task TestPasswordRecovery(Func<PasswordRecoveryInput, WebApiApplicationFactory, UserRecord, Task> inputCallback)
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            PasswordRecoveryInput input = new PasswordRecoveryInput
            {
                Email = existingUser.Email,
            };

            await inputCallback(input, applicationFactory, existingUser);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/recovery");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Null(result.Id);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.True(string.IsNullOrEmpty(user.RecoveryToken));
                Assert.Equal(existingUser.RecoveryTokenSentTimes, user.RecoveryTokenSentTimes);
            }
        }

        [Fact]
        public async Task PasswordRecoveryShouldNotBeSentWithoutEmail()
        {
            await TestPasswordRecovery((i, _, _) =>
            {
                i.Email = string.Empty;
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task PasswordRecoveryShouldNotBeSentWithoutExistingEmail()
        {
            await TestPasswordRecovery((i, _, _) =>
            {
                i.Email = "test2@test.sk";
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task PasswordRecoveryShouldNotBeSentForInactiveAccount()
        {
            await TestPasswordRecovery(async (i, f, existingUser) =>
            {
                using (IServiceScope scope = f.Services.CreateScope())
                {
                    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    context.Attach(existingUser);
                    existingUser.IsActive = false;
                    await context.SaveChangesAsync();
                }
            });
        }

        [Fact]
        public async Task PasswordRecoveryShouldNotBeSentForSSOAccount()
        {
            await TestPasswordRecovery(async (i, f, existingUser) =>
            {
                using (IServiceScope scope = f.Services.CreateScope())
                {
                    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    context.Attach(existingUser);
                    existingUser.Password = null;
                    await context.SaveChangesAsync();
                }
            });
        }

        [Fact]
        public async Task PasswordRecoveryShouldNotBeSentTooManyTimes()
        {
            await TestPasswordRecovery(async (i, f, existingUser) =>
            {
                using (IServiceScope scope = f.Services.CreateScope())
                {
                    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    context.Attach(existingUser);
                    existingUser.RecoveryTokenSentTimes = 3;
                    await context.SaveChangesAsync();
                }
            });
        }

        [Fact]
        public async Task PasswordRecoveryShouldBePerformed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Attach(existingUser);
                existingUser.IsActive = true;
                existingUser.RecoveryToken = Guid.NewGuid().ToString();
                existingUser.RecoveryTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1);
                await context.SaveChangesAsync();
            }

            var input = new
            {
                Id = existingUser.Id,
                Token = existingUser.RecoveryToken,
                Password = "newpassword",
            };

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/recovery-activation");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.True(string.IsNullOrEmpty(user.RecoveryToken));
                Assert.Equal(0, user.RecoveryTokenSentTimes);
                Assert.False(string.IsNullOrEmpty(user.Password));
                Assert.NotEqual(existingUser.Password, user.Password);
            }
        }

        private async Task TestPasswordRecoveryConfirm(Func<PasswordRecoveryConfirmationInput, WebApiApplicationFactory, UserRecord, Task> inputCallback)
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Attach(existingUser);
                existingUser.IsActive = true;
                existingUser.RecoveryToken = Guid.NewGuid().ToString();
                existingUser.RecoveryTokenSentTimes = 1;
                existingUser.RecoveryTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1);
                await context.SaveChangesAsync();
            }

            PasswordRecoveryConfirmationInput input = new PasswordRecoveryConfirmationInput
            {
                Id = existingUser.Id,
                Token = existingUser.RecoveryToken,
                Password = "newpassword",
            };

            await inputCallback(input, applicationFactory, existingUser);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/recovery-activation");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Null(result.Id);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.Equal(existingUser.RecoveryTokenSentTimes, user.RecoveryTokenSentTimes);
                Assert.NotNull(user.RecoveryTokenExpiryTime);
                Assert.Equal(existingUser.Password, user.Password);
            }
        }

        [Fact]
        public async Task PasswordRecoveryShouldNotBePerformedWithoutId()
        {
            await TestPasswordRecoveryConfirm((i, _, _) =>
            {
                i.Id = string.Empty;
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task PasswordRecoveryShouldNotBePerformedWithoutToken()
        {
            await TestPasswordRecoveryConfirm((i, _, _) =>
            {
                i.Token = string.Empty;
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task PasswordRecoveryShouldNotBePerformedWithoutPassword()
        {
            await TestPasswordRecoveryConfirm((i, _, _) =>
            {
                i.Password = string.Empty;
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task PasswordRecoveryShouldNotBePerformedForInactiveAccount()
        {
            await TestPasswordRecoveryConfirm(async (i, f, existingUser) =>
            {
                using (IServiceScope scope = f.Services.CreateScope())
                {
                    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    context.Attach(existingUser);
                    existingUser.IsActive = false;
                    await context.SaveChangesAsync();
                }
            });
        }

        [Fact]
        public async Task PasswordRecoveryShouldNotBePerformedAfterSetTime()
        {
            await TestPasswordRecoveryConfirm(async (i, f, existingUser) =>
            {
                using (IServiceScope scope = f.Services.CreateScope())
                {
                    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    context.Attach(existingUser);
                    existingUser.RecoveryTokenExpiryTime = DateTimeOffset.Now.AddHours(-1);
                    await context.SaveChangesAsync();
                }
            });
        }

        [Fact]
        public async Task PasswordChangeShouldBePerformed()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            var input = new
            {
                OldPassword = "password",
                NewPassword = "newpassword"
            };

            TokenResult tokenResult = await Login(client, existingUser);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, tokenResult.Token);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/change-password");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.False(string.IsNullOrEmpty(user.Password));
                Assert.NotEqual(existingUser.Password, user.Password);
                Assert.True(user.VerifyPassword("newpassword"));
            }
        }

        [Fact]
        public async Task PasswordChangeShouldNotBePerformedWithWrongPassword()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            var input = new
            {
                OldPassword = "password2",
                NewPassword = "newpassword"
            };

            TokenResult tokenResult = await Login(client, existingUser);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, tokenResult.Token);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/change-password");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Null(result.Id);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.False(string.IsNullOrEmpty(user.Password));
                Assert.Equal(existingUser.Password, user.Password);
                Assert.True(user.VerifyPassword("password"));
            }
        }

        [Fact]
        public async Task PasswordChangeShouldNotBePerformedWithoutNewPassword()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            var input = new
            {
                OldPassword = "password",
                NewPassword = string.Empty
            };

            TokenResult tokenResult = await Login(client, existingUser);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, tokenResult.Token);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/change-password");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Null(result.Id);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.False(string.IsNullOrEmpty(user.Password));
                Assert.Equal(existingUser.Password, user.Password);
                Assert.True(user.VerifyPassword("password"));
            }
        }

        [Fact]
        public async Task PasswordChangeShouldNotBePerformedWithInvalidNewPassword()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            var input = new
            {
                OldPassword = "password",
                NewPassword = "abcde"
            };

            TokenResult tokenResult = await Login(client, existingUser);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, tokenResult.Token);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/change-password");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SaveResult? result = await response.Content.ReadFromJsonAsync<SaveResult>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Null(result.Id);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.False(string.IsNullOrEmpty(user.Password));
                Assert.Equal(existingUser.Password, user.Password);
                Assert.True(user.VerifyPassword("password"));
            }
        }

        [Fact]
        public async Task PasswordChangeShouldNotBePerformedUnauthorized()
        {
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory();
            using HttpClient client = applicationFactory.CreateClient();

            UserRecord existingUser = await applicationFactory.CreateCommunityUser("test@test.sk", "password");

            var input = new
            {
                OldPassword = "password",
                NewPassword = "newpassword"
            };

            TokenResult tokenResult = await Login(client, existingUser);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/change-password");
            request.Content = JsonContent.Create(input);
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            using (IServiceScope scope = applicationFactory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
                Assert.NotNull(user);
                Assert.False(string.IsNullOrEmpty(user.Password));
                Assert.Equal(existingUser.Password, user.Password);
                Assert.True(user.VerifyPassword("password"));
            }
        }
    }
}
