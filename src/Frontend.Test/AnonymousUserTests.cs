//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Playwright;
//using Microsoft.Playwright.MSTest;
//using NkodSk.Abstractions;
//using NkodSk.RdfFileStorage;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using TestBase;

//namespace Frontend.Test
//{
//    [TestClass]
//    public class AnonymousUserTests : PageTest
//    {
//        private StorageFixture fixture = new StorageFixture();

//        private const string PublisherId = "http://example.com/publisher";

//        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherFileAccessPolicy(PublisherId);

//        [TestMethod]
//        public async Task AdminMenuIsNotVisible()
//        {
//            string path = fixture.GetStoragePath();
//            using Storage storage = new Storage(path);
//            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
//            using HttpClient client = f.CreateDefaultClient();

//            await Page.GotoAsync("http://localhost:6001/");

//            IReadOnlyList<IElementHandle> menuItems = await Page.QuerySelectorAllAsync("header nav li a");
//            IElementHandle lastMenuItem = menuItems.Last();
//            Assert.AreNotEqual("Správa", await lastMenuItem.TextContentAsync());
//        }
//    }
//}
