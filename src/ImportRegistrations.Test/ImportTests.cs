using AngleSharp.Attributes;
using Lucene.Net.Search.Similarities;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System.Data;
using System.Security.Policy;
using TestBase;
using VDS.RDF.Query.Algebra;
using static Lucene.Net.Documents.Field;
using static Lucene.Net.Util.Packed.PackedInt32s;

namespace ImportRegistrations.Test
{
    public class ImportTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public ImportTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        private (DcatDataset, DcatDistribution) CreateDatasetAndDistribution(string publisherId)
        {
            DcatDataset dataset = DcatDataset.Create();

            dataset.SetTitle(new Dictionary<string, string> { { "sk", "titleSk" }, { "en", "titleEn" } });
            dataset.SetDescription(new Dictionary<string, string> { { "sk", "descriptionSk" }, { "en", "descriptionEn" } });
            dataset.Publisher = new Uri(publisherId);
            dataset.Themes = new[] {
                new Uri("http://publications.europa.eu/resource/dataset/data-theme/1"),
                new Uri("http://publications.europa.eu/resource/dataset/data-theme/2"),
                new Uri(DcatDataset.EuroVocPrefix + "6409"),
                new Uri(DcatDataset.EuroVocPrefix +"6410")};
            dataset.AccrualPeriodicity = new Uri("http://publications.europa.eu/resource/dataset/frequency/1");
            dataset.SetKeywords(new Dictionary<string, List<string>> { { "sk", new List<string> { "keyword1Sk", "keyword2Sk" } }, { "en", new List<string> { "keyword1En", "keyword2En" } } });
            dataset.Type = new[] { new Uri("https://data.gov.sk/set/codelist/dataset-type/1") };
            dataset.Spatial = new[] { new Uri("http://publications.europa.eu/resource/dataset/country/1"), new Uri("http://publications.europa.eu/resource/dataset/country/2") };
            dataset.SetTemporal(new DateOnly(2023, 8, 16), new DateOnly(2023, 9, 10));
            dataset.SetContactPoint(new LanguageDependedTexts { { "sk", "nameSk" }, { "en", "nameEn" } }, "test@example.com");
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

            DcatDistribution distribution = DcatDistribution.Create(Guid.NewGuid());

            distribution.SetTermsOfUse(new Uri("https://data.gov.sk/def/ontology/law/authorsWorkType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/originalDatabaseType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1"));
            distribution.DownloadUrl = new Uri("http://data.gov.sk/download");
            distribution.AccessUrl = new Uri("http://data.gov.sk/download");
            distribution.Format = new Uri("http://publications.europa.eu/resource/dataset/file-type/1");
            distribution.MediaType = new Uri("http://www.iana.org/assignments/media-types/text/csv");
            distribution.ConformsTo = new Uri("http://data.gov.sk/specification");
            distribution.CompressFormat = new Uri("http://www.iana.org/assignments/media-types/application/zip");
            distribution.PackageFormat = new Uri("http://www.iana.org/assignments/media-types/application/zip");
            distribution.AccessService = new Uri("http://example.com/access-service");
            distribution.SetTitle(new LanguageDependedTexts { { "sk", "Test SK" }, { "en", "Test EN" } });

            return (dataset, distribution);
        }

