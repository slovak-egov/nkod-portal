using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient httpClient;

        public DefaultHttpClientFactory(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            return httpClient;
        }
    }
}
