using Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
using VDS.RDF.Query.Algebra;

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
        public async Task ResultsInAnotherLanguage()
        {
            string path = fixture.GetStoragePath();

            Guid id1 = fixture.CreateDataset("Cestovné poriadky", PublisherId, nameEn: "Timetables");
            Guid id2 = fixture.CreateDataset("Zoznam faktúr", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            AbstractResponse<DatasetView> result;

            result = await client.SearchDatasets(JsonContent.Create(new { Language = "en" } ));
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(new[] { id1 }, result.Items.Select(i => i.Id));
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
            Assert.Equal(new Uri("https://data.gov.sk/set/codelist/dataset-type/1"), view.Type);
            Assert.Equal(new[] { new Uri("http://publications.europa.eu/resource/dataset/country/1"), new Uri("http://publications.europa.eu/resource/dataset/country/2") }, view.Spatial);
            Assert.NotNull(view.Temporal);
            Assert.Equal(new DateOnly(2023, 8, 16), view.Temporal.StartDate);
            Assert.Equal(new DateOnly(2023, 9, 10), view.Temporal.EndDate);
            Assert.NotNull(view.ContactPoint);
            Assert.Equal("nameSk", view.ContactPoint.Name);
            Assert.Equal("test@example.com", view.ContactPoint.Email);
            Assert.Equal(new Uri("http://example.com/documentation"), view.Documentation);
            Assert.Equal(new Uri("http://example.com/specification"), view.Specification);
            Assert.Equal(new[] { new Uri("http://publications.europa.eu/resource/dataset/eurovoc/1"), new Uri("http://publications.europa.eu/resource/dataset/eurovoc/2") }, view.EuroVocThemes);
            Assert.Equal(10, view.SpatialResolutionInMeters);
            Assert.Equal("P2D", view.TemporalResolution);
            Assert.Equal(new Uri("http://example.com/test-dataset"), view.IsPartOf);

            Assert.NotNull(view.Themes);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/data-theme/1", "theme1sk"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/data-theme/2", "theme2sk")
            }, view.ThemeValues);

            Assert.NotNull(view.Spatial);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/country/1", "country1sk"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/country/2", "country2sk")
            }, view.SpatialValues);

            Assert.Equal(new CodelistItemView("https://data.gov.sk/set/codelist/dataset-type/1", "type1sk"), view.TypeValue);

            Assert.Equal(new CodelistItemView("http://publications.europa.eu/resource/dataset/frequency/1", "frequency1sk"), view.AccrualPeriodicityValue);

            Assert.NotNull(view.EuroVocThemeValues);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/eurovoc/1", "eurovoc1sk"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/eurovoc/2", "eurovoc2sk")
            }, view.EuroVocThemeValues);

            Assert.Equivalent(new PublisherView
            {
                Name = "Ministerstvo hospodárstva SR",
                Id = publisherId,
                DatasetCount = 0,
                Key = "https://data.gov.sk/id/publisher/full",
                Themes = null
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
            Assert.Equal(new Uri("http://data.gov.sk/download"), distribution.DownloadUrl);
            Assert.Equal(new Uri("http://data.gov.sk/"), distribution.AccessUrl);
            Assert.Equal(new Uri("http://publications.europa.eu/resource/dataset/file-type/1"), distribution.Format);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/text/csv"), distribution.MediaType);
            Assert.Equal(new Uri("http://data.gov.sk/specification"), distribution.ConformsTo);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/application/zip"), distribution.CompressFormat);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/application/zip"), distribution.PackageFormat);
            Assert.Equal(new Uri("http://data.gov.sk/"), distribution.AccessUrl);

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
            Assert.Equal(new Uri("https://data.gov.sk/set/codelist/dataset-type/1"), view.Type);
            Assert.Equal(new[] { new Uri("http://publications.europa.eu/resource/dataset/country/1"), new Uri("http://publications.europa.eu/resource/dataset/country/2") }, view.Spatial);
            Assert.NotNull(view.Temporal);
            Assert.Equal(new DateOnly(2023, 8, 16), view.Temporal.StartDate);
            Assert.Equal(new DateOnly(2023, 9, 10), view.Temporal.EndDate);
            Assert.NotNull(view.ContactPoint);
            Assert.Equal("nameEn", view.ContactPoint.Name);
            Assert.Equal("test@example.com", view.ContactPoint.Email);
            Assert.Equal(new Uri("http://example.com/documentation"), view.Documentation);
            Assert.Equal(new Uri("http://example.com/specification"), view.Specification);
            Assert.Equal(new[] { new Uri("http://publications.europa.eu/resource/dataset/eurovoc/1"), new Uri("http://publications.europa.eu/resource/dataset/eurovoc/2") }, view.EuroVocThemes);
            Assert.Equal(10, view.SpatialResolutionInMeters);
            Assert.Equal("P2D", view.TemporalResolution);
            Assert.Equal(new Uri("http://example.com/test-dataset"), view.IsPartOf);

            Assert.NotNull(view.Themes);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/data-theme/1", "theme1en"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/data-theme/2", "theme2en")
            }, view.ThemeValues);

            Assert.NotNull(view.Spatial);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/country/1", "country1en"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/country/2", "country2en")
            }, view.SpatialValues);

            Assert.Equal(new CodelistItemView("https://data.gov.sk/set/codelist/dataset-type/1", "type1en"), view.TypeValue);

            Assert.Equal(new CodelistItemView("http://publications.europa.eu/resource/dataset/frequency/1", "frequency1en"), view.AccrualPeriodicityValue);

            Assert.NotNull(view.EuroVocThemeValues);
            Assert.Equal(new[] {
                new CodelistItemView("http://publications.europa.eu/resource/dataset/eurovoc/1", "eurovoc1en"),
                new CodelistItemView("http://publications.europa.eu/resource/dataset/eurovoc/2", "eurovoc2en")
            }, view.EuroVocThemeValues);

            Assert.Equivalent(new PublisherView
            {
                Name = "Ministry of economy",
                Id = publisherId,
                DatasetCount = 0,
                Key = "https://data.gov.sk/id/publisher/full",
                Themes = null
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
            Assert.Equal(new Uri("http://data.gov.sk/download"), distribution.DownloadUrl);
            Assert.Equal(new Uri("http://data.gov.sk/"), distribution.AccessUrl);
            Assert.Equal(new Uri("http://publications.europa.eu/resource/dataset/file-type/1"), distribution.Format);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/text/csv"), distribution.MediaType);
            Assert.Equal(new Uri("http://data.gov.sk/specification"), distribution.ConformsTo);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/application/zip"), distribution.CompressFormat);
            Assert.Equal(new Uri("http://www.iana.org/assignments/media-types/application/zip"), distribution.PackageFormat);
            Assert.Equal(new Uri("http://data.gov.sk/"), distribution.AccessUrl);

            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/authorsWorkType/1", "work1en"), distribution.TermsOfUse.AuthorsWorkTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/originalDatabaseType/1", "type1en"), distribution.TermsOfUse.OriginalDatabaseTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1", "rights1en"), distribution.TermsOfUse.DatabaseProtectedBySpecialRightsTypeValue);
            Assert.Equal(new CodelistItemView("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1", "personal1en"), distribution.TermsOfUse.PersonalDataContainmentTypeValue);
            Assert.Equal(new CodelistItemView("http://publications.europa.eu/resource/dataset/file-type/1", "fileType1en"), distribution.FormatValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/text/csv", "CSV"), distribution.MediaTypeValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/application/zip", "ZIP"), distribution.CompressFormatValue);
            Assert.Equal(new CodelistItemView("http://www.iana.org/assignments/media-types/application/zip", "ZIP"), distribution.PackageFormatValue);
        }
    }
}
