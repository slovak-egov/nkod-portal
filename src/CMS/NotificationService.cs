using System.Web;

namespace CMS
{
    public class NotificationService
    {
        private readonly Uri baseUri;

        public NotificationService(string baseUri)
        {
            this.baseUri = baseUri is not null ? new Uri(baseUri) : null;
        }

        public void Notify(string email, string url, string description)
        {
            if (baseUri is not null)
            {
                Task.Run(async () =>
                {
                    using HttpClient client = new HttpClient();
                    using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/notification"));
                    request.Content = JsonContent.Create(new
                    {
                        Email = email,
                        Url = url,
                        Description = description,
                    });
                    await client.SendAsync(request);
                });
            }
        }

        public void Delete(string url)
        {
            if (baseUri is not null)
            {
                Task.Run(async () =>
                {
                    using HttpClient client = new HttpClient();
                    await client.DeleteAsync(new Uri(baseUri, $"/notification/url?url={HttpUtility.UrlEncode(url)}"));
                });
            }
        }
    }
}
