using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringCheck
{
    public class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<string, HttpClient> clientFactory;
        
        public StaticHttpClientFactory(Func<string, HttpClient> clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        public HttpClient CreateClient(string name)
        {
            return clientFactory(name);
        }
    }
}
