using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TestBase;
using static System.Net.Mime.MediaTypeNames;

namespace Frontend.Test
{
    [TestClass]
    public class DatasetSearchTests : PageTest
    {
        private StorageFixture fixture = new StorageFixture();

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublicFileAccessPolicy();

        private Guid CreateDatasetAndDistribution(StorageFixture fixture, string name, bool isPublic = true, int index = 1, bool createDataService = false, string? publisher = null)
        {
            publisher ??= PublisherId;

            DcatDataset dataset = DcatDataset.Create();
            dataset.SetTitle(new Dictionary<string, string> { { "sk", name } });
            dataset.SetDescription(new Dictionary<string, string> { { "sk", "Test Description" } });
            dataset.SetKeywords(new Dictionary<string, List<string>> { { "sk", new List<string> { "Test1", "Test2" } } });
            dataset.Type = new[] { new Uri("https://data.gov.sk/set/codelist/dataset-type/1") };
            dataset.Themes = new[] { 
                new Uri("http://publications.europa.eu/resource/dataset/data-theme/1"), 
                new Uri("http://publications.europa.eu/resource/dataset/data-theme/2"), 
                new Uri(DcatDataset.EuroVocPrefix + "6409"), 
                new Uri(DcatDataset.EuroVocPrefix + "6410") };
            dataset.LandingPage = new Uri("http://example.com/home");
            dataset.Specification = new Uri("http://example.com/specification");
            dataset.Documentation = new Uri("http://example.com/documentation");
            dataset.Relation = new Uri("http://example.com/relation");
            dataset.AccrualPeriodicity = new Uri("http://publications.europa.eu/resource/dataset/frequency/1");
            dataset.SetContactPoint(new LanguageDependedTexts { { "sk", "Test Contact Point" } }, "test@example.com");
            dataset.Spatial = new[] { new Uri("http://publications.europa.eu/resource/dataset/country/1"), new Uri("http://publications.europa.eu/resource/dataset/country/2") };
            dataset.SetTemporal(new DateOnly(2023, 8, 17), new DateOnly(2023, 9, 12));
            dataset.TemporalResolution = "1D";
            dataset.SpatialResolutionInMeters = 100;
            dataset.Publisher = new Uri(publisher);
            dataset.ShouldBePublic = isPublic;
            dataset.SetEuroVocLabelThemes(new Dictionary<string, List<string>> { { "sk", new List<string> { "nepovolená likvidácia odpadu", "chemický odpad" } } });
            dataset.ApplicableLegislations = new List<Uri>
            {
                new Uri("https://data.gov.sk/id/eli/sk/zz/2019/95"),
                new Uri("https://data.gov.sk/id/eli/sk/zz/2007/39/20220101"),
            };
            dataset.HvdCategory = new Uri("http://publications.europa.eu/resource/dataset/high-value-dataset-category/1");

            Guid datasetId = fixture.CreateDataset(dataset);

            DcatDistribution distribution = DcatDistribution.Create(datasetId);
            distribution.SetTermsOfUse(
                new Uri("http://publications.europa.eu/resource/authority/licence/CC_BY_4_0"),
                new Uri("http://publications.europa.eu/resource/authority/licence/CC_BY_4_0"),
                new Uri("http://publications.europa.eu/resource/authority/licence/CC_BY_4_0"),
                new Uri("https://data.gov.sk/def/personal-data-occurence-type/2"),
                "Author",
                "Author Original");
            distribution.Format = new Uri("http://publications.europa.eu/resource/authority/file-type/CSV");
            distribution.MediaType = new Uri("http://www.iana.org/assignments/media-types/text/csv");
            distribution.ConformsTo = new Uri("http://example.com/conforms");
            distribution.CompressFormat = new Uri("http://www.iana.org/assignments/media-types/application/zip");
            distribution.PackageFormat = new Uri("http://www.iana.org/assignments/media-types/application/zip");
            distribution.ApplicableLegislations = new List<Uri>
            {
                new Uri("https://data.gov.sk/id/eli/sk/zz/2019/95"),
                new Uri("https://data.gov.sk/id/eli/sk/zz/2007/39/20220101"),
            };

            if (createDataService)
            {
                DcatDataService dataService = distribution.GetOrCreateDataSerice();
                distribution.SetTitle(new Dictionary<string, string> { { "sk", "Test Service SK" } });
                dataService.EndpointUrl = new Uri("http://example.com/endpoint");
                dataService.Documentation = new Uri("http://example.com/new/specification");
                dataService.ConformsTo = distribution.ConformsTo;
                dataService.SetTitle(distribution.Title);
                dataService.ApplicableLegislations = distribution.ApplicableLegislations;
                dataService.EndpointDescription = new Uri("http://example.com/endpoint-description");
                dataService.HvdCategory = new Uri("http://publications.europa.eu/resource/dataset/high-value-dataset-category/1");
                distribution.AccessUrl = dataService.EndpointUrl;
                dataService.SetContactPoint(new LanguageDependedTexts { { "sk", "Test Kontakt Service" } }, "example@example.com");
            }
            else
            {
                distribution.DownloadUrl = new Uri("http://example.com/distribution");
                distribution.AccessUrl = distribution.DownloadUrl;
            }
            
            fixture.CreateDistribution(datasetId, publisher, distribution);

            return datasetId;
        }

