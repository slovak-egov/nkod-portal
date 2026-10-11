using AngleSharp.Dom;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TestBase;
using static System.Net.Mime.MediaTypeNames;

namespace Frontend.Test
{
    [TestClass]
    public class LocalCatalogsSearchTests : PageTest
    {
        private StorageFixture fixture = new StorageFixture();

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublicFileAccessPolicy();

        private Guid CreateCatalog(StorageFixture fixture, string name, bool isPublic = true)
        {
            DcatCatalog catalog = DcatCatalog.Create();
            catalog.SetTitle(new Dictionary<string, string> { { "sk", name } });
            catalog.SetDescription(new Dictionary<string, string> { { "sk", "Test Description" } });
            catalog.Publisher = new Uri(PublisherId);
            catalog.ShouldBePublic = isPublic;
            catalog.HomePage = new Uri("http://example.com/");
            return fixture.CreateLocalCatalog(catalog);
        }

        [TestMethod]
        public async Task ResultsShouldBeEmpty()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            CreateCatalog(fixture, "Test", false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenLocalCatalogSearch();

            Assert.AreEqual("0 výsledkov", await Page.GetSearchResultsCount());
            Assert.AreEqual(0, await Page.GetByTestId("sr-result").CountAsync());
        }

        [TestMethod]
        public async Task OneResultsShouldBeVisible()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            CreateCatalog(fixture, "Test", true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenLocalCatalogSearch();

            Assert.AreEqual("1 výsledok", await Page.GetSearchResultsCount());
            IReadOnlyList<IElementHandle> results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
            Assert.AreEqual(1, results.Count);

            IElementHandle result = results[0];
            
            IElementHandle? link = await result.QuerySelectorAsync("a");
            Assert.IsNotNull(link);
            Assert.AreEqual("Test", await link.TextContentAsync());
            
            IElementHandle? publisherName = await result.QuerySelectorAsync("[data-testid='sr-result-publisher']");
            Assert.IsNotNull(publisherName);
            Assert.AreEqual("Test Publisher", await publisherName.TextContentAsync());
        }

        [TestMethod]
        public async Task ResultShouldRespectQuery()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            CreateCatalog(fixture, "Test", true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenLocalCatalogSearch();

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

                IElementHandle? link = await result.QuerySelectorAsync("a");
                Assert.IsNotNull(link);
                Assert.AreEqual("Test", await link.TextContentAsync());
            }

            await Page.RunAndWaitForLocalCatalogSearch(async () =>
            {
                await Page.GetByTestId("sr-query").FillAsync("Test");
            });
            await OneItem();

            await Page.RunAndWaitForLocalCatalogSearch(async () =>
            {
                await Page.GetByTestId("sr-query").FillAsync("Abc");
            });
            await NoItems();

            await Page.RunAndWaitForLocalCatalogSearch(async () =>
            {
                await Page.GetByTestId("sr-query").FillAsync(string.Empty);
            });
            await OneItem();
        }

        [TestMethod]
        public async Task LinkShouldOpenDetail()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateCatalog(fixture, "Test", true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenLocalCatalogSearch();

            IElementHandle? link = await Page.QuerySelectorAsync("[data-testid='sr-result'] a");
            Assert.IsNotNull(link);
            await link.ClickAsync();

            await Page.WaitForURLAsync($"http://localhost:6001/lokalne-katalogy/{id}");
        }

        [TestMethod]
        public async Task DetailShouldBeDisplayed()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            Guid id = CreateCatalog(fixture, "Test", true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenLocalCatalogDetail(id);

            Assert.AreEqual("Národný katalóg otvorených dát - Test", await Page.TitleAsync());
            Assert.AreEqual("Test", await (await Page.QuerySelectorAsync("h1"))!.TextContentAsync());

            await Page.CheckTestContent("publisher-name", "Test Publisher");
            await Page.CheckTestContent("description", "Test Description");
            
            IElementHandle? homePageLink = await Page.QuerySelectorAsync("[data-testid='homepage']");
            Assert.IsNotNull(homePageLink);
            Assert.AreEqual("http://example.com/", await homePageLink.GetAttributeAsync("href"));
        }

