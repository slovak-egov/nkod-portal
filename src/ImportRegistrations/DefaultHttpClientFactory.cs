using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportRegistrations
{
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<HttpClient> factory;

        public DefaultHttpClientFactory(Func<HttpClient> factory)
        {
            this.factory = factory;
        }

        public HttpClient CreateClient(string name)
        {
            return factory();
        }
    }
}
