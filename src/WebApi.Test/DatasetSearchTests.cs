using Abstractions;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TestBase;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Algebra;
using static Lucene.Net.Documents.Field;

namespace WebApi.Test
{
    public class DatasetSearchTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        public DatasetSearchTests(StorageFixture fixture)
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

            AbstractResponse<DatasetView>? result = await client.SearchDatasets(JsonContent.Create(new { }));
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task TestOneResult()
        {
            string path = fixture.GetStoragePath();
            Guid id = fixture.CreateDataset("Test", PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            
            AbstractResponse<DatasetView>? result = await client.SearchDatasets(JsonContent.Create(new { }));
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);
            DatasetView view = result.Items[0];
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
                ids.Add(fixture.CreateDataset($"Test {i.ToString().PadLeft(2, '0')}", PublisherId));
            }

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;
            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name" }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(ids.Take(10), result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", PageSize = 10 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(ids.Take(10), result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", PageSize = 10, Page = 2 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(ids.Skip(10), result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", PageSize = 5 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(ids.Take(5), result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", PageSize = 5, Page = 2 }));
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(ids.Skip(5).Take(5), result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task QueryTextTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId);
            Guid id2 = fixture.CreateDataset("Zoznam faktúr", PublisherId);
            Guid id3 = fixture.CreateDataset("Kompletný zoznam objednávok", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { QueryText = "" }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { QueryText = "poriadky" }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(id1, result.Items[0].Id);

            result = await client.SearchDatasets(JsonContent.Create(new { QueryText = "zoznam" }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id3, id2 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { QueryText = "ministerstvo" }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task FilterByPublisherTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId);
            Guid id2 = fixture.CreateDataset("Zoznam faktúr", "http://example.com/publisher2");
            Guid id3 = fixture.CreateDataset("Kompletný zoznam objednávok", "http://example.com/publisher2");

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

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "publishers", Array.Empty<string>() } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "publishers", new[] { PublisherId } } } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "publishers", new[] { PublisherId, "http://example.com/publisher2" } } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "publishers", new[] { "http://example.com/publisher3" } } } }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "publishers", new[] { PublisherId } } }, RequiredFacets = new[] { "publishers" } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id1 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);

            result = await client.SearchDatasets(JsonContent.Create(new { RequiredFacets = new[] { "publishers" } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);
        }

        [Fact]
        public async Task FilterByCodelist()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId, type: new Uri("http://data.gov.sk/1"));
            Guid id2 = fixture.CreateDataset("Zoznam faktúr", "http://example.com/publisher2");
            Guid id3 = fixture.CreateDataset("Kompletný zoznam objednávok", "http://example.com/publisher2", type: new Uri("http://data.gov.sk/2"));
            Guid id4 = fixture.CreateDataset("Rozpočet", PublisherId, type: new Uri("http://data.gov.sk/2"));

            void AssertFacets(List<Facet>? facets)
            {
                Assert.NotNull(facets);
                Assert.Single(facets);
                Facet facet = facets[0];
                Assert.Equal("https://data.gov.sk/set/codelist/dataset-type", facet.Id);
                Assert.Equal(new Dictionary<string, int> { { "http://data.gov.sk/1", 1 }, { "http://data.gov.sk/2", 2 } }, facet.Values);
            }

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "https://data.gov.sk/set/codelist/dataset-type", Array.Empty<string>() } } }));
            Assert.Equal(4, result.TotalCount);
            Assert.Equal(4, result.Items.Count);
            Assert.Equal(new[] { id4, id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "https://data.gov.sk/set/codelist/dataset-type", new[] { "http://data.gov.sk/1" } } } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "https://data.gov.sk/set/codelist/dataset-type", new[] { "http://data.gov.sk/1", "http://data.gov.sk/2" } } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id4, id3, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "https://data.gov.sk/set/codelist/dataset-type", new[] { "http://data.gov.sk/3" } } } }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "https://data.gov.sk/set/codelist/dataset-type", new[] { "http://data.gov.sk/1" } } }, RequiredFacets = new[] { "https://data.gov.sk/set/codelist/dataset-type" } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id1 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);

            result = await client.SearchDatasets(JsonContent.Create(new { RequiredFacets = new[] { "https://data.gov.sk/set/codelist/dataset-type" } }));
            Assert.Equal(4, result.TotalCount);
            Assert.Equal(4, result.Items.Count);
            Assert.Equal(new[] { id4, id3, id2, id1 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);
        }

        [Fact]
        public async Task FilterByKeywords()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId, keywordsSk: new[] { "faktúry" });
            Guid id2 = fixture.CreateDataset("Zoznam faktúr", "http://example.com/publisher2");
            Guid id3 = fixture.CreateDataset("Kompletný zoznam objednávok", "http://example.com/publisher2", keywordsSk: new[] { "objednávky" });
            Guid id4 = fixture.CreateDataset("Rozpočet", PublisherId, type: new Uri("http://data.gov.sk/2"), keywordsSk: new[] { "faktúry", "objednávky" });

            void AssertFacets(List<Facet>? facets)
            {
                Assert.NotNull(facets);
                Assert.Single(facets);
                Facet facet = facets[0];
                Assert.Equal("keywords", facet.Id);
                Assert.Equal(new Dictionary<string, int> { { "faktúry", 2 }, { "objednávky", 2 } }, facet.Values);
            }

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "keywords", Array.Empty<string>() } } }));
            Assert.Equal(4, result.TotalCount);
            Assert.Equal(4, result.Items.Count);
            Assert.Equal(new[] { id4, id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "keywords", new[] { "faktúry" } } } }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id4, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "keywords", new[] { "faktúry", "objednávky" } } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id4, id3, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "keywords", new[] { "dobropisy" } } } }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "keywords", new[] { "faktúry" } } }, RequiredFacets = new[] { "keywords" } }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id4, id1 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);

            result = await client.SearchDatasets(JsonContent.Create(new { RequiredFacets = new[] { "keywords" } }));
            Assert.Equal(4, result.TotalCount);
            Assert.Equal(4, result.Items.Count);
            Assert.Equal(new[] { id4, id3, id2, id1 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);
        }

        [Fact]
        public async Task FilterByIdTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId);
            Guid id2 = fixture.CreateDataset("Zoznam faktúr", "http://example.com/publisher2");
            Guid id3 = fixture.CreateDataset("Kompletný zoznam objednávok", "http://example.com/publisher2");

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "id", Array.Empty<string>() } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "id", new[] { id2.ToString() } } } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id2 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "id", new[] { id2.ToString(), id3.ToString() } } } }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id3, id2 }, result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task FilterByNonExistingIdTest()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreateDataset("Cestovné poriadky", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "id", new[] { "test" } } } }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task FilterByParentTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId);
            Guid id2 = fixture.CreateDataset("Zoznam faktúr", "http://example.com/publisher2", parent: id1);
            Guid id3 = fixture.CreateDataset("Kompletný zoznam objednávok", "http://example.com/publisher2", parent: id2);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "parent", Array.Empty<string>() } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "parent", new[] { id1.ToString() } } } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id2 }, result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task FilterBySiblingTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId);
            Guid id2 = fixture.CreateDataset("Zoznam faktúr", "http://example.com/publisher2", parent: id1);
            Guid id3 = fixture.CreateDataset("Kompletný zoznam objednávok", "http://example.com/publisher2", parent: id1);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "sibling", Array.Empty<string>() } } }));
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(new[] { id3, id2, id1 }, result.Items.Select(i => i.Id));

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string[]> { { "sibling", new[] { id2.ToString() } } } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id3 }, result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task FilterByThemeTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId);
            Guid id2 = fixture.CreateDataset("Cestovné poriadky2", PublisherId);

            using Storage storage = new Storage(path);

            IFileStorageAccessPolicy accessPolicy = new AllAccessFilePolicy();
            FileState state = storage.GetFileState(id1, accessPolicy)!;
            DcatDataset dataset = DcatDataset.Parse(state.Content!)!;
            dataset.Themes = new[] { new Uri("http://publications.europa.eu/resource/dataset/data-theme/1") };
            FileMetadata metadata = dataset.UpdateMetadata(true, null, state.Metadata);
            storage.InsertFile(dataset.ToString(), metadata, true, accessPolicy);

            void AssertFacets(List<Facet>? facets)
            {
                Assert.NotNull(facets);
                Assert.Single(facets);
                Facet facet = facets[0];
                Assert.Equal(DcatDataset.ThemeCodelist, facet.Id);
                Assert.Equal(new Dictionary<string, int> { { "http://publications.europa.eu/resource/dataset/data-theme/1", 1 } }, facet.Values);
            }

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", Filters = new Dictionary<string, string[]> { { DcatDataset.ThemeCodelist, Array.Empty<string>() } }, RequiredFacets = new[] { DcatDataset.ThemeCodelist } }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id1, id2 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", Filters = new Dictionary<string, string[]> { { DcatDataset.ThemeCodelist, new[] { "http://publications.europa.eu/resource/dataset/data-theme/1" } } }, RequiredFacets = new[] { DcatDataset.ThemeCodelist } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id1 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", Filters = new Dictionary<string, string[]> { { DcatDataset.ThemeCodelist, new[] { "http://publications.europa.eu/resource/dataset/data-theme/2" } } }, RequiredFacets = new[] { DcatDataset.ThemeCodelist } }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
            AssertFacets(result.Facets);
        }

        [Fact]
        public async Task FilterByFormatTest()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId);
            Guid id2 = fixture.CreateDataset("Cestovné poriadky2", PublisherId);

            using Storage storage = new Storage(path);

            IFileStorageAccessPolicy accessPolicy = new AllAccessFilePolicy();
            fixture.CreateDistrbution(storage.GetFileState(id1, accessPolicy)!,
                new Uri("https://data.gov.sk/def/ontology/law/authorsWorkType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/originalDatabaseType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1"),
                new Uri("http://data.gov.sk/download"),
                new Uri("http://data.gov.sk/"),
                new Uri("http://publications.europa.eu/resource/dataset/file-type/1"),
                new Uri("http://www.iana.org/assignments/media-types/text/csv"));

            fixture.CreateDistrbution(storage.GetFileState(id1, accessPolicy)!,
                new Uri("https://data.gov.sk/def/ontology/law/authorsWorkType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/originalDatabaseType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1"),
                new Uri("http://data.gov.sk/download"),
                new Uri("http://data.gov.sk/"),
                new Uri("http://publications.europa.eu/resource/dataset/file-type/2"),
                new Uri("http://www.iana.org/assignments/media-types/text/xml"));

            fixture.CreateDistrbution(storage.GetFileState(id2, accessPolicy)!,
                new Uri("https://data.gov.sk/def/ontology/law/authorsWorkType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/originalDatabaseType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1"),
                new Uri("http://data.gov.sk/download"),
                new Uri("http://data.gov.sk/"),
                new Uri("http://publications.europa.eu/resource/dataset/file-type/2"),
                new Uri("http://www.iana.org/assignments/media-types/text/xml"));

            fixture.CreateDistrbution(storage.GetFileState(id2, accessPolicy)!,
                new Uri("https://data.gov.sk/def/ontology/law/authorsWorkType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/originalDatabaseType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1"),
                new Uri("http://data.gov.sk/download"),
                new Uri("http://data.gov.sk/"),
                new Uri("http://publications.europa.eu/resource/dataset/file-type/3"),
                new Uri("http://www.iana.org/assignments/media-types/application/zip"));

            void AssertFacets(List<Facet>? facets)
            {
                Assert.NotNull(facets);
                Assert.Single(facets);
                Facet facet = facets[0];
                Assert.Equal(DcatDistribution.FormatCodelist, facet.Id);
                Assert.Equal(new Dictionary<string, int> { 
                    { "http://publications.europa.eu/resource/dataset/file-type/1", 1 },
                    { "http://publications.europa.eu/resource/dataset/file-type/2", 2 },
                    { "http://publications.europa.eu/resource/dataset/file-type/3", 1 }
                }, facet.Values);
            }

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", Filters = new Dictionary<string, string[]> { { DcatDistribution.FormatCodelist, Array.Empty<string>() } }, RequiredFacets = new[] { DcatDistribution.FormatCodelist } }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id1, id2 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", Filters = new Dictionary<string, string[]> { { DcatDistribution.FormatCodelist, new[] { "http://publications.europa.eu/resource/dataset/file-type/1" } } }, RequiredFacets = new[] { DcatDistribution.FormatCodelist } }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id1 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);

            result = await client.SearchDatasets(JsonContent.Create(new
            {
                OrderBy = "name",
                Filters = new Dictionary<string, string[]> { { DcatDistribution.FormatCodelist, new[] {
                    "http://publications.europa.eu/resource/dataset/file-type/1",
                    "http://publications.europa.eu/resource/dataset/file-type/2"
                } } },
                RequiredFacets = new[] { DcatDistribution.FormatCodelist }
            }));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id1, id2 }, result.Items.Select(i => i.Id));
            AssertFacets(result.Facets);

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", Filters = new Dictionary<string, string[]> { { DcatDistribution.FormatCodelist, new[] { "http://publications.europa.eu/resource/dataset/data-theme/4" } } }, RequiredFacets = new[] { DcatDistribution.FormatCodelist } }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
            AssertFacets(result.Facets);
        }

        [Fact]
        public async Task ResultsInAnotherLanguage()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId, nameEn: "Timetables");
            Guid id2 = fixture.CreateDataset("Zoznam faktúr", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { OrderBy = "name", Language = "en" } ));
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new[] { id1, id2 }, result.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task FullEntityMapTest()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid id, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            DatasetView view = result.Items[0];

            Assert.Equal(id, view.Id);
            Assert.True(view.IsPublic);
            Assert.Equal("titleSk", view.Name);
            Assert.Equal("descriptionSk", view.Description);
            Assert.Equal("https://data.gov.sk/id/publisher/full", view.PublisherId);
            Assert.Equal(new[] { new Uri("http://publications.europa.eu/resource/dataset/data-theme/1"), new Uri("http://publications.europa.eu/resource/dataset/data-theme/2") }, view.Themes);
            Assert.Equal(new Uri("http://publications.europa.eu/resource/dataset/frequency/1"), view.AccrualPeriodicity);
            Assert.Equal(new[] { "keyword1Sk", "keyword2Sk" }, view.Keywords);
            Assert.Equal(new[] { new Uri("https://data.gov.sk/set/codelist/dataset-type/1") }, view.Type);
            Assert.Equal(new[] { new Uri("http://publications.europa.eu/resource/dataset/country/1"), new Uri("http://publications.europa.eu/resource/dataset/country/2") }, view.Spatial);
            Assert.NotNull(view.Temporal);
            Assert.Equal("16. 8. 2023", view.Temporal.StartDate);
            Assert.Equal("10. 9. 2023", view.Temporal.EndDate);
            Assert.NotNull(view.ContactPoint);
            Assert.Equal("nameSk", view.ContactPoint.Name);
            Assert.Equal("test@example.com", view.ContactPoint.Email);
            Assert.Equal(new Uri("http://example.com/documentation"), view.LandingPage);
            Assert.Equal(new Uri("http://example.com/specification"), view.Specification);
            Assert.Equal(new[] { new Uri("http://eurovoc.europa.eu/6409"), new Uri("http://eurovoc.europa.eu/6410") }, view.EuroVocThemes);
            Assert.Equal(10, view.SpatialResolutionInMeters);
            Assert.Equal("P2D", view.TemporalResolution);
            Assert.Equal("XXX", view.IsPartOf);

            Assert.NotNull(view.ThemeValues);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/data-theme/1", "theme1sk"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/data-theme/2", "theme2sk")
            }, view.ThemeValues);

            Assert.NotNull(view.SpatialValues);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/country/1", "country1sk"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/country/2", "country2sk")
            }, view.SpatialValues);

            Assert.Equal(new[] { new CodelistItemView("https://data.gov.sk/set/codelist/dataset-type/1", "type1sk") }, view.TypeValues);

            Assert.Equal(new CodelistItemView("http://publications.europa.eu/resource/dataset/frequency/1", "frequency1sk"), view.AccrualPeriodicityValue);

            Assert.NotNull(view.EuroVocThemeValues);
            Assert.Equal(new[] {
                "nepovolená likvidácia odpadu",
                "chemický odpad"
            }, view.EuroVocThemeValues);

            Assert.Equivalent(new PublisherView
            {
                Name = "Ministerstvo hospodárstva SR",
                Id = publisherId,
                DatasetCount = 0,
                Key = "https://data.gov.sk/id/publisher/full",
                Themes = null,
                IsPublic = true
            }, view.Publisher);

            Assert.Single(view.Distributions);
            DistributionView distribution = view.Distributions[0];

            Assert.Equal(distributions[0], distribution.Id);
            Assert.Equal("TitleSk", distribution.Title);
            Assert.NotNull(distribution.TermsOfUse);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/authorsWorkType/1"), distribution.TermsOfUse.AuthorsWorkType);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/originalDatabaseType/1"), distribution.TermsOfUse.OriginalDatabaseType);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1"), distribution.TermsOfUse.DatabaseProtectedBySpecialRightsType);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1"), distribution.TermsOfUse.PersonalDataContainmentType);
            Assert.Equal(new Uri("https://data.gov.sk/download"), distribution.DownloadUrl);
            Assert.Equal(new Uri("https://data.gov.sk/"), distribution.AccessUrl);
            Assert.Equal(new Uri("http://publications.europa.eu/resource/dataset/file-type/1"), distribution.Format);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/text/csv"), distribution.MediaType);
            Assert.Equal(new Uri("http://data.gov.sk/specification"), distribution.ConformsTo);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/application/zip"), distribution.CompressFormat);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/application/zip"), distribution.PackageFormat);

            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/authorsWorkType/1", "work1sk"), distribution.TermsOfUse.AuthorsWorkTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/originalDatabaseType/1", "type1sk"), distribution.TermsOfUse.OriginalDatabaseTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1", "rights1sk"), distribution.TermsOfUse.DatabaseProtectedBySpecialRightsTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1", "personal1sk"), distribution.TermsOfUse.PersonalDataContainmentTypeValue);
            Assert.Equal(new CodelistItemView("http://publications.europa.eu/resource/dataset/file-type/1", "fileType1sk"), distribution.FormatValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/text/csv", "CSV"), distribution.MediaTypeValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/application/zip", "ZIP"), distribution.CompressFormatValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/application/zip", "ZIP"), distribution.PackageFormatValue);
        }

        [Fact]
        public async Task FullEntityMapTestInAnotherLanguage()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid id, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { Language = "en" }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            DatasetView view = result.Items[0];

            Assert.Equal(id, view.Id);
            Assert.True(view.IsPublic);
            Assert.Equal("titleEn", view.Name);
            Assert.Equal("descriptionEn", view.Description);
            Assert.Equal("https://data.gov.sk/id/publisher/full", view.PublisherId);
            Assert.Equal(new[] { new Uri("http://publications.europa.eu/resource/dataset/data-theme/1"), new Uri("http://publications.europa.eu/resource/dataset/data-theme/2") }, view.Themes);
            Assert.Equal(new Uri("http://publications.europa.eu/resource/dataset/frequency/1"), view.AccrualPeriodicity);
            Assert.Equal(new[] { "keyword1En", "keyword2En" }, view.Keywords);
            Assert.Equal(new[] { new Uri("https://data.gov.sk/set/codelist/dataset-type/1") }, view.Type);
            Assert.Equal(new[] { new Uri("http://publications.europa.eu/resource/dataset/country/1"), new Uri("http://publications.europa.eu/resource/dataset/country/2") }, view.Spatial);
            Assert.NotNull(view.Temporal);
            Assert.Equal("16. 8. 2023", view.Temporal.StartDate);
            Assert.Equal("10. 9. 2023", view.Temporal.EndDate);
            Assert.NotNull(view.ContactPoint);
            Assert.Equal("nameEn", view.ContactPoint.Name);
            Assert.Equal("test@example.com", view.ContactPoint.Email);
            Assert.Equal(new Uri("http://example.com/documentation"), view.LandingPage);
            Assert.Equal(new Uri("http://example.com/specification"), view.Specification);
            Assert.Equal(new[] { new Uri("http://eurovoc.europa.eu/6409"), new Uri("http://eurovoc.europa.eu/6410") }, view.EuroVocThemes);
            Assert.Equal(10, view.SpatialResolutionInMeters);
            Assert.Equal("P2D", view.TemporalResolution);
            Assert.Equal("XXX", view.IsPartOf);
            
            Assert.NotNull(view.ThemeValues);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/data-theme/1", "theme1en"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/data-theme/2", "theme2en")
            }, view.ThemeValues);

            Assert.NotNull(view.SpatialValues);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/country/1", "country1en"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/country/2", "country2en")
            }, view.SpatialValues);

            Assert.Equal(new[] { new CodelistItemView("https://data.gov.sk/set/codelist/dataset-type/1", "type1en") }, view.TypeValues);

            Assert.Equal(new CodelistItemView("http://publications.europa.eu/resource/dataset/frequency/1", "frequency1en"), view.AccrualPeriodicityValue);

            Assert.NotNull(view.EuroVocThemeValues);
            Assert.Equal(new[] {
                "unauthorised dumping", 
                "chemical waste"
            }, view.EuroVocThemeValues);

            Assert.Equivalent(new PublisherView
            {
                Name = "Ministry of economy",
                Id = publisherId,
                DatasetCount = 0,
                Key = "https://data.gov.sk/id/publisher/full",
                Themes = null,
                IsPublic = true
            }, view.Publisher);

            Assert.Single(view.Distributions);
            DistributionView distribution = view.Distributions[0];

            Assert.Equal(distributions[0], distribution.Id);
            Assert.Equal("TitleEn", distribution.Title);
            Assert.NotNull(distribution.TermsOfUse);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/authorsWorkType/1"), distribution.TermsOfUse.AuthorsWorkType);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/originalDatabaseType/1"), distribution.TermsOfUse.OriginalDatabaseType);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1"), distribution.TermsOfUse.DatabaseProtectedBySpecialRightsType);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1"), distribution.TermsOfUse.PersonalDataContainmentType);
            Assert.Equal(new Uri("https://data.gov.sk/download"), distribution.DownloadUrl);
            Assert.Equal(new Uri("https://data.gov.sk/"), distribution.AccessUrl);
            Assert.Equal(new Uri("http://publications.europa.eu/resource/dataset/file-type/1"), distribution.Format);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/text/csv"), distribution.MediaType);
            Assert.Equal(new Uri("http://data.gov.sk/specification"), distribution.ConformsTo);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/application/zip"), distribution.CompressFormat);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/application/zip"), distribution.PackageFormat);

            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/authorsWorkType/1", "work1en"), distribution.TermsOfUse.AuthorsWorkTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/originalDatabaseType/1", "type1en"), distribution.TermsOfUse.OriginalDatabaseTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1", "rights1en"), distribution.TermsOfUse.DatabaseProtectedBySpecialRightsTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1", "personal1en"), distribution.TermsOfUse.PersonalDataContainmentTypeValue);
            Assert.Equal(new CodelistItemView("http://publications.europa.eu/resource/dataset/file-type/1", "fileType1en"), distribution.FormatValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/text/csv", "CSV"), distribution.MediaTypeValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/application/zip", "ZIP"), distribution.CompressFormatValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/application/zip", "ZIP"), distribution.PackageFormatValue);
        }

        [Fact]
        public async Task FullEntityMapTestInAnotherLanguageIfDoesNotExists()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            Guid publisherId = fixture.CreatePublisher("Ministerstvo hospodárstva SR", PublisherId);

            DcatDataset dataset = DcatDataset.Create();

            Dictionary<string, string> names = new Dictionary<string, string> { { "sk", "TestSk" } };
            dataset.SetTitle(names);
            dataset.SetDescription(new Dictionary<string, string> { { "sk", "DescriptionSk" } });
            dataset.Type = new[] { new Uri("https://data.gov.sk/set/codelist/dataset-type/1") };
            dataset.Publisher = new Uri(PublisherId);
            dataset.Themes = new[] {
                new Uri("http://publications.europa.eu/resource/dataset/data-theme/1"),
                new Uri("http://publications.europa.eu/resource/dataset/data-theme/2"),
                new Uri(DcatDataset.EuroVocPrefix + "6409"),
                new Uri(DcatDataset.EuroVocPrefix +"6410")};
            dataset.SetKeywords(new Dictionary<string, List<string>> { { "sk", new List<string> { "keyword1", "keyword2" } } });
            dataset.SetContactPoint(new LanguageDependedTexts { { "sk", "ContactSk" } }, "test@test.sk");
            dataset.AccrualPeriodicity = new Uri("http://publications.europa.eu/resource/dataset/frequency/1");
            dataset.Spatial = new[] { new Uri("http://publications.europa.eu/resource/dataset/country/1"), new Uri("http://publications.europa.eu/resource/dataset/country/2") };
            dataset.SetTemporal(new DateOnly(2023, 8, 16), new DateOnly(2023, 9, 10));
            dataset.LandingPage = new Uri("http://example.com/documentation");
            dataset.Specification = new Uri("http://example.com/specification");
            dataset.SpatialResolutionInMeters = 10;
            dataset.TemporalResolution = "P2D";
            dataset.IsPartOf = new Uri("http://example.com/test-dataset");
            dataset.IsPartOfInternalId = "XXX";
            dataset.SetEuroVocLabelThemes(new Dictionary<string, List<string>> {
                { "sk", new List<string> { "nepovolená likvidácia odpadu", "chemický odpad" } },
                { "en", new List<string> { "unauthorised dumping", "chemical waste" } }
            });


            FileMetadata metadata = dataset.UpdateMetadata(true, null);
            FileState state = new FileState(metadata, dataset.ToString());
            fixture.CreateFile(state);
            Guid id = metadata.Id;

            Guid[] distributions = new[] { fixture.CreateDistrbution(state,
                new Uri("https://data.gov.sk/def/ontology/law/authorsWorkType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/originalDatabaseType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1"),
                new Uri("http://data.gov.sk/download"),
                new Uri("http://data.gov.sk/"),
                new Uri("http://publications.europa.eu/resource/dataset/file-type/1"),
                new Uri("http://www.iana.org/assignments/media-types/text/csv"),
                new Uri("http://data.gov.sk/specification"),
                new Uri("http://www.iana.org/assignments/media-types/application/zip"),
                new Uri("http://www.iana.org/assignments/media-types/application/zip"),
                "TitleSk",
                null,
                new Uri("http://example.com/access-service")),
            };

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { Language = "en" }));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            DatasetView view = result.Items[0];

            Assert.Equal(id, view.Id);
            Assert.True(view.IsPublic);
            Assert.Equal("TestSk", view.Name);
            Assert.Equal("DescriptionSk", view.Description);
            Assert.Equal(PublisherId, view.PublisherId);
            Assert.Equal(new[] { new Uri("http://publications.europa.eu/resource/dataset/data-theme/1"), new Uri("http://publications.europa.eu/resource/dataset/data-theme/2") }, view.Themes);
            Assert.Equal(new Uri("http://publications.europa.eu/resource/dataset/frequency/1"), view.AccrualPeriodicity);
            Assert.Equal(new[] { "keyword1", "keyword2" }, view.Keywords);
            Assert.Equal(new[] { new Uri("https://data.gov.sk/set/codelist/dataset-type/1") }, view.Type);
            Assert.Equal(new[] { new Uri("http://publications.europa.eu/resource/dataset/country/1"), new Uri("http://publications.europa.eu/resource/dataset/country/2") }, view.Spatial);
            Assert.NotNull(view.Temporal);
            Assert.Equal("16. 8. 2023", view.Temporal.StartDate);
            Assert.Equal("10. 9. 2023", view.Temporal.EndDate);
            Assert.NotNull(view.ContactPoint);
            Assert.Equal("ContactSk", view.ContactPoint.Name);
            Assert.Equal("test@test.sk", view.ContactPoint.Email);
            Assert.Equal(new Uri("http://example.com/documentation"), view.LandingPage);
            Assert.Equal(new Uri("http://example.com/specification"), view.Specification);
            Assert.Equal(new[] { new Uri("http://eurovoc.europa.eu/6409"), new Uri("http://eurovoc.europa.eu/6410") }, view.EuroVocThemes);
            Assert.Equal(10, view.SpatialResolutionInMeters);
            Assert.Equal("P2D", view.TemporalResolution);
            Assert.Equal("XXX", view.IsPartOf);

            Assert.NotNull(view.ThemeValues);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/data-theme/1", "theme1en"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/data-theme/2", "theme2en")
            }, view.ThemeValues);

            Assert.NotNull(view.SpatialValues);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/country/1", "country1en"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/country/2", "country2en")
            }, view.SpatialValues);

            Assert.Equal(new[] { new CodelistItemView("https://data.gov.sk/set/codelist/dataset-type/1", "type1en") }, view.TypeValues);

            Assert.Equal(new CodelistItemView("http://publications.europa.eu/resource/dataset/frequency/1", "frequency1en"), view.AccrualPeriodicityValue);

            Assert.NotNull(view.EuroVocThemeValues);
            Assert.Equal(new[] {
                "unauthorised dumping",
                "chemical waste"
            }, view.EuroVocThemeValues);

            Assert.Equivalent(new PublisherView
            {
                Name = "Ministerstvo hospodárstva SR",
                Id = publisherId,
                DatasetCount = 0,
                Key = PublisherId,
                Themes = null,
                IsPublic = true
            }, view.Publisher);

            Assert.Single(view.Distributions);
            DistributionView distribution = view.Distributions[0];

            Assert.Equal(distributions[0], distribution.Id);
            Assert.Equal("TitleSk", distribution.Title);
            Assert.NotNull(distribution.TermsOfUse);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/authorsWorkType/1"), distribution.TermsOfUse.AuthorsWorkType);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/originalDatabaseType/1"), distribution.TermsOfUse.OriginalDatabaseType);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1"), distribution.TermsOfUse.DatabaseProtectedBySpecialRightsType);
            Assert.Equal(new Uri("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1"), distribution.TermsOfUse.PersonalDataContainmentType);
            Assert.Equal(new Uri("https://data.gov.sk/download"), distribution.DownloadUrl);
            Assert.Equal(new Uri("https://data.gov.sk/"), distribution.AccessUrl);
            Assert.Equal(new Uri("http://publications.europa.eu/resource/dataset/file-type/1"), distribution.Format);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/text/csv"), distribution.MediaType);
            Assert.Equal(new Uri("http://data.gov.sk/specification"), distribution.ConformsTo);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/application/zip"), distribution.CompressFormat);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/application/zip"), distribution.PackageFormat);

            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/authorsWorkType/1", "work1en"), distribution.TermsOfUse.AuthorsWorkTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/originalDatabaseType/1", "type1en"), distribution.TermsOfUse.OriginalDatabaseTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1", "rights1en"), distribution.TermsOfUse.DatabaseProtectedBySpecialRightsTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1", "personal1en"), distribution.TermsOfUse.PersonalDataContainmentTypeValue);
            Assert.Equal(new CodelistItemView("http://publications.europa.eu/resource/dataset/file-type/1", "fileType1en"), distribution.FormatValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/text/csv", "CSV"), distribution.MediaTypeValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/application/zip", "ZIP"), distribution.CompressFormatValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/application/zip", "ZIP"), distribution.PackageFormatValue);
        }

        [Fact]
        public async Task DereferenceByDatasetUriShouldBeRedirected()
        {
            string path = fixture.GetStoragePath();

            Uri uri = new Uri($"https://data.gov.sk/set/{Guid.NewGuid()}");

            IGraph graph = new VDS.RDF.Graph();
            RdfDocument.AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("dcat:Dataset");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            DcatDataset dataset = new DcatDataset(graph, subject);

            using Storage storage = new Storage(path);

            FileMetadata metadata = dataset.UpdateMetadata(true, null);
            storage.InsertFile(dataset.ToString(), metadata, false, new AllAccessFilePolicy());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using HttpResponseMessage response = await client.GetAsync(uri.PathAndQuery);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("/datasety/" + metadata.Id.ToString(), response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task DereferenceByDatasetUriShouldBeRedirected2()
        {
            string path = fixture.GetStoragePath();

            Uri uri = new Uri($"https://data.gov.sk/set/{Guid.NewGuid()}/test");

            IGraph graph = new VDS.RDF.Graph();
            RdfDocument.AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("dcat:Dataset");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            DcatDataset dataset = new DcatDataset(graph, subject);

            using Storage storage = new Storage(path);

            FileMetadata metadata = dataset.UpdateMetadata(true, null);
            storage.InsertFile(dataset.ToString(), metadata, false, new AllAccessFilePolicy());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using HttpResponseMessage response = await client.GetAsync(uri.PathAndQuery);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("/datasety/" + metadata.Id.ToString(), response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task DereferenceByDistributionUriShouldBeRedirected()
        {
            string path = fixture.GetStoragePath();

            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            Uri uri = new Uri($"https://data.gov.sk/set/{Guid.NewGuid()}/{Guid.NewGuid()}");

            IGraph graph = new VDS.RDF.Graph();
            RdfDocument.AddDefaultNamespaces(graph);
            IUriNode subject = graph.CreateUriNode(uri);
            IUriNode rdfTypeNode = graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            IUriNode targetTypeNode = graph.CreateUriNode("dcat:Distribution");
            graph.Assert(subject, rdfTypeNode, targetTypeNode);
            DcatDistribution distribution = new DcatDistribution(graph, subject);

            using Storage storage = new Storage(path);

            FileMetadata metadata = distribution.UpdateMetadata(datasetId, PublisherId);
            storage.InsertFile(distribution.ToString(), metadata, false, new AllAccessFilePolicy());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using HttpResponseMessage response = await client.GetAsync(uri.PathAndQuery);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("/datasety/" + datasetId.ToString(), response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task DereferenceByLandingPageShouldBeRedirected()
        {
            string path = fixture.GetStoragePath();

            Uri uri = new Uri($"https://data.gov.sk/dataset/{Guid.NewGuid()}");

            DcatDataset dataset = DcatDataset.Create();
            dataset.LandingPage = uri;

            using Storage storage = new Storage(path);

            FileMetadata metadata = dataset.UpdateMetadata(true, null);
            storage.InsertFile(dataset.ToString(), metadata, false, new AllAccessFilePolicy());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using HttpResponseMessage response = await client.GetAsync(uri.PathAndQuery);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("/datasety/" + metadata.Id.ToString(), response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task DereferenceByLandingPageShouldBeRedirected2()
        {
            string path = fixture.GetStoragePath();

            Uri uri = new Uri($"https://data.gov.sk/dataset/test");

            DcatDataset dataset = DcatDataset.Create();
            dataset.LandingPage = uri;

            using Storage storage = new Storage(path);

            FileMetadata metadata = dataset.UpdateMetadata(true, null);
            storage.InsertFile(dataset.ToString(), metadata, false, new AllAccessFilePolicy());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using HttpResponseMessage response = await client.GetAsync("/datasety/test");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("/datasety/" + metadata.Id.ToString(), response.Headers.Location.OriginalString);
        }


        [Fact]
        public async Task DereferenceByLandingPageShouldBeRedirected3()
        {
            string path = fixture.GetStoragePath();

            Uri uri = new Uri("https://data.gov.sk/set/codelist/CL003003");

            DcatDataset dataset = DcatDataset.Create();
            dataset.LandingPage = uri;

            using Storage storage = new Storage(path);

            FileMetadata metadata = dataset.UpdateMetadata(true, null);
            storage.InsertFile(dataset.ToString(), metadata, false, new AllAccessFilePolicy());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using HttpResponseMessage response = await client.GetAsync("/set/codelist/CL003003");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("/datasety/" + metadata.Id.ToString(), response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task DereferenceByLandingPageShouldBeRedirected4()
        {
            string path = fixture.GetStoragePath();

            Uri uri = new Uri("https://data.gov.sk/set/codelist/CL003003");

            DcatDataset dataset = DcatDataset.Create();
            dataset.LandingPage = uri;

            using Storage storage = new Storage(path);

            FileMetadata metadata = dataset.UpdateMetadata(true, null);
            storage.InsertFile(dataset.ToString(), metadata, false, new AllAccessFilePolicy());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using HttpResponseMessage response = await client.GetAsync("/datasety/codelist/CL003003");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("/datasety/" + metadata.Id.ToString(), response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task FilterWitnNullKeyShouldReturnEmptyResult()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDataset("Cestovné poriadky", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { Filters = new Dictionary<string, string?[]> { { "key", new string?[] { null } } } }));
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }
    }
}
