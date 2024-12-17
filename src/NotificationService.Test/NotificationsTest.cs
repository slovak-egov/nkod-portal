using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Test
{
    public class NotificationsTest
    {
        private async Task AssertNotificationSetting(WebApiApplicationFactory f, string email, bool? disabled)
        {
            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                NotificationSetting? setting = await context.NotificationSettings.FirstOrDefaultAsync(e => e.Email == email);
                if (disabled.HasValue)
                {
                    Assert.NotNull(setting);
                    Assert.Equal(email, setting.Email);
                    Assert.NotEmpty(setting.AuthKey);
                    Assert.Equal(disabled.Value, setting.IsDisabled);
                }
                else
                {
                    Assert.Null(setting);
                }
            }
        }

        [Fact]
        public async Task NotificationShouldBeSentImmediately()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                context.SentEmails.Add(new SentEmail
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "test2@test.sk",
                    Sent = DateTimeOffset.Now.AddMinutes(-59)
                });
                context.SentEmails.Add(new SentEmail
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "test@test.sk",
                    Sent = DateTimeOffset.Now.AddMinutes(-61)
                });
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification");
            request.Content = JsonContent.Create(new
            {
                Notifications = new[]
                {
                    new
                    {
                        Email = "test@test.sk",
                        Url = "http://example.com/url",
                        Description = "Test description",
                    }
                }
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(["test@test.sk"], f.Sender.Sent.Keys);

            await AssertNotificationSetting(f, "test@test.sk", false);
        }

        [Fact]
        public async Task NotificationShouldNotBeSentImmediately()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                context.SentEmails.Add(new SentEmail
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "test@test.sk",
                    Sent = DateTimeOffset.Now.AddMinutes(-59)
                });
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification");
            request.Content = JsonContent.Create(new
            {
                Notifications = new[]
                {
                    new
                    {
                        Email = "test@test.sk",
                        Url = "http://example.com/url",
                        Description = "Test description",
                    }
                }
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Empty(f.Sender.Sent);

            await AssertNotificationSetting(f, "test@test.sk", null);
        }

        [Fact]
        public async Task NotificationShouldNotBeSentLater()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                context.SentEmails.Add(new SentEmail
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "test@test.sk",
                    Sent = DateTimeOffset.Now.AddMinutes(-59)
                });
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification");
            request.Content = JsonContent.Create(new
            {
                Notifications = new[]
                {
                    new
                    {
                        Email = "test@test.sk",
                        Url = "http://example.com/url",
                        Description = "Test description",
                    }
                }
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Empty(f.Sender.Sent);

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                SenderService senderService = scope.ServiceProvider.GetRequiredService<SenderService>();
                await senderService.Run();

                Assert.Empty(f.Sender.Sent);

                context.SentEmails.First().Sent = DateTimeOffset.Now.AddMinutes(-61);
                await context.SaveChangesAsync();

                await senderService.Run();

                Assert.Equal(["test@test.sk"], f.Sender.Sent.Keys);
                Assert.Single(f.Sender.Sent["test@test.sk"]);

                await senderService.Run();

                Assert.Equal(["test@test.sk"], f.Sender.Sent.Keys);
                Assert.Single(f.Sender.Sent["test@test.sk"]);
            }

            await AssertNotificationSetting(f, "test@test.sk", false);
        }


        [Fact]
        public async Task NotificationShouldBeCancelled()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                context.SentEmails.Add(new SentEmail
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "test@test.sk",
                    Sent = DateTimeOffset.Now.AddMinutes(-59)
                });
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification");
            request.Content = JsonContent.Create(new
            {
                Notifications = new[]
                {
                    new
                    {
                        Email = "test@test.sk",
                        Url = "http://example.com/url",
                        Description = "Test description",
                        Tags = new[] { "A", "B" }
                    }
                }
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Empty(f.Sender.Sent);

            using HttpResponseMessage response2 = await client.DeleteAsync("/notification/tag?tag=B");
            Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                Notification notification = context.Notifications.First();
                Assert.True(notification.IsDeleted);

                context.SentEmails.First().Sent = DateTimeOffset.Now.AddMinutes(-61);

                await context.SaveChangesAsync();

                SenderService senderService = scope.ServiceProvider.GetRequiredService<SenderService>();
                await senderService.Run();

                Assert.Empty(f.Sender.Sent);
            }

            await AssertNotificationSetting(f, "test@test.sk", null);
        }

        [Fact]
        public async Task NotificationSettingsShouldBeDisabledByAuthKey()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            NotificationSetting setting;
            string email = "test@test.sk";

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                setting = await context.GetOrCreateNotificationSettings(email);
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification/set");
            request.Content = JsonContent.Create(new
            {
                AuthKey = setting.AuthKey,
                IsDisabled = true
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            await AssertNotificationSetting(f, "test@test.sk", true);
        }

        [Fact]
        public async Task NotificationSettingsShouldBeDisabledByEmail()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            NotificationSetting setting;
            string email = "test@test.sk";

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                setting = await context.GetOrCreateNotificationSettings(email);
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification/set");
            request.Content = JsonContent.Create(new
            {
                Email = email,
                IsDisabled = true
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            await AssertNotificationSetting(f, "test@test.sk", true);
        }

        [Fact]
        public async Task NotificationSettingsShouldBeEnabledByAuthKey()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            NotificationSetting setting;
            string email = "test@test.sk";

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                setting = await context.GetOrCreateNotificationSettings(email);
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification/set");
            request.Content = JsonContent.Create(new
            {
                AuthKey = setting.AuthKey,
                IsDisabled = false
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            await AssertNotificationSetting(f, "test@test.sk", false);
        }

        [Fact]
        public async Task NotificationSettingsShouldBeEnabledByEmail()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            NotificationSetting setting;
            string email = "test@test.sk";

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                setting = await context.GetOrCreateNotificationSettings(email);
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification/set");
            request.Content = JsonContent.Create(new
            {
                Email = email,
                IsDisabled = false
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            await AssertNotificationSetting(f, "test@test.sk", false);
        }

        [Fact]
        public async Task NotificationSettingsShouldBeReadDisabledWithAuthKey()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            NotificationSetting setting;
            string email = "test@test.sk";

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                setting = await context.GetOrCreateNotificationSettings(email);
                setting.IsDisabled = true;
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification/get");
            request.Content = JsonContent.Create(new
            {
                AuthKey = setting.AuthKey,
                IsDisabled = false
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            NotificationSetting? current = await response.Content.ReadFromJsonAsync<NotificationSetting>();

            Assert.NotNull(current);
            Assert.True(current.IsDisabled);
        }

        [Fact]
        public async Task NotificationSettingsShouldBeReadEnabledWithAuthKey()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            NotificationSetting setting;
            string email = "test@test.sk";

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                setting = await context.GetOrCreateNotificationSettings(email);
                setting.IsDisabled = false;
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification/get");
            request.Content = JsonContent.Create(new
            {
                AuthKey = setting.AuthKey,
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            NotificationSetting? current = await response.Content.ReadFromJsonAsync<NotificationSetting>();

            Assert.NotNull(current);
            Assert.False(current.IsDisabled);
        }

        [Fact]
        public async Task NotificationSettingsShouldBeReadDisabledWithEmail()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            NotificationSetting setting;
            string email = "test@test.sk";

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                setting = await context.GetOrCreateNotificationSettings(email);
                setting.IsDisabled = true;
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification/get");
            request.Content = JsonContent.Create(new
            {
                Email = email,
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            NotificationSetting? current = await response.Content.ReadFromJsonAsync<NotificationSetting>();

            Assert.NotNull(current);
            Assert.True(current.IsDisabled);
        }

        [Fact]
        public async Task NotificationSettingsShouldBeReadEnabledWithEmail()
        {
            using WebApiApplicationFactory f = new WebApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            NotificationSetting setting;
            string email = "test@test.sk";

            using (IServiceScope scope = f.Services.CreateScope())
            using (MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>())
            {
                setting = await context.GetOrCreateNotificationSettings(email);
                setting.IsDisabled = false;
                await context.SaveChangesAsync();
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/notification/get");
            request.Content = JsonContent.Create(new
            {
                Email = email,
            });
            using HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            NotificationSetting? current = await response.Content.ReadFromJsonAsync<NotificationSetting>();

            Assert.NotNull(current);
            Assert.False(current.IsDisabled);
        }
    }
}
