using NkodSk.RdfFileStorage;
using System.Web;

namespace DocumentStorageApi
{
    public class CmsServerOrderProvider : IOrderProvider
    {
        private readonly Uri baseUrl;

        private readonly string propertyName;

        public CmsServerOrderProvider(Uri baseUrl, string propertyName) 
        {
            this.baseUrl = baseUrl;
            this.propertyName = propertyName;
        }

        public List<string> GetOrder(bool reverseOrder)
        {
            return Task.Run(async () =>
            {
                using HttpClient client = new HttpClient();
                return await client.GetFromJsonAsync<List<string>>(new Uri(baseUrl, $"/cms/datasets/order?property={HttpUtility.UrlEncode(propertyName)}&reverse={HttpUtility.UrlEncode(reverseOrder.ToString())}")) ?? new List<string>();
            }).Result;
        }
    }
}
