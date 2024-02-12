using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright.MSTest;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestBase;
using Microsoft.Playwright;
using System.Data;
using static System.Net.Mime.MediaTypeNames;

namespace Frontend.Test
{
    [TestClass]
    public class DatasetAdministrationTests : PageTest
    {
        private StorageFixture fixture = new StorageFixture();

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherFileAccessPolicy(PublisherId);

        [TestMethod]
        public async Task TableShouldBeEmptyByDefault()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDataset("Test", PublisherId + "!");

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();

            await Page.AssertNoTable();
        }

        [TestMethod]
        public async Task TableShouldHaveOneRow()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();

            await Page.AssertTableRowsCount(1);
        }

        [TestMethod]
        public async Task TestNavigateToNewRecord()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDatasetCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetCreate(async () =>
            {
                await Page.GetByText("Nový dataset").ClickAsync();
            });

            DcatDataset dataset = DcatDataset.Create();
            dataset.AccrualPeriodicity = new Uri("http://publications.europa.eu/resource/dataset/frequency/1");
            dataset.SetTitle(new Dictionary<string, string> { { "sk", string.Empty } });
            dataset.SetDescription(new Dictionary<string, string> { { "sk", string.Empty } });
            dataset.SetContactPoint(new LanguageDependedTexts { { "sk", string.Empty } }, string.Empty);

            await Page.AssertDatasetForm(dataset);
        }

        private DcatDataset CreateMinimalDataset(int? index = null)
        {
            DcatDataset input = DcatDataset.Create();
            input.Publisher = new Uri(PublisherId);
            input.SetTitle(new Dictionary<string, string> { { "sk", "NameSk" } });
            input.SetDescription(new Dictionary<string, string> { { "sk", "DescriptionSk" } });
            input.AccrualPeriodicity = new Uri("http://publications.europa.eu/resource/dataset/frequency/1");
            input.Themes = new[] { new Uri("http://publications.europa.eu/resource/dataset/data-theme/1") };
            input.SetKeywords(new Dictionary<string, List<string>> { { "sk", new List<string> { "keyword1" } } });
            input.SetContactPoint(new LanguageDependedTexts { { "sk", string.Empty } }, string.Empty);
            input.ShouldBePublic = true;
            return input;
        }

        private DcatDataset CreateMaximalDataset(int? index = null, bool includeLanguages = false)
        {
            DcatDataset input = CreateMinimalDataset(index);
            input.Spatial = new[] { new Uri("http://publications.europa.eu/resource/dataset/country/1") };
            input.SetTemporal(new DateOnly(2023, 8, 20), new DateOnly(2023, 9, 12));
            input.SetContactPoint(new LanguageDependedTexts(new Dictionary<string, string> { { "sk", "ContactSk" } }), "test@example.com");
            input.LandingPage = new Uri("http://example.com/documentation");
            input.Specification = new Uri("http://example.com/specification");
            input.Themes = new[] { new Uri("http://publications.europa.eu/resource/dataset/data-theme/1"), new Uri(DcatDataset.EuroVocPrefix + "6409"), new Uri(DcatDataset.EuroVocPrefix + "6410") };
            input.SetEuroVocLabelThemes(new Dictionary<string, List<string>> { 
                { "sk", new List<string> { "nepovolená likvidácia odpadu", "chemický odpad" } },
                { "en", new List<string> { "unauthorised dumping", "chemical waste" } }
            });
            input.SetKeywords(new Dictionary<string, List<string>> { { "sk", new List<string> { "keyword1", "keyword2" } } });
            input.SpatialResolutionInMeters = 10;
            input.TemporalResolution = "1d";

            if (includeLanguages)
            {
                Dictionary<string, string> values = input.Title;
                values["en"] = "NameEn";
                values["de"] = "NameDe";
                input.SetTitle(values);

                values = input.Description;
                values["en"] = "DescriptionEn";
                values["de"] = "DescriptionDe";
                input.SetDescription(values);

                values = input.ContactPoint!.Name;
                values["en"] = "ContactEn";
                values["de"] = "ContactDe";
                input.ContactPoint!.SetNames(values);

                Dictionary<string, List<string>> keywords = input.Keywords;
                keywords["en"] = new List<string> { "keyword3", "keyword4" };
                keywords["de"] = new List<string> { "keyword5", "keyword6" };
                input.SetKeywords(keywords);
            }

            return input;
        }

        [TestMethod]
        public async Task TestCreateMinimalDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetCreate(async () =>
            {
                await Page.GetByText("Nový dataset").ClickAsync();
            });

            DcatDataset input = CreateMinimalDataset();
            await Page.FillDatasetFields(input);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť dataset", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, false, Extensions.GetLastEntity(storage, FileType.DatasetRegistration)!);
        }

        [TestMethod]
        public async Task TestCreateMaximalDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetCreate(async () =>
            {
                await Page.GetByText("Nový dataset").ClickAsync();
            });

            DcatDataset input = CreateMaximalDataset();
            await Page.FillDatasetFields(input);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť dataset", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, false, Extensions.GetLastEntity(storage, FileType.DatasetRegistration)!);
        }

        [TestMethod]
        public async Task TestCreateMaximalDatasetWithDistribution()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetCreate(async () =>
            {
                await Page.GetByText("Nový dataset").ClickAsync();
            });

            DcatDataset input = CreateMaximalDataset();
            await Page.FillDatasetFields(input);

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.GetByText("Uložiť dataset a pridať distribúciu", new PageGetByTextOptions { Exact = true }).ClickAsync();
            }, new List<string> { "codelists" });

            FileState state = Extensions.GetLastEntity(storage, FileType.DatasetRegistration)!;
            Extensions.AssertAreEqual(input, false, state);

            await Page.WaitForURLAsync($"http://localhost:6001/sprava/distribucie/{state.Metadata.Id}/pridat");
        }

        [TestMethod]
        public async Task TestCreateMinimalDatasetAsSerie()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetCreate(async () =>
            {
                await Page.GetByText("Nový dataset").ClickAsync();
            });

            DcatDataset input = CreateMinimalDataset();
            input.IsSerie = true;
            await Page.FillDatasetFields(input);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť dataset", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, true, Extensions.GetLastEntity(storage, FileType.DatasetRegistration)!);
        }

        [TestMethod]
        public async Task TestCreateMinimalDatasetAsPartOfSerie()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset dataset = CreateMinimalDataset(1);
            dataset.SetTitle(new Dictionary<string, string> { { "sk", "Test1" } });
            fixture.CreateDataset(dataset);

            dataset = CreateMinimalDataset(2);
            dataset.SetTitle(new Dictionary<string, string> { { "sk", "Test2" } });
            dataset.IsSerie = true;
            Guid serieId = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetCreate(async () =>
            {
                await Page.GetByText("Nový dataset").ClickAsync();
            });

            DcatDataset input = CreateMinimalDataset();
            input.IsPartOf = new Uri($"https://data.gov.sk/set/{serieId}");
            input.IsPartOfInternalId = serieId.ToString();
            await Page.FillDatasetFields(input);

            IElementHandle? partOfGroup = await Page.GetSelectInFormElementGroup("Nadradený dataset");
            Assert.IsNotNull(partOfGroup);
            CollectionAssert.AreEquivalent(new[] { serieId.ToString() }, await Extensions.GetSelectOptions(partOfGroup));

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť dataset", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(3);

            Extensions.AssertAreEqual(input, false, Extensions.GetLastEntity(storage, FileType.DatasetRegistration)!);
        }

        [TestMethod]
        public async Task TestEditRecordMinimalWithoutChange()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset dataset = CreateMinimalDataset();
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(dataset, false, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMaximalWithoutChange()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset dataset = CreateMaximalDataset();
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(dataset, false, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMinimal()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset dataset = CreateMinimalDataset();
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.AssertDatasetForm(dataset);

            DcatDataset input = DcatDataset.Create();
            input.Publisher = new Uri(PublisherId);
            input.SetTitle(new Dictionary<string, string> { { "sk", "NameSk2" } });
            input.SetDescription(new Dictionary<string, string> { { "sk", "DescriptionSk2" } });
            input.AccrualPeriodicity = new Uri("http://publications.europa.eu/resource/dataset/frequency/2");
            input.Themes = new[] { new Uri("http://publications.europa.eu/resource/dataset/data-theme/2") };
            input.SetKeywords(new Dictionary<string, List<string>> { { "sk", new List<string> { "keyword2" } } });
            input.ShouldBePublic = false;

            await Page.FillDatasetFields(input);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, false, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMaximal()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset dataset = CreateMaximalDataset();
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();

            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.AssertDatasetForm(dataset);

            DcatDataset input = DcatDataset.Create();
            input.Publisher = new Uri(PublisherId);
            input.SetTitle(new Dictionary<string, string> { { "sk", "NameSk2" } });
            input.SetDescription(new Dictionary<string, string> { { "sk", "DescriptionSk2" } });
            input.AccrualPeriodicity = new Uri("http://publications.europa.eu/resource/dataset/frequency/2");
            input.ShouldBePublic = false;

            input.Spatial = new[] { new Uri("http://publications.europa.eu/resource/dataset/country/2") };
            input.SetTemporal(new DateOnly(2023, 8, 10), new DateOnly(2023, 9, 17));
            input.SetContactPoint(new LanguageDependedTexts(new Dictionary<string, string> { { "sk", "ContactSk2" } }), "test2@example.com");
            input.LandingPage = new Uri("http://example.com/documentation2");
            input.Specification = new Uri("http://example.com/specification2");
            input.Themes = new[] { new Uri("http://publications.europa.eu/resource/dataset/data-theme/2"), new Uri(DcatDataset.EuroVocPrefix + "6411") };
            input.SetEuroVocLabelThemes(new Dictionary<string, List<string>> { 
                { "sk", new List<string> { "elektronický odpad" } },
                { "en", new List<string> { "electronic waste" } },
            });
            input.SetKeywords(new Dictionary<string, List<string>> { { "sk", new List<string> { "keyword3", "keyword4" } } });
            input.SpatialResolutionInMeters = 20;
            input.TemporalResolution = "2d";

            await Page.FillDatasetFields(input);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, false, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestRemoveRecord()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();

            Page.Dialog += (_, dialog) => dialog.AcceptAsync();
            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.ClickOnTableButton(0, "Odstrániť");
            });

            await Page.AssertNoTable();
        }

        [TestMethod]
        public async Task TestEditRecordAsSerieMinimalWithoutChange()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset dataset = CreateMinimalDataset();
            dataset.IsSerie = true;
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(dataset, true, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordAsPartOfSerieMinimalWithoutChange()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset serie = CreateMinimalDataset(2);
            serie.SetTitle(new Dictionary<string, string> { { "sk", "Test2" } });
            serie.IsSerie = true;
            Guid serieId = fixture.CreateDataset(serie);

            DcatDataset dataset = CreateMinimalDataset();
            dataset.IsPartOf = new Uri($"https://data.gov.sk/set/{serieId}");
            dataset.IsPartOfInternalId = serieId.ToString();
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(2);

            Assert.AreEqual(2, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(dataset, false, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordAsSerieMinimalToSeparate()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset dataset = CreateMinimalDataset();
            dataset.IsSerie = true;
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            dataset.IsSerie = false;
            await Page.FillDatasetFields(dataset);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(dataset, false, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordAsPartOfSerieMinimalToSeparate()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset serie = CreateMinimalDataset(2);
            serie.SetTitle(new Dictionary<string, string> { { "sk", "Test2" } });
            serie.IsSerie = true;
            Guid serieId = fixture.CreateDataset(serie);

            DcatDataset dataset = CreateMinimalDataset();
            dataset.IsPartOf = new Uri("http://example.com/dataset/2");
            dataset.IsPartOfInternalId = serieId.ToString();
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            dataset.IsPartOf = null;
            dataset.IsPartOfInternalId = null;
            await Page.FillDatasetFields(dataset);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(2);

            Assert.AreEqual(2, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(dataset, false, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMinimalToSerie()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset dataset = CreateMinimalDataset();
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            dataset.IsSerie = true;
            await Page.FillDatasetFields(dataset);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(dataset, true, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestEditRecordMinimalToPartOfSerie()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset serie = CreateMinimalDataset(2);
            serie.SetTitle(new Dictionary<string, string> { { "sk", "Test2" } });
            serie.IsSerie = true;
            Guid serieId = fixture.CreateDataset(serie);

            DcatDataset dataset = CreateMinimalDataset();
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            dataset.IsPartOf = new Uri($"https://data.gov.sk/set/{serieId}");
            dataset.IsPartOfInternalId = serieId.ToString();
            await Page.FillDatasetFields(dataset);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(2);

            Assert.AreEqual(2, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(dataset, false, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task RefreshTokenSholdNotDestroyChanges()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset dataset = CreateMinimalDataset();
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            f.TestIdentityAccessManagementClient.RefreshTokenAfter = TimeSpan.FromSeconds(15);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.AssertDatasetForm(dataset);

            DcatDataset input = DcatDataset.Create();
            input.Publisher = new Uri(PublisherId);
            input.SetTitle(new Dictionary<string, string> { { "sk", "NameSk2" } });
            input.SetDescription(new Dictionary<string, string> { { "sk", "DescriptionSk2" } });
            input.SetContactPoint(new LanguageDependedTexts(new Dictionary<string, string> { { "sk", string.Empty } }), string.Empty);
            input.AccrualPeriodicity = new Uri("http://publications.europa.eu/resource/dataset/frequency/2");
            input.Themes = new[] { new Uri("http://publications.europa.eu/resource/dataset/data-theme/2") };
            input.SetKeywords(new Dictionary<string, List<string>> { { "sk", new List<string> { "keyword2" } } });
            input.ShouldBePublic = false;

            await Page.FillDatasetFields(input);
            await Page.AssertDatasetForm(input);

            await Page.WaitForRefershToken();

            await Page.AssertDatasetForm(input);
        }

        [TestMethod]
        public async Task TestCreateMaximalDatasetWithLanguageOverride()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetCreate(async () =>
            {
                await Page.GetByText("Nový dataset").ClickAsync();
            });

            DcatDataset input = CreateMaximalDataset(includeLanguages: true);
            await Page.FillDatasetFields(input);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť dataset", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Extensions.AssertAreEqual(input, false, Extensions.GetLastEntity(storage, FileType.DatasetRegistration)!);
        }

        [TestMethod]
        public async Task TestEditMaximalDatasetWithLanguageOverride()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            DcatDataset dataset = CreateMaximalDataset();
            Guid id = fixture.CreateDataset(dataset);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();

            await Page.RunAndWaitForDatasetEdit(id, async () =>
            {
                await Page.ClickOnTableButton(0, "Upraviť");
            });

            await Page.AssertDatasetForm(dataset);

            DcatDataset input = DcatDataset.Create();
            input.Publisher = new Uri(PublisherId);
            input.SetTitle(new Dictionary<string, string> { { "sk", "NameSk2" }, { "en", "NameEn2" }, { "de", "NameDe2" } });
            input.SetDescription(new Dictionary<string, string> { { "sk", "DescriptionSk2" }, { "en", "DescriptionEn2" }, { "de", "DescriptionDe2" } });
            input.AccrualPeriodicity = new Uri("http://publications.europa.eu/resource/dataset/frequency/2");
            input.ShouldBePublic = false;

            input.Spatial = new[] { new Uri("http://publications.europa.eu/resource/dataset/country/2") };
            input.SetTemporal(new DateOnly(2023, 8, 10), new DateOnly(2023, 9, 17));
            input.SetContactPoint(new LanguageDependedTexts(new Dictionary<string, string> { { "sk", "ContactSk2" }, { "en", "ContactEn2" }, { "de", "ContactDe2" } }), "test2@example.com");
            input.LandingPage = new Uri("http://example.com/documentation2");
            input.Specification = new Uri("http://example.com/specification2");
            input.Themes = new[] { new Uri("http://publications.europa.eu/resource/dataset/data-theme/2"), new Uri(DcatDataset.EuroVocPrefix + "6411") };
            input.SetEuroVocLabelThemes(new Dictionary<string, List<string>> {
                { "sk", new List<string> { "elektronický odpad" } },
                { "en", new List<string> { "electronic waste" } },
            });
            input.SetKeywords(new Dictionary<string, List<string>> { { "sk", new List<string> { "keywordnew1", "keywordnew2" } }, { "en", new List<string> { "keywordnew3", "keywordnew4" } }, { "de", new List<string> { "keywordnew5", "keywordnew6" } } });
            input.SpatialResolutionInMeters = 20;
            input.TemporalResolution = "2d";

            await Page.FillDatasetFields(input);

            await Page.RunAndWaitForDatasetList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration } }, accessPolicy).TotalCount);
            Extensions.AssertAreEqual(input, false, storage.GetFileState(id, accessPolicy)!);
        }

        [TestMethod]
        public async Task TestRemoveRecordAsSerie()
        {
            string path = fixture.GetStoragePath();

            Guid parentId = fixture.CreateDataset("Test 1", PublisherId);
            Guid childId = fixture.CreateDataset("Test 2", PublisherId);

            using Storage storage = new Storage(path);

            FileState parentState = storage.GetFileState(parentId, accessPolicy)!;
            DcatDataset parentDataset = DcatDataset.Parse(parentState.Content!)!;
            parentDataset.IsSerie = true;
            parentDataset.Modified = new DateTimeOffset(2023, 8, 10, 0, 0, 0, TimeSpan.Zero);
            storage.InsertFile(parentDataset.ToString(), parentDataset.UpdateMetadata(true, parentState.Metadata), true, accessPolicy);

            FileState partState = storage.GetFileState(childId, accessPolicy)!;
            DcatDataset partDataset = DcatDataset.Parse(partState.Content!)!;
            partDataset.IsPartOf = parentDataset.Uri;
            partDataset.IsPartOfInternalId = parentState.Metadata.Id.ToString();
            partDataset.Modified = new DateTimeOffset(2023, 8, 11, 0, 0, 0, TimeSpan.Zero);
            storage.InsertFile(partDataset.ToString(), partDataset.UpdateMetadata(true, partState.Metadata), true, accessPolicy);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();

            Page.Dialog += (_, dialog) => dialog.AcceptAsync();
            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.ClickOnTableButton(1, "Odstrániť");
            }, new List<string> { "datasets" });


            await Page.AssertTableRowsCount(2);

            CollectionAssert.AreEquivalent(new[] { "Dátovú sériu nie je možné zmazať, najskôr prosím zmažte všetky datasety z tejto série." }, (await Page.GetAlerts()).ToArray());
        }

        [TestMethod]
        public async Task TestDistributionsShouldBeVisible()
        {
            string path = fixture.GetStoragePath();

            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(1, await Page.GetByText("Zmeniť distribúcie").CountAsync());
        }

        [TestMethod]
        public async Task TestDistributionsShouldNotBeVisibleWithSerie()
        {
            string path = fixture.GetStoragePath();

            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);

            FileState state = storage.GetFileState(datasetId, accessPolicy)!;
            DcatDataset dataset = DcatDataset.Parse(state.Content!)!;
            dataset.IsSerie = true;
            storage.InsertFile(dataset.ToString(), dataset.UpdateMetadata(true, state.Metadata), true, accessPolicy);

            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();

            await Page.AssertTableRowsCount(1);

            Assert.AreEqual(0, await Page.GetByText("Zmeniť distribúcie").CountAsync());
        }

        [TestMethod]
        public async Task TestSaveAndAddDitributionsShouldBeNotVisibleOnSerie()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "Publisher");

            await Page.OpenDatasetsAdmin();
            await Page.RunAndWaitForDatasetCreate(async () =>
            {
                await Page.GetByText("Nový dataset").ClickAsync();
            });

            Assert.AreEqual(1, await Page.GetByText("Uložiť dataset a pridať distribúciu").CountAsync());

            await Page.CheckDatasetSerieRadio("Dataset je séria");

            Assert.AreEqual(0, await Page.GetByText("Uložiť dataset a pridať distribúciu").CountAsync());

            await Page.CheckDatasetSerieRadio("Samostatný dataset");

            Assert.AreEqual(1, await Page.GetByText("Uložiť dataset a pridať distribúciu").CountAsync());
        }
    }
}
