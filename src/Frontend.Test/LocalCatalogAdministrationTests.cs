using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace Frontend.Test
{
    [TestClass]
    public class LocalCatalogAdministrationTests : PageTest
    {
        private StorageFixture fixture = new StorageFixture();

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherFileAccessPolicy(PublisherId);

        [TestMethod]
        public async Task TableShouldBeEmptyByDefault()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateLocalCatalog("Test", PublisherId + "!");

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();

            await Page.AssertNoTable();
        }

        [TestMethod]
        public async Task TableShouldHaveOneRow()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateLocalCatalog("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();

            await Page.AssertTableRowsCount(1);
        }

        [TestMethod]
        public async Task TestNavigateToNewRecord()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateLocalCatalogCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();
            await Page.RunAndWaitForLocalCatalogCreate(async () =>
            {
                await Page.GetByText("Nový lokálny katalóg").ClickAsync();
            });

            DcatCatalog localCatalog = DcatCatalog.Create();
            localCatalog.Type = new Uri("https://data.gov.sk/def/local-catalog-type/1");
            localCatalog.SetTitle(new Dictionary<string, string> { { "sk", string.Empty } });
            localCatalog.SetDescription(new Dictionary<string, string> { { "sk", string.Empty } });
            localCatalog.SetContactPoint(new LanguageDependedTexts { { "sk", string.Empty } }, string.Empty);

            await Page.AssertLocalCatalogForm(localCatalog);
        }

        private DcatCatalog CreateMinimalLocalCatalog(int? index = null)
        {
            DcatCatalog input = DcatCatalog.Create();
            input.Publisher = new Uri(PublisherId);
            input.SetTitle(new Dictionary<string, string> { { "sk", "NameSk" } });
            input.SetDescription(new Dictionary<string, string> { { "sk", "DescriptionSk" } });
            input.HomePage = new Uri("http://example.com/catalog");
            input.Type = new Uri("https://data.gov.sk/def/local-catalog-type/1");
            input.EndpointUrl = new Uri("http://example.com/endpoint");
            input.SetContactPoint(new LanguageDependedTexts { { "sk", string.Empty } }, string.Empty);
            return input;
        }

        private DcatCatalog CreateMaximalLocalCatalog(int? index = null, bool includeLanguages = false)
        {
            DcatCatalog input = CreateMinimalLocalCatalog(index);
            input.SetContactPoint(new LanguageDependedTexts(new Dictionary<string, string> { { "sk", "ContactSk" } }), "test@example.com");

            if (includeLanguages)
            {
                Dictionary<string, string> values = input.Title;
                values["en"] = "NameEn";
                input.SetTitle(values);

                values = input.Description;
                values["en"] = "DescriptionEn";
                input.SetDescription(values);

                values = input.ContactPoint!.Name;
                values["en"] = "ContactEn";
                input.ContactPoint!.SetNames(values);
            }

            return input;
        }

        [TestMethod]
        public async Task TestCreateMinimalLocalCatalog()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateLocalCatalogCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();
            await Page.RunAndWaitForLocalCatalogCreate(async () =>
            {
                await Page.GetByText("Nový lokálny katalóg").ClickAsync();
            });

            DcatCatalog input = CreateMinimalLocalCatalog();
            await Page.FillLocalCatalogFields(input);

            await Page.RunAndWaitForLocalCatalogList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, Extensions.GetLastEntity(storage, FileType.LocalCatalogRegistration)!);
        }

        [TestMethod]
        public async Task TestCreateMaximalLocalCatalog()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateLocalCatalogCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();
            await Page.RunAndWaitForLocalCatalogCreate(async () =>
            {
                await Page.GetByText("Nový").ClickAsync();
            });

            DcatCatalog input = CreateMaximalLocalCatalog();
            await Page.FillLocalCatalogFields(input);

            await Page.RunAndWaitForLocalCatalogList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, Extensions.GetLastEntity(storage, FileType.LocalCatalogRegistration)!);
        }

        [TestMethod]
        public async Task TestEditRecordMinimalWithoutChange()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateLocalCatalogCodelists();

            DcatCatalog localCatalog = CreateMinimalLocalCatalog();
            Guid id = fixture.CreateLocalCatalog(localCatalog);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();
            await Page.RunAndWaitForLocalCatalogEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.RunAndWaitForLocalCatalogList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.LocalCatalogRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(localCatalog, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMaximalWithoutChange()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateLocalCatalogCodelists();

            DcatCatalog localCatalog = CreateMaximalLocalCatalog();
            Guid id = fixture.CreateLocalCatalog(localCatalog);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();
            await Page.RunAndWaitForLocalCatalogEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.RunAndWaitForLocalCatalogList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.LocalCatalogRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(localCatalog, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMinimal()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateLocalCatalogCodelists();

            DcatCatalog localCatalog = CreateMinimalLocalCatalog();
            Guid id = fixture.CreateLocalCatalog(localCatalog);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();
            await Page.RunAndWaitForLocalCatalogEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.AssertLocalCatalogForm(localCatalog);

            DcatCatalog input = DcatCatalog.Create();
            input.Publisher = new Uri(PublisherId);
            input.SetTitle(new Dictionary<string, string> { { "sk", "NameSk2" } });
            input.SetDescription(new Dictionary<string, string> { { "sk", "DescriptionSk2" } });
            input.Type = new Uri("https://data.gov.sk/def/local-catalog-type/2");
            input.EndpointUrl = new Uri("http://example.com/endpoint/other");

            await Page.FillLocalCatalogFields(input);

            await Page.RunAndWaitForLocalCatalogList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.LocalCatalogRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMaximal()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateLocalCatalogCodelists();

            DcatCatalog localCatalog = CreateMaximalLocalCatalog();
            Guid id = fixture.CreateLocalCatalog(localCatalog);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();

            await Page.RunAndWaitForLocalCatalogEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.AssertLocalCatalogForm(localCatalog);

            DcatCatalog input = DcatCatalog.Create();
            input.Publisher = new Uri(PublisherId);
            input.SetTitle(new Dictionary<string, string> { { "sk", "NameSk2" } });
            input.SetDescription(new Dictionary<string, string> { { "sk", "DescriptionSk2" } });
            input.HomePage = new Uri("http://example.com/catalog/other");
            input.ShouldBePublic = false;
            input.Type = new Uri("https://data.gov.sk/def/local-catalog-type/2");
            input.EndpointUrl = new Uri("http://example.com/endpoint/other");

            input.SetContactPoint(new LanguageDependedTexts(new Dictionary<string, string> { { "sk", "ContactSk2" } }), "test2@example.com");

            await Page.FillLocalCatalogFields(input);

            await Page.RunAndWaitForLocalCatalogList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.LocalCatalogRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestRemoveRecord()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateLocalCatalogCodelists();

            fixture.CreateLocalCatalog("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();

            Page.Dialog += (_, dialog) => dialog.AcceptAsync();
            await Page.RunAndWaitForLocalCatalogList(async () =>
            {
                await Page.ClickOnTableButton(0, "Odstrániť");
            });

            await Page.AssertNoTable();
        }

        [TestMethod]
        public async Task TestCreateMaximalCatalogWithLanguageOverride()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateLocalCatalogCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();
            await Page.RunAndWaitForLocalCatalogCreate(async () =>
            {
                await Page.GetByText("Nový").ClickAsync();
            });

            DcatCatalog input = CreateMaximalLocalCatalog(includeLanguages: true);
            await Page.FillLocalCatalogFields(input);

            await Page.RunAndWaitForLocalCatalogList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, Extensions.GetLastEntity(storage, FileType.LocalCatalogRegistration)!);
        }

        [TestMethod]
        public async Task TestEditMaximalCatalogWithLanguageOverride()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateLocalCatalogCodelists();

            DcatCatalog localCatalog = CreateMaximalLocalCatalog();
            Guid id = fixture.CreateLocalCatalog(localCatalog);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenLocalCatalogsAdmin();

            await Page.RunAndWaitForLocalCatalogEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.AssertLocalCatalogForm(localCatalog);

            DcatCatalog input = DcatCatalog.Create();
            input.Publisher = new Uri(PublisherId);
            input.SetTitle(new Dictionary<string, string> { { "sk", "NameSk2" }, { "en", "NameEn2" } });
            input.SetDescription(new Dictionary<string, string> { { "sk", "DescriptionSk2" }, { "en", "DescriptionEn2" } });
            input.HomePage = new Uri("http://example.com/catalog/other");
            input.ShouldBePublic = false;
            input.Type = new Uri("https://data.gov.sk/def/local-catalog-type/2");
            input.EndpointUrl = new Uri("http://example.com/endpoint/other");

            input.SetContactPoint(new LanguageDependedTexts(new Dictionary<string, string> { { "sk", "ContactSk2" }, { "en", "ContactEn2" } }), "test2@example.com");

            await Page.FillLocalCatalogFields(input);

            await Page.RunAndWaitForLocalCatalogList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.LocalCatalogRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, storage.GetFileState(id, accessPolicy)!);
        }
    }
}