        private void UpdateDataset(Guid id, Storage storage, Action<DcatDataset> action)
        {
            FileState? state = storage.GetFileState(id, accessPolicy);
            Assert.IsNotNull(state);
            DcatDataset dataset = DcatDataset.Parse(state.Content!)!;
            action(dataset);
            storage.InsertFile(dataset.ToString(), dataset.UpdateMetadata(state.Metadata.IsPublic, null, state.Metadata), true, new AllAccessFilePolicy());
        }

        [TestMethod]
        public async Task ResultsShouldBeEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            CreateDatasetAndDistribution(fixture, "Test", false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetSearch();

            Assert.AreEqual("0 výsledkov", await Page.GetSearchResultsCount());
            Assert.AreEqual(0, await Page.GetByTestId("sr-result").CountAsync());
        }

        private async Task AssertResult(IElementHandle result, Guid id, string name, string publisherName)
        {
            IElementHandle? link = await result.QuerySelectorAsync("a");
            Assert.IsNotNull(link);
            Assert.AreEqual(name, await link.TextContentAsync());
            Assert.AreEqual($"/datasety/{id}", await link.GetAttributeAsync("href"));

            IElementHandle? publisherNameElement = await result.QuerySelectorAsync("[data-testid='sr-result-publisher']");
            Assert.IsNotNull(publisherNameElement);
            Assert.AreEqual(publisherName, await publisherNameElement.TextContentAsync());
        }

        [TestMethod]
        public async Task OneResultsShouldBeVisible()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test", true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetSearch();

            Assert.AreEqual("1 výsledok", await Page.GetSearchResultsCount());
            IReadOnlyList<IElementHandle> results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
            Assert.AreEqual(1, results.Count);

            await AssertResult(results[0], id, "Test", "Test Publisher");
        }

        [TestMethod]
        public async Task ResultShouldRespectQuery()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test", true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetSearch();

            async Task NoItems()
            {
                Assert.AreEqual("0 výsledkov", await Page.GetSearchResultsCount());
                IReadOnlyList<IElementHandle> results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
                Assert.AreEqual(0, results.Count);
            }

