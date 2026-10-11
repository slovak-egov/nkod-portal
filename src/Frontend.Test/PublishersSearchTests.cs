using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Frontend.Test
{
    [TestClass]
    public class PublishersSearchTests : PageTest
    {
        private StorageFixture fixture = new StorageFixture();

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublicFileAccessPolicy();

        private Guid CreatePublisher(StorageFixture fixture, string name, bool isPublic = true)
        {
            FoafAgent agent = FoafAgent.Create(new Uri(PublisherId));
            agent.SetNames(new Dictionary<string, string> { { "sk", name } });            
            return fixture.CreatePublisher(agent, isPublic);
        }

        [TestMethod]
        public async Task ResultsShouldBeEmpty()
        {
            string path = fixture.GetStoragePath();

            CreatePublisher(fixture, "Test", false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenPublisherSearch();

            Assert.AreEqual("0 výsledkov", await Page.GetSearchResultsCount());
            Assert.AreEqual(0, await Page.GetByTestId("sr-result").CountAsync());
        }

        [TestMethod]
        public async Task OneResultsShouldBeVisible()
        {
            string path = fixture.GetStoragePath();

            CreatePublisher(fixture, "Test", true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenPublisherSearch();

            Assert.AreEqual("1 výsledok", await Page.GetSearchResultsCount());
            IReadOnlyList<IElementHandle> results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
            Assert.AreEqual(1, results.Count);

            IElementHandle result = results[0];

            IElementHandle? link = await result.QuerySelectorAsync("a");
            Assert.IsNotNull(link);
            Assert.IsTrue(string.Equals($"/datasety?publisher={HttpUtility.UrlEncode(PublisherId)}", await link.GetAttributeAsync("href"), StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("Test (datasetov: 0)", await link.TextContentAsync());
        }

        [TestMethod]
        public async Task ResultShouldRespectQuery()
        {
            string path = fixture.GetStoragePath();

            CreatePublisher(fixture, "Test", true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenPublisherSearch();

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
                Assert.AreEqual("Test (datasetov: 0)", await link.TextContentAsync());
            }

            await Page.RunAndWaitForPublisherSearch(async () =>
            {
                await Page.GetByTestId("sr-query").FillAsync("Test");
            });
            await OneItem();

            await Page.RunAndWaitForPublisherSearch(async () =>
            {
                await Page.GetByTestId("sr-query").FillAsync("Abc");
            });
            await NoItems();

            await Page.RunAndWaitForPublisherSearch(async () =>
            {
                await Page.GetByTestId("sr-query").FillAsync(string.Empty);
            });
            await OneItem();
        }

        [TestMethod]
        public async Task SearchStateShouldBeStoredInUrlAndRestoredOnBack()
        {
            string path = fixture.GetStoragePath();

            for (int i = 0; i <= 10; i++)
            {
                FoafAgent agent = FoafAgent.Create(new Uri(PublisherId + i));
                agent.SetNames(new Dictionary<string, string> { { "sk", $"Test {i:00}" } });
                fixture.CreatePublisher(agent, true);
            }

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.OpenPublisherSearch();

            async Task AssertState(string queryText, string orderBy, int page, string firstResult)
            {
                Assert.AreEqual(queryText, await Page.GetByTestId("sr-query").InputValueAsync());
                Assert.AreEqual(orderBy, await Page.GetByTestId("sr-order").InputValueAsync());
                StringAssert.Contains(await (await Page.GetTestElement("sr-current-page"))!.TextContentAsync(), $"Strana {page} z");
                IElementHandle? link = await Page.QuerySelectorAsync("[data-testid='sr-result'] a");
                Assert.IsNotNull(link);
                StringAssert.StartsWith(await link.TextContentAsync(), firstResult);
            }

            await Page.RunAndWaitForPublisherSearch(async () =>
            {
                await Page.GetByTestId("sr-order").SelectOptionAsync("name");
            });
            await Page.RunAndWaitForPublisherSearch(async () =>
            {
                await (await Page.GetTestElement("sr-next-page"))!.ClickAsync();
            });

            Dictionary<string, string[]> parameters = Page.GetUrlParameters();
            CollectionAssert.AreEquivalent(new[] { "sort", "page" }, parameters.Keys);
            CollectionAssert.AreEqual(new[] { "name" }, parameters["sort"]);
            CollectionAssert.AreEqual(new[] { "2" }, parameters["page"]);
            await AssertState(string.Empty, "name", 2, "Test 10");
            string searchUrl = Page.Url;

            // Result links to dataset search; going back must restore publisher search
            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                await (await Page.QuerySelectorAsync("[data-testid='sr-result'] a"))!.ClickAsync();
            });
            await Page.RunAndWaitForPublisherSearch(async () => await Page.GoBackAsync());
            Assert.AreEqual(searchUrl, Page.Url);
            await AssertState(string.Empty, "name", 2, "Test 10");

            await Page.RunAndWaitForPublisherSearch(async () => await Page.GoBackAsync());
            CollectionAssert.AreEquivalent(new[] { "sort" }, Page.GetUrlParameters().Keys);
            await AssertState(string.Empty, "name", 1, "Test 00");

            await Page.RunAndWaitForPublisherSearch(async () => await Page.GoBackAsync());
            Assert.AreEqual(0, Page.GetUrlParameters().Count);
            Assert.AreEqual("relevance", await Page.GetByTestId("sr-order").InputValueAsync());

            await Page.RunAndWaitForPublisherSearch(async () =>
            {
                await Page.GetByTestId("sr-query").FillAsync("Test 05");
            });
            CollectionAssert.AreEqual(new[] { "Test 05" }, Page.GetUrlParameters()["query"]);
            searchUrl = Page.Url;

            await Page.RunAndWaitForDatasetSearch(async () =>
            {
                await (await Page.QuerySelectorAsync("[data-testid='sr-result'] a"))!.ClickAsync();
            });
            await Page.RunAndWaitForPublisherSearch(async () => await Page.GoBackAsync());
            Assert.AreEqual(searchUrl, Page.Url);
            Assert.AreEqual("Test 05", await Page.GetByTestId("sr-query").InputValueAsync());
        }

        [TestMethod]
        public async Task SearchStateShouldBeLoadedFromUrl()
        {
            string path = fixture.GetStoragePath();

            for (int i = 0; i <= 10; i++)
            {
                FoafAgent agent = FoafAgent.Create(new Uri(PublisherId + i));
                agent.SetNames(new Dictionary<string, string> { { "sk", $"Test {i:00}" } });
                fixture.CreatePublisher(agent, true);
            }

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.GotoAsync("http://localhost:6001/poskytovatelia?sort=name&page=2");
            }, new List<string> { "publishers/search" });

            Assert.AreEqual("11 výsledkov", await Page.GetSearchResultsCount());
            Assert.AreEqual(string.Empty, await Page.GetByTestId("sr-query").InputValueAsync());
            Assert.AreEqual("name", await Page.GetByTestId("sr-order").InputValueAsync());
            IReadOnlyList<IElementHandle> results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
            Assert.AreEqual(1, results.Count);
            StringAssert.StartsWith(await (await results[0].QuerySelectorAsync("a"))!.TextContentAsync(), "Test 10");

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.GotoAsync("http://localhost:6001/poskytovatelia?query=Test&sort=name&page=2");
            }, new List<string> { "publishers/search" });

            Assert.AreEqual("11 výsledkov", await Page.GetSearchResultsCount());
            Assert.AreEqual("Test", await Page.GetByTestId("sr-query").InputValueAsync());
            Assert.AreEqual("name", await Page.GetByTestId("sr-order").InputValueAsync());
            results = await Page.QuerySelectorAllAsync("[data-testid='sr-result']");
            Assert.AreEqual(1, results.Count);
            StringAssert.StartsWith(await (await results[0].QuerySelectorAsync("a"))!.TextContentAsync(), "Test 10");
        }
    }
}
