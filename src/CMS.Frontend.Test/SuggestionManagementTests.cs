using Microsoft.Playwright.MSTest;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Frontend.Test;
using Microsoft.Playwright;
using Piranha;
using CMS.Suggestions;
using CMS.Test;
using Microsoft.AspNetCore.Http.HttpResults;
using NkodSk.Abstractions;

namespace CMS.Frontend.Test
{
    [TestClass]
    public class SuggestionManagementTests : PageTest
    {
        private async Task AssertForm(string publisher, string? dataset, string title, string description, string type)
        {
            Assert.AreEqual(publisher, await (await Page.GetInputInFormElementGroup("Organizácii")).GetAttributeAsync("value"));
            Assert.AreEqual(title, await (await Page.GetInputInFormElementGroup("Názov podnetu")).GetAttributeAsync("value") ?? string.Empty);
            Assert.AreEqual(description, await (await Page.GetTextareaInFormElementGroup("Popis podnetu")).TextContentAsync());            
            Assert.AreEqual(type, await (await (await Page.GetSelectInFormElementGroup("Typ podnetu"))!.QuerySelectorAsync("option:checked"))!.TextContentAsync());

            IElementHandle? datasetSelect = await Page.GetFormElementGroup("Dataset");
            if (dataset is not null)
            {
                Assert.AreEqual(dataset, await (await datasetSelect!.QuerySelectorAsync("input"))!.GetAttributeAsync("value"));
            }
            else
            {
                Assert.IsNull(dataset);
            }
        }

        private async Task FillAutocomplete(string label, string query, string value)
        {
            IElementHandle? group = await Page.GetFormElementGroup(label);
            Assert.IsNotNull(group);

            IElementHandle? input = await group.QuerySelectorAsync("input[type=text]");
            Assert.IsNotNull(input);

            await input.ClickAsync();

            await input.FillAsync(query);

            foreach (IElementHandle option in await group.QuerySelectorAllAsync("[role=option]"))
            {
                if (await option.TextContentAsync() == value)
                {
                    await option.ClickAsync();
                }
            }
        }

        private async Task FillForm(string publisher, string? dataset, string title, string description, string type)
        {
            await FillAutocomplete("Organizácii", string.Empty, publisher);

            await (await Page.GetInputInFormElementGroup("Názov podnetu")).FillAsync(title);
            await (await Page.GetTextareaInFormElementGroup("Popis podnetu")).FillAsync(description);

            await (await Page.GetSelectInFormElementGroup("Typ podnetu"))!.SelectOptionAsync(new SelectOptionValue { Label = type });

            if (dataset is not null)
            {
                await FillAutocomplete("Dataset", string.Empty, dataset);
            }
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

        public static IEnumerable<object?[]> SuggestionTypes
        {
            get
            {
                yield return new object?[] { ContentTypes.PN, "podnet na zverejnenie nového datasetu/distribúcie" };
                yield return new object?[] { ContentTypes.DQ, "podnet na kvalitu dát zverejneného datasetu/distribúcie" };
                yield return new object?[] { ContentTypes.MQ, "podnet na kvalitu metadát zverejneného datasetu/distribúcie" };
                yield return new object?[] { ContentTypes.O, "iný podnet" };
            }
        }

        private static IEnumerable<object?[]> TestTypes
        {
            get
            {
                foreach (object?[] v1 in ExplicitRoles)
                {
                    foreach (object?[] v2 in SuggestionTypes)
                    {
                        yield return v1.Union(v2).ToArray();
                    }
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(TestTypes))]
        public async Task SuggestionShouldBeCreated(string role, ContentTypes contentType, string type)
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

            await Page.OpenSuggestions();

            await Page.RunAndWaitForRequests(async () => {
                await Page.GetByText("Nový podnet", new PageGetByTextOptions { Exact = true }).ClickAsync();
            }, new List<string> { "publishers/search" });

            await AssertForm(string.Empty, null, string.Empty, string.Empty, "podnet na zverejnenie nového datasetu/distribúcie");

            string? datasetTitle = contentType != ContentTypes.PN ? "Dataset" : null;
            string? datasetUri = contentType != ContentTypes.PN ? $"https://data.gov.sk/set/{d1}" : null;

            await FillForm("Zyx", datasetTitle, "Test Title", "Test Description", type);

            await Page.RunAndWaitForRequests(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            }, new List<string> { "/cms/suggestions" });

            using IApi api = f.ApiApplicationFactory.CreateApi();
            List<SuggestionPost> posts = (await api.GetAllSuggestions()).ToList();
            Assert.AreEqual(1, posts.Count);

            PersistentUserInfo userInfo = f.TestIdentityAccessManagementClient.GetUserByEmail("test@example.com")!;

            SuggestionPost created = posts[0];

            Assert.AreEqual("Test Title", created.Title);
            Assert.IsNotNull(created.Suggestion);
            Assert.AreEqual(userInfo.Id, created.Suggestion.UserId.Value);
            Assert.AreEqual($"{userInfo.FirstName} {userInfo.LastName}", created.Suggestion.UserFormattedName.Value);
            //Assert.AreEqual(publisher, created.Suggestion.UserOrgUri.Value);
            Assert.AreEqual(publisher1, created.Suggestion.OrgToUri.Value);
            Assert.AreEqual(contentType, created.Suggestion.Type.Value);
            Assert.AreEqual(datasetUri, created.Suggestion.DatasetUri.Value);
            Assert.AreEqual("Test Description", created.Suggestion.Description.Value);
            Assert.AreEqual(SuggestionStates.C, created.Suggestion.Status.Value);
            Assert.IsTrue((DateTime.UtcNow - created.Published!.Value).Duration().TotalMinutes < 1);
            Assert.IsNull(created.Suggestion.Likes.Value);
        }
    }
}
