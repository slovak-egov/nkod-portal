using Lucene.Net.Search.Similarities;
using Microsoft.AspNetCore.Authorization;
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
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace Frontend.Test
{
    [TestClass]
    public class DistributionAdministrationTests : PageTest
    {
        private StorageFixture fixture = new StorageFixture();

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherFileAccessPolicy(PublisherId);

        [TestMethod]
        public async Task TableShouldBeEmptyByDefault()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);

            await Page.AssertNoTable();
        }

        [TestMethod]
        public async Task TableShouldHaveOneRow()
        {
            string path = fixture.GetStoragePath();

            Guid datasetId = fixture.CreateDataset("Test", PublisherId);
            fixture.CreateDistribution(datasetId, PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);

            await Page.AssertTableRowsCount(1);
        }

        [TestMethod]
        public async Task TestNavigateToNewRecord()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDistributionCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);
            await Page.RunAndWaitForDistributionCreate(datasetId, async () =>
            {
                await Page.GetByText("Nová distribúcia").ClickAsync();
            });

            DcatDistribution distribution = DcatDistribution.Create(datasetId);
            distribution.SetTermsOfUse(
                new Uri("https://creativecommons.org/licenses/by/4.0/"),
                new Uri("https://creativecommons.org/licenses/by/4.0/"),
                new Uri("https://creativecommons.org/publicdomain/zero/1.0/"),
                new Uri("https://data.gov.sk/def/personal-data-occurence-type/2"));
            distribution.Format = new Uri("http://publications.europa.eu/resource/authority/file-type/CSV");
            distribution.MediaType = new Uri("http://www.iana.org/assignments/media-types/text/csv");
            distribution.SetTitle(new Dictionary<string, string> { { "sk", string.Empty } });

            await Page.AssertDistributionForm(distribution);
        }

        private DcatDistribution CreateMinimalDistribution(int? index = null)
        {
            DcatDistribution input = DcatDistribution.Create(Guid.NewGuid());
            input.SetTermsOfUse(
                new Uri("https://creativecommons.org/licenses/by/4.0/"),
                new Uri("https://creativecommons.org/licenses/by/4.0/"),
                new Uri("https://creativecommons.org/publicdomain/zero/1.0/"),
                new Uri("https://data.gov.sk/def/personal-data-occurence-type/2"));
            input.DownloadUrl = new Uri("http://example.com/download");
            input.AccessUrl = input.DownloadUrl;
            input.Format = new Uri("http://publications.europa.eu/resource/dataset/file-type/1");
            input.MediaType = new Uri("http://www.iana.org/assignments/media-types/text/csv");
            input.CompressFormat = new Uri("http://www.iana.org/assignments/media-types/application/zip");
            input.PackageFormat = input.CompressFormat;
            input.SetTitle(new Dictionary<string, string> { { "sk", string.Empty } });

            return input;
        }

        private DcatDistribution CreateMaximalDistribution(int? index = null, bool includeLanguages = false)
        {
            DcatDistribution input = CreateMinimalDistribution(index);
            input.ConformsTo = new Uri("http://example.com/conforms");
            input.SetTitle(new Dictionary<string, string> { { "sk", "TestSk" } });

            if (includeLanguages)
            {
                Dictionary<string, string> values = input.Title;
                values["en"] = "NameEn";
                values["de"] = "NameDe";
                input.SetTitle(values);
            }

            return input;
        }

        [TestMethod]
        public async Task TestCreateMinimalDistribution()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDistributionCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);
            await Page.RunAndWaitForDistributionCreate(datasetId, async () =>
            {
                await Page.GetByText("Nová distribúcia").ClickAsync();
            });

            DcatDistribution input = CreateMinimalDistribution();
            await Page.FillDistributionFields(input);

            await Page.RunAndWaitForDistributionList(datasetId, async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, Extensions.GetLastEntity(storage, FileType.DistributionRegistration)!);
        }

        [TestMethod]
        public async Task TestCreateMinimalDistributionWithUpload()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDistributionCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);
            await Page.RunAndWaitForDistributionCreate(datasetId, async () =>
            {
                await Page.GetByText("Nová distribúcia").ClickAsync();
            });

            DcatDistribution input = CreateMinimalDistribution();
            input.AccessUrl = null;
            input.DownloadUrl = null;
            await Page.FillDistributionFields(input);

            await Page.UplaodDistributionFile();

            await Page.RunAndWaitForDistributionList(datasetId, async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            FileState state = Extensions.GetLastEntity(storage, FileType.DistributionRegistration)!;
            DcatDistribution distribution = DcatDistribution.Parse(state.Content!)!;
            Assert.IsNotNull(distribution.DownloadUrl);
            Assert.AreEqual(distribution.DownloadUrl, distribution.AccessUrl);
            StringAssert.StartsWith(distribution.DownloadUrl.ToString(), "http://localhost:6001/download?");

            input.DownloadUrl = distribution.DownloadUrl;
            input.AccessUrl = distribution.AccessUrl;

            using HttpClient client = f.CreateDefaultClient();
            Assert.AreEqual("test", await client.GetStringAsync(distribution.DownloadUrl));

            Extensions.AssertAreEqual(input, Extensions.GetLastEntity(storage, FileType.DistributionRegistration)!);
        }

        [TestMethod]
        public async Task TestCreateMaximalDistribution()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDistributionCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);
            await Page.RunAndWaitForDistributionCreate(datasetId, async () =>
            {
                await Page.GetByText("Nová distribúcia").ClickAsync();
            });

            DcatDistribution input = CreateMaximalDistribution();
            await Page.FillDistributionFields(input);

            await Page.RunAndWaitForDistributionList(datasetId, async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, Extensions.GetLastEntity(storage, FileType.DistributionRegistration)!);
        }

        [TestMethod]
        public async Task TestEditRecordMinimalWithoutChange()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDistributionCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            DcatDistribution distribution = CreateMinimalDistribution();
            Guid id = fixture.CreateDistribution(datasetId, PublisherId, distribution);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);
            await Page.RunAndWaitForDistributionEdit(id, datasetId, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.RunAndWaitForDistributionList(datasetId, async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DistributionRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(distribution, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMaximalWithoutChange()
        {
            string path = fixture.GetStoragePath();
            
            fixture.CreateDistributionCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            DcatDistribution distribution = CreateMaximalDistribution();
            Guid id = fixture.CreateDistribution(datasetId, PublisherId, distribution);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);
            await Page.RunAndWaitForDistributionEdit(id, datasetId, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.RunAndWaitForDistributionList(datasetId, async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DistributionRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(distribution, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMinimal()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDistributionCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            DcatDistribution distribution = CreateMinimalDistribution();
            Guid id = fixture.CreateDistribution(datasetId, PublisherId, distribution);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);
            await Page.RunAndWaitForDistributionEdit(id, datasetId, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.AssertDistributionForm(distribution);

            DcatDistribution input = DcatDistribution.Create(datasetId);
            input.SetTermsOfUse(
                new Uri("https://data.gov.sk/def/authors-work-type/1"),
                new Uri("https://data.gov.sk/def/original-database-type/1"),
                new Uri("https://data.gov.sk/def/codelist/database-creator-special-rights-type/2"),
                new Uri("https://data.gov.sk/def/personal-data-occurence-type/1")
            );
            input.DownloadUrl = new Uri("http://example.com/download/other");
            input.AccessUrl = input.DownloadUrl;
            input.Format = new Uri("http://publications.europa.eu/resource/dataset/file-type/2");
            input.MediaType = new Uri("http://www.iana.org/assignments/media-types/text/xml");
            input.CompressFormat = new Uri("http://www.iana.org/assignments/media-types/application/rar");
            input.PackageFormat = input.CompressFormat;

            await Page.FillDistributionFields(input);

            await Page.RunAndWaitForDistributionList(datasetId, async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DistributionRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMaximal()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDistributionCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            DcatDistribution distribution = CreateMaximalDistribution();
            Guid id = fixture.CreateDistribution(datasetId, PublisherId, distribution);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);

            await Page.RunAndWaitForDistributionEdit(id, datasetId, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.AssertDistributionForm(distribution);

            DcatDistribution input = DcatDistribution.Create(datasetId);
            input.SetTermsOfUse(
                new Uri("https://data.gov.sk/def/authors-work-type/1"),
                new Uri("https://data.gov.sk/def/original-database-type/1"),
                new Uri("https://data.gov.sk/def/codelist/database-creator-special-rights-type/2"),
                new Uri("https://data.gov.sk/def/personal-data-occurence-type/1")
            );
            input.DownloadUrl = new Uri("http://example.com/download/other");
            input.AccessUrl = input.DownloadUrl;
            input.Format = new Uri("http://publications.europa.eu/resource/dataset/file-type/2");
            input.MediaType = new Uri("http://www.iana.org/assignments/media-types/text/xml");
            input.CompressFormat = new Uri("http://www.iana.org/assignments/media-types/application/rar");
            input.PackageFormat = input.CompressFormat;
            input.ConformsTo = new Uri("http://example.com/conforms/other");
            input.SetTitle(new Dictionary<string, string> { { "sk", "TestSkOther" } });

            await Page.FillDistributionFields(input);

            await Page.RunAndWaitForDistributionList(datasetId, async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DistributionRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestRemoveRecord()
        {
            string path = fixture.GetStoragePath();

            Guid datasetId = fixture.CreateDataset("Test", PublisherId);
            Guid id = fixture.CreateDistribution(datasetId, PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);

            Page.Dialog += (_, dialog) => dialog.AcceptAsync();
            await Page.RunAndWaitForDistributionList(datasetId, async () =>
            {
                await Page.ClickOnTableButton(0, "Odstrániť");
            });

            await Page.AssertNoTable();
        }

        [TestMethod]
        public async Task TestCreateMaximalDistributionWithLanguageOverride()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDistributionCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);
            await Page.RunAndWaitForDistributionCreate(datasetId, async () =>
            {
                await Page.GetByText("Nová distribúcia").ClickAsync();
            });

            DcatDistribution input = CreateMaximalDistribution(includeLanguages: true);
            await Page.FillDistributionFields(input);

            await Page.RunAndWaitForDistributionList(datasetId, async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, Extensions.GetLastEntity(storage, FileType.DistributionRegistration)!);
        }

        [TestMethod]
        public async Task TestEditMaximalDistributionWithLanguageOverride()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDistributionCodelists();
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            DcatDistribution distribution = CreateMaximalDistribution();
            Guid id = fixture.CreateDistribution(datasetId, PublisherId, distribution);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDistributionsAdmin(0);

            await Page.RunAndWaitForDistributionEdit(id, datasetId, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.AssertDistributionForm(distribution);

            DcatDistribution input = DcatDistribution.Create(datasetId);
            input.SetTermsOfUse(
                new Uri("https://data.gov.sk/def/authors-work-type/1"),
                new Uri("https://data.gov.sk/def/original-database-type/1"),
                new Uri("https://data.gov.sk/def/codelist/database-creator-special-rights-type/2"),
                new Uri("https://data.gov.sk/def/personal-data-occurence-type/1")
            );
            input.DownloadUrl = new Uri("http://example.com/download/other");
            input.AccessUrl = input.DownloadUrl;
            input.Format = new Uri("http://publications.europa.eu/resource/dataset/file-type/2");
            input.MediaType = new Uri("http://www.iana.org/assignments/media-types/text/xml");
            input.CompressFormat = new Uri("http://www.iana.org/assignments/media-types/application/rar");
            input.PackageFormat = input.CompressFormat;
            input.ConformsTo = new Uri("http://example.com/conforms/other");
            input.SetTitle(new Dictionary<string, string> { { "sk", "TestSkOther" }, { "en", "TestEnOther" }, { "de", "TestDeOther" } });

            await Page.FillDistributionFields(input);

            await Page.RunAndWaitForDistributionList(datasetId, async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DistributionRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, storage.GetFileState(id, accessPolicy)!);
        }
    }
}
