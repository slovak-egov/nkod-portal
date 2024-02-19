using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestBase;
using static System.Net.Mime.MediaTypeNames;

namespace Frontend.Test
{
    [TestClass]
    public class ProfileTests : PageTest
    {
        private StorageFixture fixture = new StorageFixture();

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherFileAccessPolicy(PublisherId);

        [TestMethod]
        public async Task ProfileShouldBeEmptyByDefault()
        {
            string path = fixture.GetStoragePath();

            FoafAgent agent = FoafAgent.Create(new Uri(PublisherId));
            agent.SetNames(new Dictionary<string, string> { { "sk", "Test" } });
            fixture.CreatePublisher(agent, true);
            
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "PublisherAdmin");

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.OpenMenu("Správa", "Profil");
            }, new List<string> { "codelists" });
            await Page.WaitForURLAsync("http://localhost:6001/sprava/profil");

            Assert.AreEqual(string.Empty, await (await Page.GetInputInFormElementGroup("Adresa webového sídla")).GetAttributeAsync("value"));
            Assert.AreEqual(string.Empty, await (await Page.GetInputInFormElementGroup("E-mailová adresa kontaktnej osoby")).GetAttributeAsync("value"));
            Assert.AreEqual(string.Empty, await (await Page.GetInputInFormElementGroup("Telefónne číslo kontaktnej osoby")).GetAttributeAsync("value"));
        }

        [TestMethod]
        public async Task ProfileShouldBeLoaded()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            FoafAgent agent = FoafAgent.Create(new Uri(PublisherId));
            agent.SetNames(new Dictionary<string, string> { { "sk", "Test" } });
            agent.HomePage = new Uri("http://example.com/");
            agent.EmailAddress = "test@example.com";
            agent.Phone = "+421 123 456 789";
            agent.LegalForm = new Uri("https://data.gov.sk/def/legal-form-type/321");
            fixture.CreatePublisher(agent, true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "PublisherAdmin");

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.OpenMenu("Správa", "Profil");
            }, new List<string> { "codelists" });
            await Page.WaitForURLAsync("http://localhost:6001/sprava/profil");

            Assert.AreEqual("http://example.com/", await (await Page.GetInputInFormElementGroup("Adresa webového sídla")).GetAttributeAsync("value"));
            Assert.AreEqual("test@example.com", await (await Page.GetInputInFormElementGroup("E-mailová adresa kontaktnej osoby")).GetAttributeAsync("value"));
            Assert.AreEqual("+421 123 456 789", await (await Page.GetInputInFormElementGroup("Telefónne číslo kontaktnej osoby")).GetAttributeAsync("value"));

            Assert.AreEqual(agent.LegalForm?.ToString(), await Page.GetSelectItemFormElementGroup("Právna forma"));
        }

        [TestMethod]
        public async Task ProfileShouldBeSaved()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            FoafAgent agent = FoafAgent.Create(new Uri(PublisherId));
            agent.SetNames(new Dictionary<string, string> { { "sk", "Test" } });
            agent.HomePage = new Uri("http://example.com");
            agent.EmailAddress = "test@example.com";
            agent.Phone = "+421 123 456 789";
            agent.LegalForm = new Uri("https://data.gov.sk/def/legal-form-type/321");
            Guid id = fixture.CreatePublisher(agent, true);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "PublisherAdmin");

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.OpenMenu("Správa", "Profil");
            }, new List<string> { "codelists" });
            await Page.WaitForURLAsync("http://localhost:6001/sprava/profil");

            await (await Page.GetInputInFormElementGroup("Adresa webového sídla")).FillAsync("http://example.com/updated");
            await (await Page.GetInputInFormElementGroup("E-mailová adresa kontaktnej osoby")).FillAsync("other@example.com");
            await (await Page.GetInputInFormElementGroup("Telefónne číslo kontaktnej osoby")).FillAsync("+421 987 654 321");

            await (await Page.GetSelectInFormElementGroup("Právna forma"))!.SelectOptionAsync("https://data.gov.sk/def/legal-form-type/331");

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.GetByText("Uložiť").ClickAsync();
            }, new List<string> { "profile", "user-info" });

            await Page.OpenLocalCatalogsAdmin();
            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.OpenMenu("Správa", "Profil");
            }, new List<string> { "codelists" });
            await Page.WaitForURLAsync("http://localhost:6001/sprava/profil");

            Assert.AreEqual("http://example.com/updated", await (await Page.GetInputInFormElementGroup("Adresa webového sídla")).GetAttributeAsync("value"));
            Assert.AreEqual("other@example.com", await (await Page.GetInputInFormElementGroup("E-mailová adresa kontaktnej osoby")).GetAttributeAsync("value"));
            Assert.AreEqual("+421 987 654 321", await (await Page.GetInputInFormElementGroup("Telefónne číslo kontaktnej osoby")).GetAttributeAsync("value"));

            Assert.AreEqual("https://data.gov.sk/def/legal-form-type/331", await Page.GetSelectItemFormElementGroup("Právna forma"));

            FileState? state = storage.GetFileState(id, accessPolicy);
            Assert.IsNotNull(state);
            FoafAgent updatedAgent = FoafAgent.Parse(state.Content!)!;

            Assert.AreEqual("http://example.com/updated", updatedAgent.HomePage?.ToString());
            Assert.AreEqual("other@example.com", updatedAgent.EmailAddress);
            Assert.AreEqual("+421 987 654 321", updatedAgent.Phone);
            Assert.AreEqual("https://data.gov.sk/def/legal-form-type/331", updatedAgent.LegalForm?.ToString());
        }

        [TestMethod]
        public async Task RegistrationShouldBeCreated()
        {
            string path = fixture.GetStoragePath();
            fixture.CreatePublisherCodelists();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "PublisherAdmin", "Test Company", false, false);

            await Page.WaitForURLAsync("http://localhost:6001/registracia");

            Assert.AreEqual(string.Empty, await (await Page.GetInputInFormElementGroup("Adresa webového sídla")).GetAttributeAsync("value"));
            Assert.AreEqual(string.Empty, await (await Page.GetInputInFormElementGroup("E-mailová adresa kontaktnej osoby")).GetAttributeAsync("value"));
            Assert.AreEqual(string.Empty, await (await Page.GetInputInFormElementGroup("Telefónne číslo kontaktnej osoby")).GetAttributeAsync("value"));
            Assert.AreEqual("https://data.gov.sk/def/legal-form-type/321", await Page.GetSelectItemFormElementGroup("Právna forma"));

            await (await Page.GetInputInFormElementGroup("Adresa webového sídla")).FillAsync("http://example.com/updated");
            await (await Page.GetInputInFormElementGroup("E-mailová adresa kontaktnej osoby")).FillAsync("other@example.com");
            await (await Page.GetInputInFormElementGroup("Telefónne číslo kontaktnej osoby")).FillAsync("+421 987 654 321");

            await (await Page.GetSelectInFormElementGroup("Právna forma"))!.SelectOptionAsync("https://data.gov.sk/def/legal-form-type/331");

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.GetByText("Registrovať").ClickAsync();
            }, new List<string> { "registration" });

            FileState? state = ((IFileStorage)storage).GetPublisherState(PublisherId, accessPolicy);
            Assert.IsNotNull(state);
            Assert.IsFalse(state.Metadata.IsPublic);

            FoafAgent updatedAgent = FoafAgent.Parse(state.Content!)!;
            Assert.AreEqual("Test Company", updatedAgent.GetName("sk"));
            Assert.AreEqual("http://example.com/updated", updatedAgent.HomePage?.ToString());
            Assert.AreEqual("other@example.com", updatedAgent.EmailAddress);
            Assert.AreEqual("+421 987 654 321", updatedAgent.Phone);
            Assert.AreEqual("https://data.gov.sk/def/legal-form-type/331", updatedAgent.LegalForm?.ToString());
        }
    }
}
