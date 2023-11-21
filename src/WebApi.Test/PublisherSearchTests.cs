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
    public class PublisherSearchTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        public PublisherSearchTests(StorageFixture fixture)
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

            AbstractResponse<PublisherView>? result = await client.SearchPublishers(JsonContent.Create(new { }));
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task TestOneResult()
        {
            string path = fixture.GetStoragePath();
            Guid id = fixture.CreatePublisher("Test");
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<PublisherView>? result = await client.SearchPublishers(JsonContent.Create(new { }));
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);
            PublisherView view = result.Items[0];
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
                ids.Add(fixture.CreatePublisher($"Test {i.ToString().PadLeft(2, '0')}"));
            }

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<PublisherView> result;
            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name" }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(ids.Take(10), result.Items.Select(i => i.Id));

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", PageSize = 10 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(ids.Take(10), result.Items.Select(i => i.Id));

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", PageSize = 10, Page = 2 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(ids.Skip(10), result.Items.Select(i => i.Id));

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", PageSize = 5 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(ids.Take(5), result.Items.Select(i => i.Id));

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", PageSize = 5, Page = 2 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(ids.Skip(5).Take(5), result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task QueryTextTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreatePublisher("Ministerstvo vnútra SR");
            Guid id2 = fixture.CreatePublisher("Štatistický úrad SR");
            Guid id3 = fixture.CreatePublisher("Ministerstvo hospodárstva");

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<PublisherView> result;

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", QueryText = "" }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id1, id2 }, result.Items.Select(i => i.Id));

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", QueryText = "urad" }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(id2, result.Items[0].Id);

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", QueryText = "ministerstvo" }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id3, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", QueryText = "narodna" }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task FilterByIdTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreatePublisher("Ministerstvo vnútra SR");
            Guid id2 = fixture.CreatePublisher("Štatistický úrad SR");
            Guid id3 = fixture.CreatePublisher("Ministerstvo hospodárstva");

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<PublisherView> result;

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", Filters = new Dictionary<string, string[]> { { "id", Array.Empty<string>() } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id1, id2 }, result.Items.Select(i => i.Id));

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", Filters = new Dictionary<string, string[]> { { "id", new[] { id2.ToString() } } } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id2 }, result.Items.Select(i => i.Id));

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", Filters = new Dictionary<string, string[]> { { "id", new[] { id2.ToString(), id3.ToString() } } } }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id3, id2 }, result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task ResultsInAnotherLanguage()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreatePublisher("Ministerstvo vnútra SR");
            Guid id2 = fixture.CreatePublisher("Štatistický úrad SR");
            Guid id3 = fixture.CreatePublisher("Ministerstvo hospodárstva", nameEn: "Ministry of economy");

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<PublisherView> result;

            result = await client.SearchPublishers(JsonContent.Create(new { OrderBy = "name", Language = "en" }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id1, id3, id2 }, result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task FullEntityMapTest()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Ministerstvo vnútra SR", nameEn: "Ministry of economy");

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<PublisherView> result;

            result = await client.SearchPublishers(JsonContent.Create(new { }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            PublisherView view = result.Items[0];

            Assert.Equal(id, view.Id);
            Assert.Equal("Ministerstvo vnútra SR", view.Name);
        }

        [Fact]
        public async Task FullEntityMapTestInAnotherLanguage()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("Ministerstvo vnútra SR", nameEn: "Ministry of economy");

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<PublisherView> result;

            result = await client.SearchPublishers(JsonContent.Create(new { Language = "en" }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            PublisherView view = result.Items[0];

            Assert.Equal(id, view.Id);
            Assert.Equal("Ministry of economy", view.Name);
        }
    }
}
