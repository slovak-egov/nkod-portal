using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace WebApi.Test
{
    public class LocalCatalogSearchTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        public LocalCatalogSearchTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task TestEmptySearch()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<LocalCatalogView>? result = await client.SearchLocalCatalogs(JsonContent.Create(new { }));
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task TestOneResult()
        {
            string path = fixture.GetStoragePath();
            Guid id = fixture.CreateLocalCatalog("Test", PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<LocalCatalogView>? result = await client.SearchLocalCatalogs(JsonContent.Create(new { }));
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);
            LocalCatalogView view = result.Items[0];
            Assert.Equal(id, view.Id);
            Assert.Equal("Test", view.Name);
        }

        [Fact]
        public async Task TestPageAndSize()
        {
            string path = fixture.GetStoragePath();
            List<Guid> ids = new List<Guid>(11);
            for (int i = 0; i < ids.Capacity; i++)
            {
                ids.Add(fixture.CreateLocalCatalog($"Test {i.ToString().PadLeft(2, '0')}", PublisherId));
            }

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<LocalCatalogView> result;
            result = await client.SearchLocalCatalogs(JsonContent.Create(new { OrderBy = "name" }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(ids.Take(10), result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { OrderBy = "name", PageSize = 10 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(ids.Take(10), result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { OrderBy = "name", PageSize = 10, Page = 2 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(ids.Skip(10), result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { OrderBy = "name", PageSize = 5 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(ids.Take(5), result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { OrderBy = "name", PageSize = 5, Page = 2 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(ids.Skip(5).Take(5), result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task QueryTextTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateLocalCatalog("Cestovné poriadky", PublisherId);
            Guid id2 = fixture.CreateLocalCatalog("Zoznam faktúr", PublisherId);
            Guid id3 = fixture.CreateLocalCatalog("Kompletný zoznam objednávok", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<LocalCatalogView> result;

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { QueryText = "" }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { QueryText = "poriadky" }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(id1, result.Items[0].Id);

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { QueryText = "zoznam" }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id3, id2 }, result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { QueryText = "ministerstvo" }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task FilterByPublisherTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateLocalCatalog("Cestovné poriadky", PublisherId);
            Guid id2 = fixture.CreateLocalCatalog("Zoznam faktúr", "http://example.com/publisher2");
            Guid id3 = fixture.CreateLocalCatalog("Kompletný zoznam objednávok", "http://example.com/publisher2");

            void AssertFacets(List<Facet>? facets)
            {
                Assert.NotNull(facets);
                Assert.Single(facets);
                Facet facet = facets[0];
                Assert.Equal("publishers", facet.Id);
                Assert.Equal(new Dictionary<string, int> { { PublisherId, 1 }, { "http://example.com/publisher2", 2 } }, facet.Values);
            }

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<LocalCatalogView> result;

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "publishers", Array.Empty<string>() } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "publishers", new[] { PublisherId } } } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "publishers", new[] { PublisherId, "http://example.com/publisher2" } } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "publishers", new[] { "http://example.com/publisher3" } } } }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "publishers", new[] { PublisherId } } }, RequiredFacets = new[] { "publishers" } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id1 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { RequiredFacets = new[] { "publishers" } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);
        }

        [Fact]
        public async Task FilterByIdTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateLocalCatalog("Cestovné poriadky", PublisherId);
            Guid id2 = fixture.CreateLocalCatalog("Zoznam faktúr", "http://example.com/publisher2");
            Guid id3 = fixture.CreateLocalCatalog("Kompletný zoznam objednávok", "http://example.com/publisher2");

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<LocalCatalogView> result;

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "id", Array.Empty<string>() } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "id", new[] { id2.ToString() } } } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id2 }, result.Items.Select(i => i.Id));

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "id", new[] { id2.ToString(), id3.ToString() } } } }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id3, id2 }, result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task ResultsInAnotherLanguage()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateLocalCatalog("Cestovné poriadky", PublisherId, nameEn: "Timetables");
            Guid id2 = fixture.CreateLocalCatalog("Zoznam faktúr", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<LocalCatalogView> result;

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { OrderBy = "name", Language = "en" }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id1, id2 }, result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task FullEntityMapTest()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateLocalCatalogCodelists();
            fixture.CreateDistributionCodelists();
            (Guid id, Guid publisherId) = fixture.CreateFullLocalCatalog();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<LocalCatalogView> result;

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { Language = "en" }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            LocalCatalogView view = result.Items[0];

            Assert.Equal(id, view.Id);
            Assert.Equal("titleEn", view.Name);
            Assert.Equal("descriptionEn", view.Description);
            Assert.Equal("https://data.gov.sk/id/publisher/full", view.PublisherId);
            Assert.NotNull(view.ContactPoint);
            Assert.Equal("nameEn", view.ContactPoint.Name);
            Assert.Equal("test@example.com", view.ContactPoint.Email);
            Assert.Equal(new Uri("http://data.gov.sk"), view.HomePage);

            Assert.Equivalent(new PublisherView
            {
                Name = "Ministry of economy",
                Id = publisherId,
                DatasetCount = 0,
                Key = "https://data.gov.sk/id/publisher/full",
                Themes = null,
                IsPublic = true
            }, view.Publisher);
        }

        [Fact]
        public async Task FullEntityMapTestInAnotherLanguage()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateLocalCatalogCodelists();
            fixture.CreateDistributionCodelists();
            (Guid id, Guid publisherId) = fixture.CreateFullLocalCatalog();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<LocalCatalogView> result;

            result = await client.SearchLocalCatalogs(JsonContent.Create(new { }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            LocalCatalogView view = result.Items[0];

            Assert.Equal(id, view.Id);
            Assert.Equal("titleSk", view.Name);
            Assert.Equal("descriptionSk", view.Description);
            Assert.Equal("https://data.gov.sk/id/publisher/full", view.PublisherId);
            Assert.NotNull(view.ContactPoint);
            Assert.Equal("nameSk", view.ContactPoint.Name);
            Assert.Equal("test@example.com", view.ContactPoint.Email);
            Assert.Equal(new Uri("http://data.gov.sk"), view.HomePage);

            Assert.Equivalent(new PublisherView
            {
                Name = "Ministerstvo hospodárstva SR",
                Id = publisherId,
                DatasetCount = 0,
                Key = "https://data.gov.sk/id/publisher/full",
                Themes = null,
                IsPublic = true
            }, view.Publisher);
        }
    }
}