            async Task OneItem()
            {
                Assert.AreEqual("1 výsledok", await Page.GetSearchResultsCount());
                IReadOnlyList<IElementHandle> results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
                Assert.AreEqual(1, results.Count);
                IElementHandle result = results[0];

                await AssertResult(results[0], id, "Test", "Test Publisher");
            }

            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                await Page.GetByTestId("sr-query").FillAsync("Test");
            });
            await OneItem();

            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                await Page.GetByTestId("sr-query").FillAsync("Abc");
            });
            await NoItems();

            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                await Page.GetByTestId("sr-query").FillAsync(string.Empty);
            });
            await OneItem();
        }

        [TestMethod]
        public async Task ResultShouldRespectPublisherFilter()
        {
            string path = fixture.GetStoragePath();

            string publisher1 = PublisherId + "1";
            string publisher2 = PublisherId + "2";

            fixture.CreatePublisher(publisher1, true, "Test Publisher 1");
            fixture.CreatePublisher(publisher2, true, "Test Publisher 2");

            Guid d1 = CreateDatasetAndDistribution(fixture, "Test 1", true, publisher: publisher1);
            Guid d2 = CreateDatasetAndDistribution(fixture, "Test 2", true, publisher: publisher2);

            using Storage storage = new Storage(path);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetSearch();

            IElementHandle? filter = await Page.GetTestElement("sr-filter-publishers");
            Assert.IsNotNull(filter);

            IReadOnlyList<IElementHandle> checkBoxes = await filter.QuerySelectorAllAsync("input[type='checkbox']");
            Assert.AreEqual(2, checkBoxes.Count);

            Assert.AreEqual("Test Publisher 1 (1)", await Page.GetLabel(checkBoxes[0]));
            Assert.AreEqual("Test Publisher 2 (1)", await Page.GetLabel(checkBoxes[1]));

            async Task AssertAllResults()
            {
                Assert.AreEqual("2 výsledky", await Page.GetSearchResultsCount());
                IReadOnlyList<IElementHandle> results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
                Assert.AreEqual(2, results.Count);

                await AssertResult(results[1], d1, "Test 1", "Test Publisher 1");
                await AssertResult(results[0], d2, "Test 2", "Test Publisher 2");                
            }

            async Task CheckFilters(bool checkPublisher1, bool checkPublisher2)
            { 
                IElementHandle? filter = await Page.GetTestElement("sr-filter-publishers");
                Assert.IsNotNull(filter);

                IReadOnlyList<IElementHandle> checkBoxes = await filter.QuerySelectorAllAsync("input[type='checkbox']");
                Assert.AreEqual(2, checkBoxes.Count);

                if (checkPublisher1)
                {
                    await checkBoxes[0].CheckAsync();
                }
                else
                {
                    await checkBoxes[0].UncheckAsync();
                }

                if (checkPublisher2)
                {
                    await checkBoxes[1].CheckAsync();
                }
                else
                {
                    await checkBoxes[1].UncheckAsync();
                }
            }

            await AssertAllResults();

            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                await CheckFilters(true, false);
            });
            
            Assert.AreEqual("1 výsledok", await Page.GetSearchResultsCount());
            IReadOnlyList<IElementHandle> results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
            Assert.AreEqual(1, results.Count);
            await AssertResult(results[0], d1, "Test 1", "Test Publisher 1");

            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                await CheckFilters(true, true);
            });

            await AssertAllResults();

            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                await CheckFilters(false, true);
            });

            Assert.AreEqual("1 výsledok", await Page.GetSearchResultsCount());
            results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
            Assert.AreEqual(1, results.Count);
            await AssertResult(results[0], d2, "Test 2", "Test Publisher 2");

            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                await CheckFilters(false, false);
            });

            await AssertAllResults();
        }

        [TestMethod]
        public async Task ChangeInQueryShouldResetPage()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            for (int i = 0; i <= 10; i++)
            {
                CreateDatasetAndDistribution(fixture, $"Test {i}", true);
            }

            using Storage storage = new Storage(path);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetSearch();

            async Task AssertFirstPage()
            {
                StringAssert.Contains("Strana 1 z 2", await (await Page.GetTestElement("sr-current-page"))!.TextContentAsync());
            }

            async Task GoToSecondPage()
            {
                await Page.RunAndWaitForDatasetSearch(async () =>
                {
                    await (await Page.GetTestElement("sr-next-page"))!.ClickAsync();
                });
            }

            await GoToSecondPage();
            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                IElementHandle? filter = await Page.GetTestElement("sr-filter-publishers");
                Assert.IsNotNull(filter);

                IReadOnlyList<IElementHandle> checkBoxes = await filter.QuerySelectorAllAsync("input[type='checkbox']");
                await checkBoxes[0].CheckAsync();
            });
            await AssertFirstPage();

            await GoToSecondPage();
            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                IElementHandle? filter = await Page.GetTestElement("sr-filter-publishers");
                Assert.IsNotNull(filter);

                IReadOnlyList<IElementHandle> checkBoxes = await filter.QuerySelectorAllAsync("input[type='checkbox']");
                await checkBoxes[0].UncheckAsync();
            });
            await AssertFirstPage();
        }

        [TestMethod]
        public async Task LinkShouldOpenDetail()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test", true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetSearch();

            IElementHandle? link = await Page.QuerySelectorAsync("[data-testid='sr-result'] a");
            Assert.IsNotNull(link);
            await link.ClickAsync();

            await Page.WaitForURLAsync($"http://localhost:6001/datasety/{id}");
        }

        [TestMethod]
        public async Task DetailShouldBeDisplayed()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.AreEqual("Národný katalóg otvorených dát - Test dataset", await Page.TitleAsync());
            Assert.AreEqual("Test dataset", await (await Page.QuerySelectorAsync("h1"))!.TextContentAsync());

            await Page.CheckTestContent("publisher-name", "Test Publisher");
            await Page.CheckTestContent("description", "Test Description");
            await Page.CheckTestContent("types", "type1sk");

            IElementHandle? keywords = await Page.GetTestElement("keywords");
            Assert.IsNotNull(keywords);
            List<string> tags = await Extensions.GetTags(keywords);
            CollectionAssert.AreEquivalent(new[] { "Test1", "Test2" }, tags);

            IElementHandle? themes = await Page.GetTestElement("themes");
            Assert.IsNotNull(themes);
            List<string> themeValues = new List<string>();
            foreach (IElementHandle theme in await themes.QuerySelectorAllAsync("div"))
            {
                themeValues.Add((await theme.TextContentAsync())!);
            }
            CollectionAssert.AreEquivalent(new[] { "theme1sk", "theme2sk", "nepovolená likvidácia odpadu", "chemický odpad" }, themeValues);

            await Page.CheckTestContentLink("landing-page", "http://example.com/home");
            await Page.CheckTestContentLink("specification", "http://example.com/specification");
            await Page.CheckTestContentLink("documentation", "http://example.com/documentation");
            await Page.CheckTestContentLink("relation", "http://example.com/relation");

            await Page.CheckTestContent("update-frequency", "frequency1sk");
            await Page.CheckTestContent("hvd-category", "HVD 1 sk");
            await Page.CheckTestContent("applicable-legislations", new List<string> {
                "https://data.gov.sk/id/eli/sk/zz/2019/95",
                "https://data.gov.sk/id/eli/sk/zz/2007/39/20220101"
            });
            await Page.CheckTestContent("contact-name", "Test Contact Point");
            await Page.CheckTestContent("contact-email", "test@example.com");

            IElementHandle? spatial = await Page.GetTestElement("spatial");
            Assert.IsNotNull(spatial);
            List<string> spatialValues = new List<string>();
            foreach (IElementHandle s in await spatial.QuerySelectorAllAsync("div"))
            {
                spatialValues.Add((await s.TextContentAsync())!);
            }
            CollectionAssert.AreEquivalent(new[] { "country1sk", "country2sk" }, spatialValues);

            await Page.CheckTestContent("spatial-resolution", "100");

            await Page.CheckTestContent("temporal-start", "17. 8. 2023");
            await Page.CheckTestContent("temporal-end", "12. 9. 2023");
            await Page.CheckTestContent("temporal-resolution", "1D");

            await Page.CheckTestContent("distributions-count", "1 distribúcia");

            IReadOnlyList<IElementHandle> distributions = await Page.GetTestElements("distribution");
            Assert.AreEqual(1, distributions.Count);

            IElementHandle distribution = distributions[0];
            IElementHandle? distributionLink = await distribution.QuerySelectorAsync("a");
            Assert.IsNotNull(distributionLink);
            Assert.AreEqual("https://example.com/distribution", await distributionLink.GetAttributeAsync("href"));
            Assert.AreEqual("Test dataset", await distributionLink.TextContentAsync());

            await AssertTextPropertyLink(distribution, "Typ autorského diela", "CC sk", "http://publications.europa.eu/resource/authority/licence/CC_BY_4_0");
            await AssertTextPropertyLink(distribution, "Typ originálnej databázy", "CC sk", "http://publications.europa.eu/resource/authority/licence/CC_BY_4_0");
            await AssertTextPropertyLink(distribution, "Typ špeciálnej právnej ochrany databázy", "CC sk", "http://publications.europa.eu/resource/authority/licence/CC_BY_4_0");
            await AssertTextPropertyLink(distribution, "Typ výskytu osobných údajov", "personal2sk", "https://data.gov.sk/def/personal-data-occurence-type/2");
            await AssertTextProperty(distribution, $"Meno autora diela: Author");
            await AssertTextProperty(distribution, $"Meno autora originálnej databázy: Author Original");
            await AssertTextPropertyLink(distribution, "Formát", "csv", "http://publications.europa.eu/resource/authority/file-type/CSV");
            await AssertTextPropertyLink(distribution, "Typ média", "CSV", "http://www.iana.org/assignments/media-types/text/csv");
            await AssertTextProperty(distribution, $"Právny predpis: https://data.gov.sk/id/eli/sk/zz/2019/95 https://data.gov.sk/id/eli/sk/zz/2007/39/20220101");
            await AssertTextPropertyLink(distribution, "Odkaz na strojovo-čitateľnú schému súboru na stiahnutie", "http://example.com/conforms", "http://example.com/conforms", false);
            await AssertTextPropertyLink(distribution, "Typ média kompresného formátu", "ZIP", "http://www.iana.org/assignments/media-types/application/zip");
            await AssertTextPropertyLink(distribution, "Typ média balíčkovacieho formátu", "ZIP", "http://www.iana.org/assignments/media-types/application/zip");

            Assert.IsNull(await Page.GetTestElement("related"));
        }

        private async Task AssertTextProperty(IElementHandle element, string value)
        {
            foreach (IElementHandle e in await element.QuerySelectorAllAsync("p"))
            {
                string? text = (await e.TextContentAsync())?.Trim();
                if (text == value)
                {
                    return;
                }
            }
            Assert.Fail($"'{value}' was not found in '{await element.TextContentAsync()}'");
        }

        private async Task AssertTextPropertyLink(IElementHandle element, string header, string value, string link, bool useDereference = true)
        {
            if (useDereference)
            {
                link = $"https://znalosti.gov.sk/resource?uri={WebUtility.UrlEncode(link)}";
            }
            foreach (IElementHandle e in await element.QuerySelectorAllAsync("p"))
            {
                string? text = (await e.TextContentAsync())?.Trim();
                if (text != null && text.StartsWith(header))
                {
                    IElementHandle? l = await e.QuerySelectorAsync("a");
                    Assert.IsNotNull(l);
                    Assert.AreEqual(link, await l.GetAttributeAsync("href"));
                    Assert.AreEqual(value, await l.TextContentAsync());
                    return;
                }
            }
            Assert.Fail($"'{header}' was not found in '{await element.TextContentAsync()}'");
        }

        [TestMethod]
        public async Task DetailShouldBeDisplayedWithDataService()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true, createDataService: true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.AreEqual("Národný katalóg otvorených dát - Test dataset", await Page.TitleAsync());
            Assert.AreEqual("Test dataset", await (await Page.QuerySelectorAsync("h1"))!.TextContentAsync());

            await Page.CheckTestContent("publisher-name", "Test Publisher");
            await Page.CheckTestContent("description", "Test Description");
            await Page.CheckTestContent("types", "type1sk");

            IElementHandle? keywords = await Page.GetTestElement("keywords");
            Assert.IsNotNull(keywords);
            List<string> tags = await Extensions.GetTags(keywords);
            CollectionAssert.AreEquivalent(new[] { "Test1", "Test2" }, tags);

            IElementHandle? themes = await Page.GetTestElement("themes");
            Assert.IsNotNull(themes);
            List<string> themeValues = new List<string>();
            foreach (IElementHandle theme in await themes.QuerySelectorAllAsync("div"))
            {
                themeValues.Add((await theme.TextContentAsync())!);
            }
            CollectionAssert.AreEquivalent(new[] { "theme1sk", "theme2sk", "nepovolená likvidácia odpadu", "chemický odpad" }, themeValues);

            await Page.CheckTestContentLink("landing-page", "http://example.com/home");
            await Page.CheckTestContentLink("specification", "http://example.com/specification");
            await Page.CheckTestContentLink("documentation", "http://example.com/documentation");
            await Page.CheckTestContentLink("relation", "http://example.com/relation");

            await Page.CheckTestContent("update-frequency", "frequency1sk");
            await Page.CheckTestContent("hvd-category", "HVD 1 sk");
            await Page.CheckTestContent("applicable-legislations", new List<string> {
                "https://data.gov.sk/id/eli/sk/zz/2019/95",
                "https://data.gov.sk/id/eli/sk/zz/2007/39/20220101"
            });
            await Page.CheckTestContent("contact-name", "Test Contact Point");
            await Page.CheckTestContent("contact-email", "test@example.com");

            IElementHandle? spatial = await Page.GetTestElement("spatial");
            Assert.IsNotNull(spatial);
            List<string> spatialValues = new List<string>();
            foreach (IElementHandle s in await spatial.QuerySelectorAllAsync("div"))
            {
                spatialValues.Add((await s.TextContentAsync())!);
            }
            CollectionAssert.AreEquivalent(new[] { "country1sk", "country2sk" }, spatialValues);

            await Page.CheckTestContent("spatial-resolution", "100");

            await Page.CheckTestContent("temporal-start", "17. 8. 2023");
            await Page.CheckTestContent("temporal-end", "12. 9. 2023");
            await Page.CheckTestContent("temporal-resolution", "1D");

            await Page.CheckTestContent("distributions-count", "1 distribúcia");

            IReadOnlyList<IElementHandle> distributions = await Page.GetTestElements("distribution");
            Assert.AreEqual(1, distributions.Count);

            IElementHandle distribution = distributions[0];
            IElementHandle? distributionLink = await distribution.QuerySelectorAsync("[data-testid='service-name']");
            Assert.IsNotNull(distributionLink);
            Assert.AreEqual("Test Service SK (Dátová služba)", await distributionLink.TextContentAsync());

            await AssertTextPropertyLink(distribution, "Typ autorského diela", "CC sk", "http://publications.europa.eu/resource/authority/licence/CC_BY_4_0");
            await AssertTextPropertyLink(distribution, "Typ originálnej databázy", "CC sk", "http://publications.europa.eu/resource/authority/licence/CC_BY_4_0");
            await AssertTextPropertyLink(distribution, "Typ špeciálnej právnej ochrany databázy", "CC sk", "http://publications.europa.eu/resource/authority/licence/CC_BY_4_0");
            await AssertTextPropertyLink(distribution, "Typ výskytu osobných údajov", "personal2sk", "https://data.gov.sk/def/personal-data-occurence-type/2");
            await AssertTextProperty(distribution, $"Meno autora diela: Author");
            await AssertTextProperty(distribution, $"Meno autora originálnej databázy: Author Original");
            await AssertTextPropertyLink(distribution, "Formát", "csv", "http://publications.europa.eu/resource/authority/file-type/CSV");
            await AssertTextPropertyLink(distribution, "Typ média", "CSV", "http://www.iana.org/assignments/media-types/text/csv");
            await AssertTextProperty(distribution, $"Právny predpis: https://data.gov.sk/id/eli/sk/zz/2019/95 https://data.gov.sk/id/eli/sk/zz/2007/39/20220101");
            await AssertTextPropertyLink(distribution, "Odkaz na strojovo-čitateľnú schému súboru na stiahnutie", "http://example.com/conforms", "http://example.com/conforms", false);
            await AssertTextPropertyLink(distribution, "Typ média kompresného formátu", "ZIP", "http://www.iana.org/assignments/media-types/application/zip");
            await AssertTextPropertyLink(distribution, "Typ média balíčkovacieho formátu", "ZIP", "http://www.iana.org/assignments/media-types/application/zip");
            await AssertTextPropertyLink(distribution, "Kategória HVD", "HVD 1 sk", "http://publications.europa.eu/resource/dataset/high-value-dataset-category/1");
            await AssertTextPropertyLink(distribution, "Prístupový bod", "http://example.com/endpoint", "http://example.com/endpoint", false);
            await AssertTextProperty(distribution, $"Kontaktný bod: Test Kontakt Service example@example.com");
            await AssertTextPropertyLink(distribution, "Odkaz na dokumentáciu", "http://example.com/new/specification", "http://example.com/new/specification", false);
            await AssertTextPropertyLink(distribution, "Popis prístupového bodu", "http://example.com/endpoint-description", "http://example.com/endpoint-description", false);

            Assert.IsNull(await Page.GetTestElement("related"));
        }

        [TestMethod]
        public async Task DescriptionIsNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.SetDescription(new Dictionary<string, string>()));

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("description"));
        }

        [TestMethod]
        public async Task TypesAreNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.Type = Array.Empty<Uri>());

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("types"));
        }

        [TestMethod]
        public async Task KeywordsAreNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.SetKeywords(new Dictionary<string, List<string>>()));

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("keywords"));
        }

        [TestMethod]
        public async Task ThemesAreNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d =>
            {
                d.Themes = Array.Empty<Uri>();
                d.SetEuroVocLabelThemes(new Dictionary<string, List<string>>());
            });

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("themes"));
        }

        [TestMethod]
        public async Task DocumenationIsNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.LandingPage = null);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("landing-page"));
        }

        [TestMethod]
        public async Task SpecificationIsNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.Specification = null);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("specification"));
        }

        [TestMethod]
        public async Task UpdateFrequencyIsNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.AccrualPeriodicity = null);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("update-frequency"));
        }

        [TestMethod]
        public async Task ContactNameIsNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.SetContactPoint(new LanguageDependedTexts(), d.ContactPoint?.Email));

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("contact-name"));
        }

        [TestMethod]
        public async Task ContactEmailIsNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.SetContactPoint(new LanguageDependedTexts(new Dictionary<string, string> { { "sk", "Test" } }), null));

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("contact-email"));
        }

        [TestMethod]
        public async Task TemporalStartIsNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.SetTemporal(null, new DateOnly(2023, 9, 1)));

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("temporal-start"));
        }

        [TestMethod]
        public async Task TemporalEndIsNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.SetTemporal(new DateOnly(2023, 9, 1), null));

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("temporal-end"));
        }

        [TestMethod]
        public async Task TemporalResolutionIsNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.TemporalResolution = null);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("temporal-resolution"));
        }

        [TestMethod]
        public async Task SpatialResolutionIsNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.SpatialResolutionInMeters = null);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("spatial-resolution"));
        }

        [TestMethod]
        public async Task SpatialAreNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(id, storage, d => d.Spatial = Array.Empty<Uri>());

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("spatial"));
        }

        [TestMethod]
        public async Task DistributionsAreNotVisibleIfEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            IFileStorageAccessPolicy accessPolicy = new AllAccessFilePolicy();
            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery { ParentFile = id }, accessPolicy);
            foreach (FileState state in response.Files)
            {
                storage.DeleteFile(state.Metadata.Id, accessPolicy);
            }

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(id);

            Assert.IsNull(await Page.GetTestElement("distributions-count"));

            IReadOnlyList<IElementHandle> distributions = await Page.GetTestElements("distribution");
            Assert.AreEqual(0, distributions.Count);
        }

        private async Task AssertRelatedDatasets(Storage storage, params Guid[] keys)
        {
            IElementHandle? related = await Page.GetTestElement("related");
            if (keys.Length > 0)
            {
                Assert.IsNotNull(related);

                IReadOnlyList<IElementHandle> relatedDatasets = await related.QuerySelectorAllAsync("a");
                Assert.AreEqual(keys.Length, relatedDatasets.Count);

                for (int i = 0; i < keys.Length; i++)
                {
                    Guid key = keys[i];
                    IElementHandle relatedDataset = relatedDatasets[i];
                    Assert.AreEqual($"/datasety/{key}", await relatedDataset.GetAttributeAsync("href"));

                    FileState? state = storage.GetFileState(key, accessPolicy);
                    Assert.IsNotNull(state);
                    DcatDataset dataset = DcatDataset.Parse(state.Content!)!;
                    Assert.AreEqual(dataset.GetTitle("sk"), await relatedDataset.TextContentAsync());
                }
            }
            else
            {
                Assert.IsNull(related);
            }
        }

        [TestMethod]
        public async Task RelatedDatasetsShouldBeEmptyByDefault()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);

            Guid parentId = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            using Storage storage = new Storage(path);

            UpdateDataset(parentId, storage, d => d.IsSerie = true);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(parentId);

            await AssertRelatedDatasets(storage);
        }

        [TestMethod]
        public async Task RelatedDatasetsShouldHaveChildrenOnParent()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);

            Guid parentId = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            Guid c1 = CreateDatasetAndDistribution(fixture, "Child 1", true);
            Guid c2 = CreateDatasetAndDistribution(fixture, "Child 2", true);

            using Storage storage = new Storage(path);

            UpdateDataset(parentId, storage, d => d.IsSerie = true);
            UpdateDataset(c1, storage, d =>
            {
                d.IsPartOfInternalId = parentId.ToString();
                d.Modified = new DateTimeOffset(2023, 8, 17, 0, 0, 0, TimeSpan.Zero);
            });
            UpdateDataset(c2, storage, d => d.IsPartOfInternalId = parentId.ToString());

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(parentId);

            await AssertRelatedDatasets(storage, c2, c1);

            IElementHandle? related = await Page.GetTestElement("related");
            Assert.IsNotNull(related);
            Assert.AreEqual("Datasety z tejto série", await (await related.QuerySelectorAsync("h4"))!.TextContentAsync());
        }

        [TestMethod]
        public async Task RelatedDatasetsShouldHaveChildrenOnChild()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            fixture.CreatePublisher(PublisherId, true);

            Guid parentId = CreateDatasetAndDistribution(fixture, "Test dataset", true);

            Guid c1 = CreateDatasetAndDistribution(fixture, "Child 1", true);
            Guid c2 = CreateDatasetAndDistribution(fixture, "Child 2", true);

            using Storage storage = new Storage(path);

            UpdateDataset(parentId, storage, d => d.IsSerie = true);
            UpdateDataset(c1, storage, d => d.IsPartOfInternalId = parentId.ToString());
            UpdateDataset(c2, storage, d => d.IsPartOfInternalId = parentId.ToString());

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenDatasetDetail(c1);

            await AssertRelatedDatasets(storage, c2);

            IElementHandle? related = await Page.GetTestElement("related");
            Assert.IsNotNull(related);
            Assert.AreEqual("Dalšie datasety z tejto série", await (await related.QuerySelectorAsync("h4"))!.TextContentAsync());

            IElementHandle? dataSerie = await Page.GetTestElement("data-serie");
            Assert.IsNotNull(dataSerie);
            IElementHandle? parentLink = await dataSerie.QuerySelectorAsync("a");
            Assert.IsNotNull(parentLink);
            Assert.AreEqual($"/datasety/{parentId}", await parentLink.GetAttributeAsync("href"));
            Assert.AreEqual("Zobraziť", await parentLink.TextContentAsync());
        }

        [TestMethod]
        public async Task PublisherFilterShouldBeSetFromUrl()
        {
            string path = fixture.GetStoragePath();

            string publisher1 = PublisherId + "1";
            string publisher2 = PublisherId + "2";

            fixture.CreatePublisher(publisher1, true, "Test Publisher 1");
            fixture.CreatePublisher(publisher2, true, "Test Publisher 2");

            Guid d1 = CreateDatasetAndDistribution(fixture, "Test 1", true, publisher: publisher1);
            Guid d2 = CreateDatasetAndDistribution(fixture, "Test 2", true, publisher: publisher2);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.GotoAsync($"http://localhost:6001/datasety?publisher={HttpUtility.UrlEncode(publisher2)}");
            }, new List<string> { "datasets/search" });

            Assert.AreEqual("1 výsledok", await Page.GetSearchResultsCount());
            IReadOnlyList<IElementHandle> results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
            Assert.AreEqual(1, results.Count);
            await AssertResult(results[0], d2, "Test 2", "Test Publisher 2");

            IElementHandle? filter = await Page.GetTestElement("sr-filter-publishers");
            Assert.IsNotNull(filter);

            IReadOnlyList<IElementHandle> checkBoxes = await filter.QuerySelectorAllAsync("input[type='checkbox']");
            Assert.AreEqual(2, checkBoxes.Count);

            Assert.AreEqual("Test Publisher 1 (1)", await Page.GetLabel(checkBoxes[0]));
            Assert.AreEqual("Test Publisher 2 (1)", await Page.GetLabel(checkBoxes[1]));

            Assert.IsFalse(await checkBoxes[0].IsCheckedAsync());
            Assert.IsTrue(await checkBoxes[1].IsCheckedAsync());
        }
    }
}
