using Abstractions;
using CodelistProvider.Test;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace CodelistProviderClient.Test
{
    public class ClientTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public ClientTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task TestCodelists()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, AnonymousAccessPolicy.Default);
            HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            CodelistProviderClient client = new CodelistProviderClient(httpClientFactory);
            List<Codelist>? lists = await client.GetCodelists();
            Assert.NotNull(lists);
            Assert.Equal(2, lists.Count);
        }

        [Fact]
        public async Task TestSingleCodelist()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, AnonymousAccessPolicy.Default);
            HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            CodelistProviderClient client = new CodelistProviderClient(httpClientFactory);
            Codelist? list = await client.GetCodelist(DcatDataset.AccrualPeriodicityCodelist);
            Assert.NotNull(list);
        }

        [Fact]
        public async Task TestUnknwonCodelist()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, AnonymousAccessPolicy.Default);
            HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            CodelistProviderClient client = new CodelistProviderClient(httpClientFactory);
            Codelist? list = await client.GetCodelist("unknown");
            Assert.Null(list);
        }
    }
}
