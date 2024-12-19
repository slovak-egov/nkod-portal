using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Hosting;
using System.Security.Policy;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NotificationService.Test
{
    public class WebApiApplicationFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                services.RemoveAll(typeof(MainDbContext));
                services.RemoveAll(typeof(DbContextOptions));
                services.RemoveAll(typeof(DbContextOptions<MainDbContext>));
                                
                string name = Guid.NewGuid().ToString();

                services.AddDbContext<MainDbContext>(options =>
                {
                    options.UseInMemoryDatabase(name);
                    options.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });

                services.AddSingleton(sp => new TestSender(sp));
                services.AddSingleton<ISender>(sp => sp.GetRequiredService<TestSender>());
            });

            return base.CreateHost(builder);
        }

        public TestSender Sender => Server.Services.GetRequiredService<TestSender>();
    }
}
