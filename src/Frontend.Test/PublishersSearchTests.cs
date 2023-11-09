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
    }
}
