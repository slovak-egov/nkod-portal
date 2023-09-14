using Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Newtonsoft.Json;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TestBase;
using static System.Net.Mime.MediaTypeNames;

namespace CodelistProvider.Test
{
    public class EndpointTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public EndpointTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task TestHomepage()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, AnonymousAccessPolicy.Default);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TestCodelists()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, AnonymousAccessPolicy.Default);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync("/codelist");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<Codelist>? lists = JsonConvert.DeserializeObject<List<Codelist>>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(lists);
            Assert.Equal(2, lists.Count);
        }

        [Fact]
        public async Task TestSingCodelist()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, AnonymousAccessPolicy.Default);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync("/codelist/frequency");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Codelist? list = JsonConvert.DeserializeObject<Codelist>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(list);
        }

        [Fact]
        public async Task TestUnknwonCodelist()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, AnonymousAccessPolicy.Default);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync("/codelist/unknown");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