        [TestMethod]
        public async Task SearchStateShouldBeStoredInUrlAndRestoredOnBack()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            for (int i = 0; i <= 10; i++)
            {
                CreateCatalog(fixture, $"Test {i:00}");
            }

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenLocalCatalogSearch();

            async Task AssertState(string orderBy, int page, bool publisherChecked, string firstResult)
            {
                Assert.AreEqual(orderBy, await Page.GetByTestId("sr-order").InputValueAsync());
                StringAssert.Contains(await (await Page.GetTestElement("sr-current-page"))!.TextContentAsync(), $"Strana {page} z");
                IElementHandle? filter = await Page.GetTestElement("sr-filter-publishers");
                Assert.IsNotNull(filter);
                Assert.AreEqual(publisherChecked, await (await filter.QuerySelectorAsync("input[type='checkbox']"))!.IsCheckedAsync());
                IElementHandle? link = await Page.QuerySelectorAsync("[data-testid='sr-result'] a");
                Assert.IsNotNull(link);
                Assert.AreEqual(firstResult, await link.TextContentAsync());
            }

            await Page.RunAndWaitForLocalCatalogSearch(async () =>
            {
                await Page.GetByTestId("sr-order").SelectOptionAsync("name");
            });
            await Page.RunAndWaitForLocalCatalogSearch(async () =>
            {
                IElementHandle? filter = await Page.GetTestElement("sr-filter-publishers");
                await (await filter!.QuerySelectorAsync("input[type='checkbox']"))!.CheckAsync();
            });
            await Page.RunAndWaitForLocalCatalogSearch(async () =>
            {
                await (await Page.GetTestElement("sr-next-page"))!.ClickAsync();
            });

            Dictionary<string, string[]> parameters = Page.GetUrlParameters();
            CollectionAssert.AreEquivalent(new[] { "sort", "publisher", "page" }, parameters.Keys);
            CollectionAssert.AreEqual(new[] { PublisherId }, parameters["publisher"]);
            await AssertState("name", 2, true, "Test 10");
            string searchUrl = Page.Url;

            await (await Page.QuerySelectorAsync("[data-testid='sr-result'] a"))!.ClickAsync();
            await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex("^http://localhost:6001/lokalne-katalogy/[0-9a-f-]+$"));

            await Page.RunAndWaitForLocalCatalogSearch(async () => await Page.GoBackAsync());
            Assert.AreEqual(searchUrl, Page.Url);
            await AssertState("name", 2, true, "Test 10");

            await Page.RunAndWaitForLocalCatalogSearch(async () => await Page.GoBackAsync());
            await AssertState("name", 1, true, "Test 00");

            await Page.RunAndWaitForLocalCatalogSearch(async () => await Page.GoBackAsync());
            CollectionAssert.AreEquivalent(new[] { "sort" }, Page.GetUrlParameters().Keys);
            await AssertState("name", 1, false, "Test 00");

            await Page.RunAndWaitForLocalCatalogSearch(async () => await Page.GoBackAsync());
            Assert.AreEqual(0, Page.GetUrlParameters().Count);
            Assert.AreEqual("relevance", await Page.GetByTestId("sr-order").InputValueAsync());
        }

        [TestMethod]
        public async Task SearchStateShouldBeLoadedFromUrl()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher(PublisherId, true);
            for (int i = 0; i <= 10; i++)
            {
                CreateCatalog(fixture, $"Test {i:00}");
            }

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.GotoAsync($"http://localhost:6001/lokalne-katalogy?sort=name&page=2&publisher={Uri.EscapeDataString(PublisherId)}");
            }, new List<string> { "local-catalogs/search" });

            Assert.AreEqual("11 výsledkov", await Page.GetSearchResultsCount());
            Assert.AreEqual("name", await Page.GetByTestId("sr-order").InputValueAsync());
            IReadOnlyList<IElementHandle> results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Test 10", await (await results[0].QuerySelectorAsync("a"))!.TextContentAsync());
        }
    }
}
