using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Frontend.Test
{
    public class CompositeHost : IHost
    {
        private readonly IHost[] hosts;
        
        public CompositeHost(params IHost[] hosts)
        {
            this.hosts = hosts;
        }

        public IServiceProvider Services => hosts[0].Services;

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            foreach (IHost host in hosts)
            {
                await host.StartAsync();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            foreach (IHost host in hosts)
            {
                await host.StopAsync();
            }
        }

        public void Dispose()
        {
            foreach (IHost host in hosts)
            {
                host.Dispose();
            }
        }
    }
}
