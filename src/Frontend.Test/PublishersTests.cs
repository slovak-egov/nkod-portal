using Microsoft.AspNetCore.Mvc.RazorPages;
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
        public async Task TestNavigateToNewRecord()
        {
            string path = fixture.GetStoragePath();

            fixture.CreatePublisherCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, null, "Superadmin");

            await Page.OpenPublishersAdmin();
            await Page.RunAndWaitForPublisherCreate(async () =>
            {
                await Page.GetByText("Nový poskytovateľ dát").ClickAsync();
            });

            FoafAgent agent = FoafAgent.Create(new Uri("https://data.gov.sk/id/legal-subject/"));
            agent.LegalForm = new Uri("https://data.gov.sk/def/legal-form-type/321");
            await Page.AssertPublisherForm(agent);
        }

        private FoafAgent CreatePublisherInput()
        {
            FoafAgent input = FoafAgent.Create(new Uri("http://example.com/"));
            input.SetNames(new Dictionary<string, string> { { "sk", "NameSk" } });
            input.HomePage = new Uri("https://slovensko.sk/");
            input.EmailAddress = "test@exaple.com";
            input.Phone = "0901 123456";
            input.LegalForm = new Uri("https://data.gov.sk/def/legal-form-type/331");
            return input;
        }

        [TestMethod]
        public async Task TestCreatePublisher()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, null, "Superadmin");

            await Page.OpenPublishersAdmin();
            await Page.RunAndWaitForPublisherCreate(async () =>
            {
                await Page.GetByText("Nový poskytovateľ dát").ClickAsync();
            });

            FoafAgent input = CreatePublisherInput();

            await Page.FillAdminPublisherForm(input, false);

            await Page.RunAndWaitForPublisherList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, Extensions.GetLastEntity(storage, FileType.PublisherRegistration)!, false);
        }

        [TestMethod]
        public async Task TestCreatePublisherEnabled()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, null, "Superadmin");

            await Page.OpenPublishersAdmin();
            await Page.RunAndWaitForPublisherCreate(async () =>
            {
                await Page.GetByText("Nový poskytovateľ dát").ClickAsync();
            });

            FoafAgent input = CreatePublisherInput();

            await Page.FillAdminPublisherForm(input, true);

            await Page.RunAndWaitForPublisherList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, Extensions.GetLastEntity(storage, FileType.PublisherRegistration)!, true);
        }

        [TestMethod]
        public async Task TestEditRecordMinimalWithoutChange()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            FoafAgent input = CreatePublisherInput();
            Guid id = fixture.CreatePublisher(input, false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, null, "Superadmin");

            await Page.OpenPublishersAdmin();
            await Page.RunAndWaitForPublisherEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.RunAndWaitForPublisherList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.PublisherRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, storage.GetFileState(id, accessPolicy)!, false);
        }

        [TestMethod]
        public async Task TestEditRecordMinimal()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            FoafAgent old = CreatePublisherInput();
            Guid id = fixture.CreatePublisher(old, false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, null, "Superadmin");

            await Page.OpenPublishersAdmin();
            await Page.RunAndWaitForPublisherEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            FoafAgent input = FoafAgent.Create(new Uri("http://example.com/"));
            input.SetNames(new Dictionary<string, string> { { "sk", "NameSk2" } });
            input.HomePage = new Uri("https://test.sk/");
            input.EmailAddress = "test2@exaple.com";
            input.Phone = "+421901123456";
            input.LegalForm = new Uri("https://data.gov.sk/def/legal-form-type/321");

            await Page.FillAdminPublisherForm(input, false);

            await Page.RunAndWaitForPublisherList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.PublisherRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, storage.GetFileState(id, accessPolicy)!, false);
        }

        [TestMethod]
        public async Task TestEditRecordActivate()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            FoafAgent input = CreatePublisherInput();
            Guid id = fixture.CreatePublisher(input, false);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, null, "Superadmin");

            await Page.OpenPublishersAdmin();
            await Page.RunAndWaitForPublisherEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.FillAdminPublisherForm(input, true);

            await Page.RunAndWaitForPublisherList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.PublisherRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, storage.GetFileState(id, accessPolicy)!, true);
        }

        [TestMethod]
        public async Task TestEditRecordDeactivate()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            FoafAgent input = CreatePublisherInput();
            Guid id = fixture.CreatePublisher(input, true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, null, "Superadmin");

            await Page.OpenPublishersAdmin();
            await Page.RunAndWaitForPublisherEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.FillAdminPublisherForm(input, false);

            await Page.RunAndWaitForPublisherList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.PublisherRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, storage.GetFileState(id, accessPolicy)!, false);
        }
    }
}
