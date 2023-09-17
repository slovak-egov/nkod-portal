using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebApi.Test
{
    static class Extensions
    {
        public static async Task<AbstractResponse<DatasetView>> SearchDatasets(this HttpClient client, HttpContent? requestContent)
        {
            using HttpResponseMessage response = await client.PostAsync("/datasets/search", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();

            AbstractResponse<DatasetView>? result = JsonConvert.DeserializeObject<AbstractResponse<DatasetView>>(content);
            Assert.NotNull(result);
            return result;
        }

        public static async Task<AbstractResponse<PublisherView>> SearchPublishers(this HttpClient client, HttpContent? requestContent)
        {
            using HttpResponseMessage response = await client.PostAsync("/publishers/search", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();

            AbstractResponse<PublisherView>? result = JsonConvert.DeserializeObject<AbstractResponse<PublisherView>>(content);
            Assert.NotNull(result);
            return result;
        }

        public static async Task<AbstractResponse<LocalCatalogView>> SearchLocalCatalogs(this HttpClient client, HttpContent? requestContent)
        {
            using HttpResponseMessage response = await client.PostAsync("/local-catalogs/search", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();

            AbstractResponse<LocalCatalogView>? result = JsonConvert.DeserializeObject<AbstractResponse<LocalCatalogView>>(content);
            Assert.NotNull(result);
            return result;
        }
    }
}
