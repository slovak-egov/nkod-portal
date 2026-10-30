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
            CodelistProviderClient client = new CodelistProviderClient(httpClientFactory, new AnonymousHttpContextValueAccessor());
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
            CodelistProviderClient client = new CodelistProviderClient(httpClientFactory, new AnonymousHttpContextValueAccessor());
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
            CodelistProviderClient client = new CodelistProviderClient(httpClientFactory, new AnonymousHttpContextValueAccessor());
            Codelist? list = await client.GetCodelist("unknown");
            Assert.Null(list);
        }

        [Fact]
        public async Task TestSingleCodelistItem()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, AnonymousAccessPolicy.Default);
            HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            CodelistProviderClient client = new CodelistProviderClient(httpClientFactory, new AnonymousHttpContextValueAccessor());
            Codelist? list = await client.GetCodelist(DcatDataset.ThemeCodelist);
            Assert.NotNull(list);

            CodelistItem? item = list.Items.GetValueOrDefault("http://publications.europa.eu/resource/authority/data-theme/AGRI");
            Assert.NotNull(item);

            item = list.Items.GetValueOrDefault("http://publications.europa.eu/resource/authority/data-theme/agri");
            Assert.NotNull(item);
        }

        [Fact]
        public async Task TestCodelistItemByShortId()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, AnonymousAccessPolicy.Default);
            HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            CodelistProviderClient client = new CodelistProviderClient(httpClientFactory, new AnonymousHttpContextValueAccessor());

            CodelistItem? item = await client.GetCodelistItem(DcatDataset.ThemeCodelist, "agri");
            Assert.NotNull(item);
            Assert.Equal("http://publications.europa.eu/resource/authority/data-theme/AGRI", item.Id);

            Assert.Null(await client.GetCodelistItem(DcatDataset.ThemeCodelist, "not-existing"));
        }

        private class RecordingHandler : DelegatingHandler
        {
            public List<string> Requests { get; } = new List<string>();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add($"{request.Method} {request.RequestUri?.AbsolutePath}");
                return base.SendAsync(request, cancellationToken);
            }
        }

        [Fact]
        public async Task CodelistItemShouldBeVerifiedWithSingleRequest()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, AnonymousAccessPolicy.Default);
            RecordingHandler handler = new RecordingHandler();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(applicationFactory.CreateDefaultClient(handler));
            CodelistProviderClient client = new CodelistProviderClient(httpClientFactory, new AnonymousHttpContextValueAccessor());

            CodelistItem? item = await client.GetCodelistItem(DcatDataset.ThemeCodelist, "http://publications.europa.eu/resource/authority/data-theme/AGRI");
            Assert.NotNull(item);
            Assert.Equal("AGRI", item.ShortId);
            Assert.NotEmpty(item.Labels);
            Assert.Equal(new[] { "GET /codelists/item" }, handler.Requests);

            handler.Requests.Clear();
            item = await client.GetCodelistItem(DcatDataset.ThemeCodelist, "agri");
            Assert.NotNull(item);
            Assert.Equal("http://publications.europa.eu/resource/authority/data-theme/AGRI", item.Id);
            Assert.Equal(new[] { "GET /codelists/item" }, handler.Requests);

            Assert.Null(await client.GetCodelistItem("http://example.com/unknown-codelist", "agri"));
        }

        [Fact]
        public async Task CodelistItemWithSpecialCharactersShouldBeFound()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            using CodelistApplicationFactory applicationFactory = new CodelistApplicationFactory(storage, new AllAccessFilePolicy());
            HttpClient httpClient = applicationFactory.CreateClient();
            DefaultHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(httpClient);
            CodelistProviderClient client = new CodelistProviderClient(httpClientFactory, new AnonymousHttpContextValueAccessor());

            string itemId = DcatDistribution.MediaTypeCodelist + "/application/ld+json";
            string content = "@prefix skos: <http://www.w3.org/2004/02/skos/core#> .\n" +
                $"<{DcatDistribution.MediaTypeCodelist}> a skos:ConceptScheme ; skos:prefLabel \"Media types\"@sk .\n" +
                $"<{itemId}> a skos:Concept ; skos:prefLabel \"JSON-LD\"@sk .\n";
            Assert.True(await client.UpdateCodelist(new MemoryStream(Encoding.UTF8.GetBytes(content))));

            CodelistItem? item = await client.GetCodelistItem(DcatDistribution.MediaTypeCodelist, itemId);
            Assert.NotNull(item);
            Assert.Equal(itemId, item.Id);
            Assert.Equal("ld-json", item.ShortId);

            item = await client.GetCodelistItem(DcatDistribution.MediaTypeCodelist, "ld-json");
            Assert.NotNull(item);
            Assert.Equal(itemId, item.Id);
        }
    }
}