        private void AssertExpectedState(Storage storage, string publisherId, Guid catalogId, DcatDataset dataset, DcatDistribution? distribution)
        {
            FileStorageResponse importedDatasets = storage.GetFileStates(new FileStorageQuery
            {
                AdditionalFilters = new Dictionary<string, string[]>
                {
                    { "localCatalog", new[]{ catalogId.ToString() } },
                    { "key", new[]{ dataset.Uri.ToString() } }
                },
                OnlyTypes = new List<FileType> { FileType.DatasetRegistration }
            }, new AllAccessFilePolicy());

            Assert.Single(importedDatasets.Files);
            Assert.Equal(1, importedDatasets.TotalCount);
            FileState datasetState = importedDatasets.Files[0];

            FileMetadata datasetMetadata = datasetState.Metadata;
            if (dataset.IsPartOfInternalId is not null)
            {
                Assert.Equal(Guid.Parse(dataset.IsPartOfInternalId), datasetMetadata.ParentFile);
            }
            else
            {
                Assert.Null(datasetMetadata.ParentFile);
            }
            Assert.Equal(new[] { "true" }, datasetMetadata.AdditionalValues?["Harvested"]);
            Assert.Equal(publisherId, datasetMetadata.Publisher);
            Assert.True(datasetMetadata.IsPublic);
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/1" }, datasetMetadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist));
            Assert.NotNull(datasetState.Content);
            DcatDataset importedDataset = DcatDataset.Parse(datasetState.Content)!;
            Assert.True(dataset.IsEqualTo(importedDataset));

            FileStorageResponse importedDistributions = storage.GetFileStates(new FileStorageQuery { ParentFile = datasetMetadata.Id, OnlyTypes = new List<FileType> { FileType.DistributionRegistration } }, new AllAccessFilePolicy());
            if (distribution is not null)
            {
                Assert.Single(importedDistributions.Files);
                Assert.Equal(1, importedDistributions.TotalCount);
                FileState distributionState = importedDistributions.Files[0];

                FileMetadata distributionMetadata = distributionState.Metadata;
                Assert.Equal(new[] { "true" }, distributionMetadata.AdditionalValues?["Harvested"]);
                Assert.Equal(publisherId, distributionMetadata.Publisher);
                Assert.True(distributionMetadata.IsPublic);
                Assert.NotNull(distributionState.Content);
                DcatDistribution importedDistribution = DcatDistribution.Parse(distributionState.Content)!;
                Assert.True(distribution.IsEqualTo(importedDistribution));
            }
            else 
            {
                Assert.Empty(importedDistributions.Files);
                Assert.Equal(0, importedDistributions.TotalCount);
            }
        }

        [Fact]
        public async Task TestImportNewCatalog()
        {
            string path = fixture.GetStoragePath();

            string publisherId = "http://data.gob.sk/test";
            (Uri catalogUri, Guid catalogId) = fixture.CreateLocalCatalog("Test", publisherId);
            using Storage storage = new Storage(path);
            TestSparqlClient sparqlClient = new TestSparqlClient();

            (DcatDataset dataset, DcatDistribution distribution) = CreateDatasetAndDistribution(publisherId);
            sparqlClient.Add(catalogUri, dataset);
            sparqlClient.Add(dataset.Uri, distribution);

            HttpContextValueAccessor httpContextValueAccessor = new HttpContextValueAccessor();

            HarvestedDataImport import = new HarvestedDataImport(
                sparqlClient,
                new TestDocumentStorageClient(storage, new DefaultFileAccessPolicy(httpContextValueAccessor)),
                p =>
                {
                    httpContextValueAccessor.Publisher = p;
                    return Task.CompletedTask;
                },
                s => { });;

            await import.Import();

            AssertExpectedState(storage, publisherId, catalogId, dataset, distribution);
        }

        [Fact]
        public async Task TestImportExistingCatalogWithoutChange()
        {
            string path = fixture.GetStoragePath();

            string publisherId = "http://data.gob.sk/test";
            (Uri catalogUri, Guid catalogId) = fixture.CreateLocalCatalog("Test", publisherId);
            using Storage storage = new Storage(path);
            TestSparqlClient sparqlClient = new TestSparqlClient();

            (DcatDataset dataset, DcatDistribution distribution) = CreateDatasetAndDistribution(publisherId);
            sparqlClient.Add(catalogUri, dataset);
            sparqlClient.Add(dataset.Uri, distribution);

            HttpContextValueAccessor httpContextValueAccessor = new HttpContextValueAccessor();

            HarvestedDataImport import = new HarvestedDataImport(
                sparqlClient,
                new TestDocumentStorageClient(storage, new DefaultFileAccessPolicy(httpContextValueAccessor)),
                p =>
                {
                    httpContextValueAccessor.Publisher = p;
                    return Task.CompletedTask;
                },
                s => { });

            await import.Import();
            await import.Import();

            AssertExpectedState(storage, publisherId, catalogId, dataset, distribution);
        }

