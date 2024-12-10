using System.Web;

namespace CMS
{
    public class NotificationService : INotificationService
    {
        private readonly Uri baseUri;

        private readonly Uri frontentBaseUri;

        public NotificationService(string baseUri, string frontentBaseUri)
        {
            this.baseUri = baseUri is not null ? new Uri(baseUri) : null;
            this.frontentBaseUri = frontentBaseUri is not null ? new Uri(frontentBaseUri) : null;
        }

        public void Notify(string email, string url, string title, string description, List<string> tags)
        {
            if (baseUri is not null && frontentBaseUri is not null)
            {
                Task.Run(async () =>
                {
                    using HttpClient client = new HttpClient();
                    using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/notification"));
                    request.Content = JsonContent.Create(new
                    {
                        Notifications = new[]
                        {
                            new
                            {
                                Email = email,
                                Url = new Uri(frontentBaseUri, url).OriginalString,
                                Description = description,
                                Tags = tags
                            }
                        }
                    });
                    await client.SendAsync(request);
                });
            }
        }

        public void Delete(string tag)
        {
            if (baseUri is not null)
            {
                Task.Run(async () =>
                {
                    using HttpClient client = new HttpClient();
                    await client.DeleteAsync(new Uri(baseUri, $"/notification/tag?tag={HttpUtility.UrlEncode(tag)}"));
                });
            }
        }
    }
}
