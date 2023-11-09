using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright.MSTest;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace Frontend.Test
{
    [TestClass]
    public class PublishersTests : PageTest
    {
        private StorageFixture fixture = new StorageFixture();

        private readonly IFileStorageAccessPolicy accessPolicy = new AllAccessFilePolicy();

        [TestMethod]
        public async Task TableShouldBeEmptyByDefault()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, null, "Superadmin");

            await Page.OpenPublishersAdmin();

            await Page.AssertNoTable();
        }

        [TestMethod]
        public async Task TableShouldHaveOneRow()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisher("http://example.com/publisher", false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, null, "Superadmin");

            await Page.OpenPublishersAdmin();

            await Page.AssertTableRowsCount(1);
        }

        [TestMethod]
        public async Task PublisherShouldBeActivatedAndDeactivated()
        {
            string path = fixture.GetStoragePath();

            Guid id = fixture.CreatePublisher("http://example.com/publisher", false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, null, "Superadmin");

            await Page.OpenPublishersAdmin();

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.ClickOnTableButton(0, "Aktivovať");
            }, new List<string> { "publishers" });

            FileState? state = storage.GetFileState(id, accessPolicy);
            Assert.IsNotNull(state);
            Assert.IsTrue(state.Metadata.IsPublic);

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.ClickOnTableButton(0, "Deaktivovať");
            }, new List<string> { "publishers" });

            state = storage.GetFileState(id, accessPolicy);
            Assert.IsNotNull(state);
            Assert.IsFalse(state.Metadata.IsPublic);
        }
    }
}
