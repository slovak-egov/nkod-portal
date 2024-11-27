using Microsoft.Playwright.MSTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Frontend.Test;
using Microsoft.Playwright;
using NkodSk.RdfFileStorage;
using Piranha;
using CMS.Applications;
using CMS.Test;
using NkodSk.Abstractions;

namespace CMS.Frontend.Test
{
    [TestClass]
    public class AppManagementTests : PageTest
    {
        private async Task AssertForm(string title, string description)
        {
            Assert.AreEqual(title, await (await Page.GetInputInFormElementGroup("Názov")).GetAttributeAsync("value") ?? string.Empty);
            Assert.AreEqual(description, await (await Page.GetTextareaInFormElementGroup("Popis")).TextContentAsync());
        }

        private async Task FillForm(string title, string description)
        {
            await (await Page.GetInputInFormElementGroup("Názov")).FillAsync(title);
            await (await Page.GetTextareaInFormElementGroup("Popis")).FillAsync(description);
        }

        public const string CommunityUser = "CommunityUser";

        public const string Publisher = "Publisher";

        public const string PublisherAdmin = "PublisherAdmin";

        public const string Superadmin = "Superadmin";

        public static IEnumerable<object?[]> ExplicitRoles
        {
            get
            {
                yield return new object?[] { CommunityUser };
                yield return new object?[] { Publisher };
                yield return new object?[] { PublisherAdmin };
                yield return new object?[] { Superadmin };
            }
        }

        private static IEnumerable<object?[]> TestTypes
        {
            get
            {
                foreach (object?[] v1 in ExplicitRoles)
                {
                    yield return v1;
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(TestTypes))]
        public async Task SuggestionShouldBeCreated(string role)
        {
            using StorageFixture fixture = new StorageFixture();
            string path = fixture.GetStoragePath();

            string publisher1 = "https://example.com/publisher1";
            string publisher2 = "https://example.com/publisher2";

            string? publisher = (role == Publisher || role == PublisherAdmin) ? "https://example.com/publisher" : null;

            fixture.CreatePublisher(publisher1, true, "Zyx");
            fixture.CreatePublisher(publisher2, true, "Abc");

            if (publisher is not null)
            {
                fixture.CreatePublisher(publisher, true, "Org");
            }

            Guid d1 = fixture.CreateDataset("Dataset", publisher1);
            Guid d2 = fixture.CreateDataset("Bilancia", publisher1);
            Guid d3 = fixture.CreateDataset("Doprava", publisher2);

            using Storage storage = new Storage(path);
            using CompositeWebApplicationFactory f = new CompositeWebApplicationFactory(storage);
            await Page.Login(f, role, publisher);

            await Page.OpenApplications();

            await Page.GetByText("Nová aplikácia", new PageGetByTextOptions { Exact = true }).ClickAsync();

            await AssertForm(string.Empty, string.Empty);

            string? datasetTitle = "Dataset";
            string? datasetUri = $"https://data.gov.sk/set/{d1}";

            await FillForm("Test Title", "Test Description");

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            }, new List<string> { "/cms/applications" });

            using IApi api = f.ApiApplicationFactory.CreateApi();
            List<ApplicationPost> posts = (await api.GetAllApplications()).ToList();
            Assert.AreEqual(1, posts.Count);

            PersistentUserInfo userInfo = f.TestIdentityAccessManagementClient.GetUserByEmail("test@example.com")!;

            ApplicationPost created = posts[0];

            Assert.AreEqual("Test Title", created.Title);
            Assert.IsNotNull(created.Application);
            Assert.AreEqual(userInfo.Id, created.Application.UserId.Value);
            Assert.AreEqual($"{userInfo.FirstName} {userInfo.LastName}", created.Application.UserFormattedName.Value);
            Assert.AreEqual("Test Description", created.Application.Description.Value);
            Assert.IsTrue((DateTime.UtcNow - created.Published!.Value).Duration().TotalMinutes < 1);
        }
    }
}
