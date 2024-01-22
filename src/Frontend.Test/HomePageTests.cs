using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Frontend.Test
{
    [TestClass]
    public class HomePageTests : PageTest
    {
        private StorageFixture fixture = new StorageFixture();

        private const string PublisherId = "http://example.com/publisher";

        [TestMethod]
        public async Task TestEmptyByDefault()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.RunAndWaitForHomePage(Page.OpenHomePage);

            IReadOnlyCollection<IElementHandle> publishers = await Page.GetTestElements("publisher");
            Assert.AreEqual(0, publishers.Count);
        }

        private async Task CheckPublisher(IElementHandle element, string name, string publisher, string subHeader)
        {
            IElementHandle? link = await element.QuerySelectorAsync("a");
            Assert.IsNotNull(link);

            Assert.IsTrue(string.Equals($"/datasety?publisher={HttpUtility.UrlEncode(publisher)}", await link.GetAttributeAsync("href"), StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(name, await link.TextContentAsync());

            IElementHandle? subHeaderElement = await element.QuerySelectorAsync(".idsk-crossroad-subtitle");
            Assert.IsNotNull(subHeaderElement);

            Assert.AreEqual(subHeader, await subHeaderElement.TextContentAsync());
        }

        [TestMethod]
        public async Task TestPublishersOnTitle()
        {
            string path = fixture.GetStoragePath();

            List<string> publishers = new List<string>(5);
            for (int i = 1; i <= 5; i++)
            {
                string key = $"{PublisherId}{i}";
                fixture.CreatePublisher(key, true, $"Test Publisher {i}");
                publishers.Add(key);
            }

            int index = 0;

            void CreateDatasets(string publisher, int count)
            {
                Uri publisherUri = new Uri(publisher);
                for (int i = 0; i < count; i++)
                {
                    DcatDataset dataset = DcatDataset.Create();
                    dataset.SetTitle(new Dictionary<string, string> { { "sk", $"Test {index}" } });
                    dataset.Publisher = publisherUri;
                    dataset.ShouldBePublic = true;

                    List<string> keywords = new List<string>();
                    if ((index % 2) == 0)
                    {
                        keywords.Add("K1");
                    }
                    if ((index % 3) == 0)
                    {
                        keywords.Add("K2");
                    }
                    if ((index % 4) == 0)
                    {
                        keywords.Add("K3");
                    }
                    if ((index % 5) == 0)
                    {
                        keywords.Add("K4");
                    }

                    dataset.SetKeywords(new Dictionary<string, List<string>> { { "sk", keywords } });

                    Guid datasetId = fixture.CreateDataset(dataset);

                    fixture.CreateDistribution(datasetId, publisher);
                    index++;
                }
            }

            CreateDatasets(publishers[0], 5);
            CreateDatasets(publishers[1], 6);
            CreateDatasets(publishers[2], 11);
            CreateDatasets(publishers[3], 4);
            CreateDatasets(publishers[4], 7);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();

            await Page.RunAndWaitForHomePage(Page.OpenHomePage);

            IReadOnlyList<IElementHandle> publisherElements = await Page.GetTestElements("publisher");
            Assert.AreEqual(4, publisherElements.Count);

            await CheckPublisher(publisherElements[0], "Test Publisher 3", publishers[2], "Poskytovateľ zverejňuje 11 datasetov, najviac z oblastí K1 (5), K2 (4), K3 (3).");
            await CheckPublisher(publisherElements[1], "Test Publisher 5", publishers[4], "Poskytovateľ zverejňuje 7 datasetov, najviac z oblastí K1 (4), K2 (2), K3 (2).");
            await CheckPublisher(publisherElements[2], "Test Publisher 2", publishers[1], "Poskytovateľ zverejňuje 6 datasetov, najviac z oblastí K1 (3), K2 (2), K4 (2).");
            await CheckPublisher(publisherElements[3], "Test Publisher 1", publishers[0], "Poskytovateľ zverejňuje 5 datasetov, najviac z oblastí K1 (3), K2 (2), K3 (2).");
        }

        [TestMethod]
        public async Task RedirectOnNotFoundPagePost()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            using HttpClient client = f.CreateDefaultClient();
            using HttpResponseMessage response = await client.PostAsync("http://localhost:6001/404", null);

            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("http://localhost:6001/404", response.Headers.Location?.ToString());
        }

        [TestMethod]
        public async Task AccessTokenShouldBeRefreshed()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.CreateDefaultClient();
            f.TestIdentityAccessManagementClient.RefreshTokenAfter = TimeSpan.FromSeconds(15);

            await Page.Login(f, PublisherId, "Publisher");

            await Page.TakeScreenshot();

            await Page.WaitForRefershToken();
        }
    }
}
