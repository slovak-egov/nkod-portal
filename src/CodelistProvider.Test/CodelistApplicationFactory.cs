using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace CodelistProvider.Test
{
    public class CodelistApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly IFileStorage storage;

        private readonly IFileStorageAccessPolicy accessPolicy;

        public CodelistApplicationFactory(IFileStorage storage, IFileStorageAccessPolicy accessPolicy)
        {
            this.storage = storage;
            this.accessPolicy = accessPolicy;
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(storage);
                services.AddSingleton(accessPolicy);
                services.AddSingleton<IDocumentStorageClient, TestDocumentStorageClient>();
            });

            return base.CreateHost(builder);
        }
    }
}
