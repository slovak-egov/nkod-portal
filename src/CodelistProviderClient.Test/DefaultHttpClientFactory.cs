using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodelistProviderClient.Test
{
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient httpClient;

        public DefaultHttpClientFactory(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public HttpClient CreateClient(string name) => httpClient;
    }
}