        [Fact]
        public async Task TestImportExistingCatalogWithChange()
        {
            string path = fixture.GetStoragePath();

            string publisherId = "http://data.gob.sk/test";
            (Uri catalogUri, Guid catalogId) = fixture.CreateLocalCatalog("Test", publisherId);
            using Storage storage = new Storage(path);
            TestSparqlClient sparqlClient = new TestSparqlClient();

            (DcatDataset dataset, DcatDistribution distribution) = CreateDatasetAndDistribution(publisherId);
            sparqlClient.Add(catalogUri, dataset);
            sparqlClient.Add(dataset.Uri, distribution);

            HttpContextValueAccessor httpContextValueAccessor = new HttpContextValueAccessor();

            HarvestedDataImport import = new HarvestedDataImport(
                sparqlClient,
                new TestDocumentStorageClient(storage, new DefaultFileAccessPolicy(httpContextValueAccessor)),
                p =>
                {
                    httpContextValueAccessor.Publisher = p;
                    return Task.CompletedTask;
                },
                s => { });

            await import.Import();

            dataset.SetTitle(new Dictionary<string, string> { { "sk", "titleSk2" }, { "en", "titleEn2" } });
            distribution.DownloadUrl = new Uri("http://data.gov.sk/download2");

            await import.Import();

            AssertExpectedState(storage, publisherId, catalogId, dataset, distribution);
        }

        [Fact]
        public async Task TestImportExistingCatalogEmpty()
        {
            string path = fixture.GetStoragePath();

            string publisherId = "http://data.gob.sk/test";
            (Uri catalogUri, Guid catalogId) = fixture.CreateLocalCatalog("Test", publisherId);
            using Storage storage = new Storage(path);
            TestSparqlClient sparqlClient = new TestSparqlClient();

            (DcatDataset dataset, DcatDistribution distribution) = CreateDatasetAndDistribution(publisherId);
            sparqlClient.Add(catalogUri, dataset);
            sparqlClient.Add(dataset.Uri, distribution);

            HttpContextValueAccessor httpContextValueAccessor = new HttpContextValueAccessor();

            HarvestedDataImport import = new HarvestedDataImport(
                sparqlClient,
                new TestDocumentStorageClient(storage, new DefaultFileAccessPolicy(httpContextValueAccessor)),
                p =>
                {
                    httpContextValueAccessor.Publisher = p;
                    return Task.CompletedTask;
                },
                s => { });

            await import.Import();

            sparqlClient.Clear();

            await import.Import();

            FileStorageResponse importedDatasets = storage.GetFileStates(new FileStorageQuery { ParentFile = catalogId, OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, new AllAccessFilePolicy());
            Assert.Empty(importedDatasets.Files);
            Assert.Equal(0, importedDatasets.TotalCount);
        }

        [Fact]
        public async Task TestImportSerieDataset()
        {
            string path = fixture.GetStoragePath();

            string publisherId = "http://data.gob.sk/test";
            (Uri catalogUri, Guid catalogId) = fixture.CreateLocalCatalog("Test", publisherId);
            using Storage storage = new Storage(path);
            TestSparqlClient sparqlClient = new TestSparqlClient();

            (DcatDataset datasetSerie, _) = CreateDatasetAndDistribution(publisherId);

            sparqlClient.Add(catalogUri, datasetSerie);

            (DcatDataset datasetPart, DcatDistribution distribution) = CreateDatasetAndDistribution(publisherId);

            datasetPart.IsPartOf = datasetSerie.Uri;

            sparqlClient.Add(catalogUri, datasetPart);
            sparqlClient.Add(catalogUri, distribution);

            HttpContextValueAccessor httpContextValueAccessor = new HttpContextValueAccessor();

            HarvestedDataImport import = new HarvestedDataImport(
                sparqlClient,
                new TestDocumentStorageClient(storage, new DefaultFileAccessPolicy(httpContextValueAccessor)),
                p =>
                {
                    httpContextValueAccessor.Publisher = p;
                    return Task.CompletedTask;
                },
                s => { });

            await import.Import();

            datasetSerie.IsSerie = true;

            FileStorageResponse importedDatasets = storage.GetFileStates(new FileStorageQuery
            {
                AdditionalFilters = new Dictionary<string, string[]>
                {
                    { "localCatalog", new[]{ catalogId.ToString() } },
                    { "key", new[]{ datasetSerie.Uri.ToString() } }
                },
                OnlyTypes = new List<FileType> { FileType.DatasetRegistration }
            }, new AllAccessFilePolicy());

            datasetPart.IsPartOfInternalId = importedDatasets.Files[0].Metadata.Id.ToString();

            AssertExpectedState(storage, publisherId, catalogId, datasetSerie, null);
            AssertExpectedState(storage, publisherId, catalogId, datasetPart, distribution);
        }

        private class HttpContextValueAccessor : IHttpContextValueAccessor
        {
            public string? Publisher { get; set; }

            public string? Token => "-";

            public string? UserId => null;

            public bool HasRole(string role)
            {
                return string.Equals(role, "Harvester", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}