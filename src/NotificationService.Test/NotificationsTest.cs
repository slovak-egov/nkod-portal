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
        }
    }
}
