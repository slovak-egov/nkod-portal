using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
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
    public class UserAdministrationTests : PageTest
    {
        private StorageFixture fixture = new StorageFixture();

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherFileAccessPolicy(PublisherId);

        [TestMethod]
        public async Task TableShouldHaveOneUserByDefault()
        {
            string path = fixture.GetStoragePath();
            
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "PublisherAdmin");

            await Page.OpenUsersAdmin();

            await Page.AssertTableRowsCount(1);
        }

        [TestMethod]
        public async Task TestNavigateToNewRecord()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "PublisherAdmin");

            await Page.OpenUsersAdmin();
            await Page.RunAndWaitForUserCreate(async () =>
            {
                await Page.GetByText("Nový používateľ").ClickAsync();
            });

            await Page.AssertNewUserForm(new NewUserInput
            {
                FirstName = string.Empty,
                LastName = string.Empty,
                Email = string.Empty,
                Role = "Publisher",
            });
        }

        private static NewUserInput CreatelUserInput()
        {
            return new NewUserInput
            {
                Email = "test2@example.com",
                FirstName = "Meno",
                LastName = "Priezvisko",
                Role = "Publisher",
            };
        }

        [TestMethod]
        public async Task TestCreateUser()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);
            await Page.Login(f, PublisherId, "PublisherAdmin");

            await Page.OpenUsersAdmin();
            await Page.RunAndWaitForUserCreate(async () =>
            {
                await Page.GetByText("Nový používateľ").ClickAsync();
            });

            NewUserInput input = CreatelUserInput();
            await Page.FillUserFields(input);

            await Page.RunAndWaitForUserList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(2);

            UserInfoResult users = await f.Services.GetRequiredService<TestIdentityAccessManagementClient>().GetUsers(new UserInfoQuery(), PublisherId);
            Extensions.AssertAreEqual(input, users.Items.Single(u => u.Email == input.Email));
        }

        [TestMethod]
        public async Task TestEditRecordWithoutChange()
        {
            string path = fixture.GetStoragePath();
                        
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);

            NewUserInput newUser = CreatelUserInput();
            UserSaveResult saveResult = f.Services.GetRequiredService<TestIdentityAccessManagementClient>().CreateUser(newUser, PublisherId);

            await Page.Login(f, PublisherId, "PublisherAdmin");

            await Page.OpenUsersAdmin();
            await Page.RunAndWaitForUserEdit(saveResult.Id!, async () =>
            {
                await Page.ClickOnTableButton(1, "Upraviť");
            });

            await Page.RunAndWaitForUserList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(2);

            Extensions.AssertAreEqual(newUser, f.Services.GetRequiredService<TestIdentityAccessManagementClient>().GetUser(PublisherId, saveResult.Id!)!);
        }

        [TestMethod]
        public async Task TestEditRecordMinimal()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);

            NewUserInput newUser = CreatelUserInput();
            UserSaveResult saveResult = f.Services.GetRequiredService<TestIdentityAccessManagementClient>().CreateUser(newUser, PublisherId);

            await Page.Login(f, PublisherId, "PublisherAdmin");

            await Page.OpenUsersAdmin();
            await Page.RunAndWaitForUserEdit(saveResult.Id!, async () =>
            {
                await Page.ClickOnTableButton(1, "Upraviť");
            });

            await Page.AssertEditUserForm(newUser);

            EditUserInput input = new EditUserInput
            {
                Id = saveResult.Id!,
                Email = "new@example.com",
                FirstName = "Meno2",
                LastName = "Priezvisko2",
                Role = "PublisherAdmin",
            };

            await Page.FillUserFields(input);

            await Page.RunAndWaitForUserList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(2);

            Extensions.AssertAreEqual(input, f.Services.GetRequiredService<TestIdentityAccessManagementClient>().GetUser(PublisherId, saveResult.Id!)!);
        }

        [TestMethod]
        public async Task TestEditRecordMinimalToNoneRole()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);

            NewUserInput newUser = CreatelUserInput();
            UserSaveResult saveResult = f.Services.GetRequiredService<TestIdentityAccessManagementClient>().CreateUser(newUser, PublisherId);

            await Page.Login(f, PublisherId, "PublisherAdmin");

            await Page.OpenUsersAdmin();
            await Page.RunAndWaitForUserEdit(saveResult.Id!, async () =>
            {
                await Page.ClickOnTableButton(1, "Upraviť");
            });

            await Page.AssertEditUserForm(newUser);

            EditUserInput input = new EditUserInput
            {
                Id = saveResult.Id!,
                Email = "new@example.com",
                FirstName = "Meno2",
                LastName = "Priezvisko2",
                Role = null,
            };

            await Page.FillUserFields(input);

            await Page.RunAndWaitForUserList(async () =>
            {
                await Page.GetByText("Uložiť", new PageGetByTextOptions { Exact = true }).ClickAsync();
            });

            await Page.AssertTableRowsCount(2);

            Extensions.AssertAreEqual(input, f.Services.GetRequiredService<TestIdentityAccessManagementClient>().GetUser(PublisherId, saveResult.Id!)!);
        }

        [TestMethod]
        public async Task TestRemoveRecord()
        {
            string path = fixture.GetStoragePath();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory f = new WebApiApplicationFactory(storage);

            NewUserInput newUser = CreatelUserInput();
            UserSaveResult saveResult = f.Services.GetRequiredService<TestIdentityAccessManagementClient>().CreateUser(newUser, PublisherId);

            await Page.Login(f, PublisherId, "PublisherAdmin");

            await Page.OpenUsersAdmin();

            Page.Dialog += (_, dialog) => dialog.AcceptAsync();
            await Page.RunAndWaitForUserList(async () =>
            {
                await Page.ClickOnTableButton(1, "Odstrániť");
            });

            await Page.AssertTableRowsCount(1);
            Assert.IsNull(f.Services.GetRequiredService<TestIdentityAccessManagementClient>().GetUser(PublisherId, saveResult.Id!));
        }
    }
}
