using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Waf = Frontend.Test.WebApiApplicationFactory;
using Microsoft.AspNetCore.Builder;
using VDS.RDF.Query.Algebra;
using Microsoft.AspNetCore.Routing;
using Lucene.Net.Search;
using System.Security.Cryptography;

namespace CMS.Frontend.Test
{
    public class CompositeWebApplicationFactory : Waf
    {
        public CompositeWebApplicationFactory(Storage storage) : this(storage, RandomNumberGenerator.GetBytes(32))
        {
            
        }

        private CompositeWebApplicationFactory(Storage storage, byte[] key) : base(storage, key)
        {
            ApiApplicationFactory = new CMS.Test.ApiApplicationFactory(key);
        }

        public CMS.Test.ApiApplicationFactory ApiApplicationFactory { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.UseSolutionRelativeContentRoot(".");
            builder.UseWebRoot("frontend/build");

            HttpClient client = ApiApplicationFactory.CreateDefaultClient();

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new StartupFilter(client));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            ApiApplicationFactory.Dispose();
        }

        private class StartupFilter : IStartupFilter
        {
            private readonly HttpClient client;

            public StartupFilter(HttpClient client)
            {
                this.client = client;
            }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return builder =>
                {
                    builder.Use(async (ctx, next) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/cms"))
                        {
                            Microsoft.AspNetCore.Http.HttpRequest r = ctx.Request;

                            using HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(r.Method), r.Path + "?" + r.QueryString);

                            using MemoryStream stream = new MemoryStream();
                            await r.Body.CopyToAsync(stream);
                            
                            if (stream.Length > 0)
                            {
                                ByteArrayContent content = new ByteArrayContent(stream.ToArray());
                                foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in r.Headers)
                                {
                                    content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                                }
                                request.Content = content;
                            }

                            foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in r.Headers)
                            {
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                            }

                            using HttpResponseMessage response = await client.SendAsync(request);

                            ctx.Response.StatusCode = (int)response.StatusCode;

                            foreach (var header in response.Headers)
                            {
                                ctx.Response.Headers[header.Key] = header.Value.ToArray();
                            }
                            foreach (var header in response.Content.Headers)
                            {
                                ctx.Response.Headers[header.Key] = header.Value.ToArray();
                            }

                            ctx.Response.Headers.Remove("transfer-encoding");

                            await response.Content.CopyToAsync(ctx.Response.Body);
                        }
                        else
                        {
                            await next();
                        }
                    });
                    next(builder);
                };
            }
        }
    }
}
