using CMS.Applications;
using CMS.Comments;
using CMS.Datasets;
using CMS.Likes;
using CMS.Suggestions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NkodSk.Abstractions;
using Piranha;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace CMS.Test
{
    public class AppTests
    {
        public const string CommunityUser = "CommunityUser";

        public const string Publisher = "Publisher";

        public const string PublisherAdmin = "PublisherAdmin";

        public const string Superadmin = "Superadmin";

        public static IEnumerable<object?[]> GetExplicitRoles()
        {
            yield return new object?[] { CommunityUser };
            yield return new object?[] { Publisher };
            yield return new object?[] { PublisherAdmin };
            yield return new object?[] { Superadmin };
        }

        public static IEnumerable<object?[]> GetRolesWithAnonymous()
        {
            foreach (object?[] role in GetExplicitRoles())
            {
                yield return role;
            }
            yield return new object?[] { null };
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task GetAllIsEmptyByDefault(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role));
            }

            using HttpResponseMessage response = await client.GetAsync("/cms/applications");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            ApplicationSearchResponse? r = await response.Content.ReadFromJsonAsync<ApplicationSearchResponse>();
            Assert.NotNull(r);
            Assert.Empty(r.Items);
            Assert.Null(r.PaginationMetadata);
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task GetAllHasOneItem(string role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role));
            }

            using IApi api = f.CreateApi();
            ApplicationPost post = await api.CreateApplication();

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/applications");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            ApplicationSearchResponse? r = await response.Content.ReadFromJsonAsync<ApplicationSearchResponse>();
            Assert.NotNull(r);
            Assert.Single(r.Items);
            Assert.Null(r.PaginationMetadata);
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task GetAllHasFirstPage(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role));
            }

            using IApi api = f.CreateApi();

            for (int i = 0; i < 100; i++)
            {
                await api.CreateApplication();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/applications?pageNumber=0&pageSize=10");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            ApplicationSearchResponse? r = await response.Content.ReadFromJsonAsync<ApplicationSearchResponse>();
            Assert.NotNull(r);
            Assert.Equal(10, r.Items.Count());
            Assert.NotNull(r.PaginationMetadata);
            Assert.Equal(10, r.PaginationMetadata.PageSize);
            Assert.Equal(0, r.PaginationMetadata.CurrentPage);
            Assert.Equal(100, r.PaginationMetadata.TotalItemCount);
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task GetAllHasSecondPage(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role));
            }

            using IApi api = f.CreateApi();

            for (int i = 0; i < 100; i++)
            {
                await api.CreateApplication();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/applications?pageNumber=1&pageSize=10");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            ApplicationSearchResponse? r = await response.Content.ReadFromJsonAsync<ApplicationSearchResponse>();
            Assert.NotNull(r);
            Assert.Equal(10, r.Items.Count());
            Assert.NotNull(r.PaginationMetadata);
            Assert.Equal(10, r.PaginationMetadata.PageSize);
            Assert.Equal(1, r.PaginationMetadata.CurrentPage);
            Assert.Equal(100, r.PaginationMetadata.TotalItemCount);
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task SearchIsEmptyByDefault(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role));
            }

            ApplicationSearchRequest request = new ApplicationSearchRequest
            {

            };
            using HttpResponseMessage response = await client.PostAsync("/cms/applications/search", JsonContent.Create(request));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            ApplicationSearchResponse? r = await response.Content.ReadFromJsonAsync<ApplicationSearchResponse>();
            Assert.NotNull(r);
            Assert.Empty(r.Items);
            Assert.Null(r.PaginationMetadata);
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task SearchHasOneItem(string role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role));
            }

            using IApi api = f.CreateApi();
            ApplicationPost post = await api.CreateApplication();

            using QueryExecutionListener listener = new QueryExecutionListener();

            ApplicationSearchRequest request = new ApplicationSearchRequest
            {

            };
            using HttpResponseMessage response = await client.PostAsync("/cms/applications/search", JsonContent.Create(request));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            ApplicationSearchResponse? r = await response.Content.ReadFromJsonAsync<ApplicationSearchResponse>();
            Assert.NotNull(r);
            Assert.Single(r.Items);
            Assert.Null(r.PaginationMetadata);
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task SearchHasFirstPage(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role));
            }

            using IApi api = f.CreateApi();

            for (int i = 0; i < 100; i++)
            {
                await api.CreateApplication();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            ApplicationSearchRequest request = new ApplicationSearchRequest
            {
                PageNumber = 0,
                PageSize = 10
            };
            using HttpResponseMessage response = await client.PostAsync("/cms/applications/search", JsonContent.Create(request));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            ApplicationSearchResponse? r = await response.Content.ReadFromJsonAsync<ApplicationSearchResponse>();
            Assert.NotNull(r);
            Assert.Equal(10, r.Items.Count());
            Assert.NotNull(r.PaginationMetadata);
            Assert.Equal(10, r.PaginationMetadata.PageSize);
            Assert.Equal(0, r.PaginationMetadata.CurrentPage);
            Assert.Equal(100, r.PaginationMetadata.TotalItemCount);
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task SearchHasSecondPage(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role));
            }

            using IApi api = f.CreateApi();

            for (int i = 0; i < 100; i++)
            {
                await api.CreateApplication();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            ApplicationSearchRequest request = new ApplicationSearchRequest
            {
                PageNumber = 1,
                PageSize = 10
            };
            using HttpResponseMessage response = await client.PostAsync("/cms/applications/search", JsonContent.Create(request));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            ApplicationSearchResponse? r = await response.Content.ReadFromJsonAsync<ApplicationSearchResponse>();
            Assert.NotNull(r);
            Assert.Equal(10, r.Items.Count());
            Assert.NotNull(r.PaginationMetadata);
            Assert.Equal(10, r.PaginationMetadata.PageSize);
            Assert.Equal(1, r.PaginationMetadata.CurrentPage);
            Assert.Equal(100, r.PaginationMetadata.TotalItemCount);
        }





























        private ApplicationDto CreateInput(Guid userId)
        {
            return new ApplicationDto
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = ApplicationTypes.V,
                Theme = ApplicationThemes.CU,
                Description = "Test Description",
                Title = "Test Title",
                ContactName = "Name2",
                ContactSurname = "Surname2",
                ContactEmail = "test-contact-2@example.com",
                Url = "https://example.com/some-application2",
                Created = DateTime.MinValue,
                Updated = DateTime.MinValue,
                CommentCount = 100,
                LikeCount = 200
            };
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task CreateNewApplication(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string publisher = "http://example.com/publisher";
            string userFormattedName = "Meno Priezvisko";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userFormattedName: userFormattedName));
            }

            using IApi api = f.CreateApi();
            await api.CreateApplication();

            using QueryExecutionListener listener = new QueryExecutionListener();

            ApplicationDto post = CreateInput(userId);
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));

            if (role is not null)
            {
                if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
                {
                    Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
                }

                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                Guid id = await response.Content.ReadFromJsonAsync<Guid>();
                Assert.NotEqual(Guid.Empty, id);

                ApplicationPost? created = await api.FindOneApplication(id);
                Assert.NotNull(created);
                Assert.Equal(post.Title, created.Title);
                Assert.NotNull(created.Application);
                Assert.Equal(post.UserId.ToString(), created.Application.UserId.Value);
                Assert.NotNull(created.Application.UserEmail.Value);
                Assert.Equal(userFormattedName, created.Application.UserFormattedName.Value);
                Assert.Equal(post.Type, created.Application.Type.Value);
                Assert.Equal(post.Theme, created.Application.Theme.Value);
                Assert.Equal(post.Url, created.Application.Url.Value);
                Assert.Equal(post.Logo, created.Application.Logo.Value);
                Assert.Equal(post.LogoFileName, created.Application.LogoFileName.Value);
                Assert.Equal(post.DatasetURIs, created.Application.DatasetURIs.Value);
                Assert.Equal(post.Description, created.Application.Description.Value);
                Assert.Equal(post.ContactName, created.Application.ContactName.Value);
                Assert.Equal(post.ContactSurname, created.Application.ContactSurname.Value);
                Assert.Equal(post.ContactEmail, created.Application.ContactEmail.Value);
                Assert.True((DateTime.UtcNow - created.Published!.Value).Duration().TotalMinutes < 1);
                Assert.Null(created.Application.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task ApplicationCanNotBeCreatedForOtherUser(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
            }

            using IApi api = f.CreateApi();
            await api.CreateApplication();

            ApplicationDto post = CreateInput(Guid.NewGuid());
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));

            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task CreateNewApplicationShouldNotBeEnabledWithInvalidToken(string role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail, key: RandomNumberGenerator.GetBytes(32)));
            }

            using IApi api = f.CreateApi();
            await api.CreateApplication();

            ApplicationDto post = CreateInput(userId);
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task TitleShouldBeRequired(string role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
            }

            using IApi api = f.CreateApi();
            await api.CreateApplication();

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(userId);
            post.Title = null;
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task TitleShouldNotBeEmpty(string role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
            }

            using IApi api = f.CreateApi();
            await api.CreateApplication();

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(userId);
            post.Title = string.Empty;
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task TitleShouldNotBeWhitespace(string role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
            }

            using IApi api = f.CreateApi();
            await api.CreateApplication();

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(userId);
            post.Title = " ";
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
        }


        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task DescriptionShouldBeRequired(string role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
            }

            using IApi api = f.CreateApi();
            await api.CreateApplication();

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(userId);
            post.Description = null;
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task DescriptionShouldNotBeEmpty(string role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
            }

            using IApi api = f.CreateApi();
            await api.CreateApplication();

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(userId);
            post.Description = string.Empty;
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task DescriptionShouldNotBeWhitespace(string role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
            }

            using IApi api = f.CreateApi();
            await api.CreateApplication();

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(userId);
            post.Description = " ";
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
        }

        [Fact]
        public async Task ApplicationShouldBeUpdatedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(userId, userEmail);

            int beforeCount = (await api.GetAllApplications()).Count();
            using QueryExecutionListener listener = new QueryExecutionListener();

            ApplicationDto post = CreateInput(userId);
            using HttpResponseMessage response = await client.PutAsync($"/cms/applications/{original.Id}", JsonContent.Create(post));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());

            ApplicationPost? updated = await api.FindOneApplication(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Application);
            Assert.Equal(userId.ToString("D"), updated.Application.UserId.Value);
            Assert.Equal(userEmail, updated.Application.UserEmail.Value);
            Assert.Equal(post.Description, updated.Application.Description.Value);
            Assert.Equal(post.Type, updated.Application.Type.Value);
            Assert.Equal(post.Theme, updated.Application.Theme.Value);
            Assert.Equal(post.Url, updated.Application.Url.Value);
            Assert.Equal(post.Logo, updated.Application.Logo.Value);
            Assert.Equal(post.LogoFileName, updated.Application.LogoFileName.Value);
            Assert.Equal(post.DatasetURIs, updated.Application.DatasetURIs.Value);
            Assert.Equal(post.ContactName, updated.Application.ContactName.Value);
            Assert.Equal(post.ContactSurname, updated.Application.ContactSurname.Value);
            Assert.Equal(post.ContactEmail, updated.Application.ContactEmail.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Application.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Application.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task OtherUserApplicationShouldNotBeUpdatedForCommunity()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "other@test.sk");

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(userId);
            using HttpResponseMessage response = await client.PutAsync($"/cms/applications/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task ApplicationShouldNotBeUpdatedForCommunityUserToOtherUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(userId, userEmail);

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(Guid.NewGuid());
            using HttpResponseMessage response = await client.PutAsync($"/cms/applications/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());

            ApplicationPost? updated = await api.FindOneApplication(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(userId.ToString("D"), updated.Application.UserId.Value);
            Assert.Equal(userEmail, updated.Application.UserEmail.Value);
        }

        [Fact]
        public async Task ApplicationShouldBeUpdatedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(userId, userEmail);

            int beforeCount = (await api.GetAllApplications()).Count();
            using QueryExecutionListener listener = new QueryExecutionListener();

            ApplicationDto post = CreateInput(userId);
            using HttpResponseMessage response = await client.PutAsync($"/cms/applications/{original.Id}", JsonContent.Create(post));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());

            ApplicationPost? updated = await api.FindOneApplication(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Application);
            Assert.Equal(userId.ToString("D"), updated.Application.UserId.Value);
            Assert.Equal(userEmail, updated.Application.UserEmail.Value);
            Assert.Equal(post.Description, updated.Application.Description.Value);
            Assert.Equal(post.Type, updated.Application.Type.Value);
            Assert.Equal(post.Theme, updated.Application.Theme.Value);
            Assert.Equal(post.Url, updated.Application.Url.Value);
            Assert.Equal(post.Logo, updated.Application.Logo.Value);
            Assert.Equal(post.LogoFileName, updated.Application.LogoFileName.Value);
            Assert.Equal(post.DatasetURIs, updated.Application.DatasetURIs.Value);
            Assert.Equal(post.ContactName, updated.Application.ContactName.Value);
            Assert.Equal(post.ContactSurname, updated.Application.ContactSurname.Value);
            Assert.Equal(post.ContactEmail, updated.Application.ContactEmail.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Application.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Application.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task OtherUserApplicationShouldNotBeUpdatedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "other@test.sk");

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(userId);
            using HttpResponseMessage response = await client.PutAsync($"/cms/applications/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task ApplicationShouldNotBeUpdatedToOtherUserForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(userId, userEmail);

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(Guid.NewGuid());
            using HttpResponseMessage response = await client.PutAsync($"/cms/applications/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());

            ApplicationPost? updated = await api.FindOneApplication(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(userId.ToString("D"), updated.Application.UserId.Value);
            Assert.Equal(userEmail, updated.Application.UserEmail.Value);
        }

        [Fact]
        public async Task ApplicationShouldBeUpdatedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(userId, userEmail);

            int beforeCount = (await api.GetAllApplications()).Count();
            using QueryExecutionListener listener = new QueryExecutionListener();

            ApplicationDto post = CreateInput(userId);
            using HttpResponseMessage response = await client.PutAsync($"/cms/applications/{original.Id}", JsonContent.Create(post));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());

            ApplicationPost? updated = await api.FindOneApplication(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Application);
            Assert.Equal(userId.ToString("D"), updated.Application.UserId.Value);
            Assert.Equal(userEmail, updated.Application.UserEmail.Value);
            Assert.Equal(post.Description, updated.Application.Description.Value);
            Assert.Equal(post.Type, updated.Application.Type.Value);
            Assert.Equal(post.Theme, updated.Application.Theme.Value);
            Assert.Equal(post.Url, updated.Application.Url.Value);
            Assert.Equal(post.Logo, updated.Application.Logo.Value);
            Assert.Equal(post.LogoFileName, updated.Application.LogoFileName.Value);
            Assert.Equal(post.DatasetURIs, updated.Application.DatasetURIs.Value);
            Assert.Equal(post.ContactName, updated.Application.ContactName.Value);
            Assert.Equal(post.ContactSurname, updated.Application.ContactSurname.Value);
            Assert.Equal(post.ContactEmail, updated.Application.ContactEmail.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Application.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Application.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task OtherUserApplicationShouldNotBeUpdatedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "other@test.sk");

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(userId);
            using HttpResponseMessage response = await client.PutAsync($"/cms/applications/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task ApplicationShouldNotBeUpdatedToOtherUserForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(userId, userEmail);

            int beforeCount = (await api.GetAllApplications()).Count();

            ApplicationDto post = CreateInput(Guid.NewGuid());
            using HttpResponseMessage response = await client.PutAsync($"/cms/applications/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());

            ApplicationPost? updated = await api.FindOneApplication(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(userId.ToString("D"), updated.Application.UserId.Value);
            Assert.Equal(userEmail, updated.Application.UserEmail.Value);
        }

        [Fact]
        public async Task ApplicationShouldBeDeletedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(userId, userEmail);

            int beforeCount = (await api.GetAllApplications()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/applications/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllApplications()).Count());

            Assert.Null(await api.FindOneApplication(original.Id));
        }

        [Fact]
        public async Task OtherUserApplicationShouldNotBeDeletedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllApplications()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/applications/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);

            Assert.NotNull(await api.FindOneApplication(original.Id));
        }

        [Fact]
        public async Task ApplicationShouldBeDeletedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(userId, userEmail);

            int beforeCount = (await api.GetAllApplications()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/applications/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllApplications()).Count());

            Assert.Null(await api.FindOneApplication(original.Id));
        }

        [Fact]
        public async Task OtherUserApplicationShouldNotBeDeletedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllApplications()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/applications/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);

            Assert.NotNull(await api.FindOneApplication(original.Id));
        }

        [Fact]
        public async Task ApplicationShouldBeDeletedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(userId, userEmail);

            int beforeCount = (await api.GetAllApplications()).Count();


            using HttpResponseMessage response = await client.DeleteAsync($"/cms/applications/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllApplications()).Count());

            Assert.Null(await api.FindOneApplication(original.Id));
        }

        [Fact]
        public async Task OtherUserApplicationShouldNotBeDeletedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllApplications()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/applications/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllApplications()).Count());
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);

            Assert.NotNull(await api.FindOneApplication(original.Id));
        }

        [Fact]
        public async Task ApplicationShouldBeDeletedForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllApplications()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/applications/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllApplications()).Count());

            Assert.Null(await api.FindOneApplication(original.Id));
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldBeAdded(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, userId: userId.ToString(), userEmail: userEmail));
            }

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "test2@test.sk");

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/applications/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Application.Updated.Value, updated.Application.Updated.Value);
                Assert.Equal(new[] { userId }, updated.Application.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Empty(updated.Application.Likes.Value);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldBeRemoved(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, userId: userId.ToString(), userEmail: userEmail));
            }

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "test2@test.sk", likeUsers: new[] { userId });

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/applications/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Application.Updated.Value, updated.Application.Updated.Value);
                Assert.Empty(updated.Application.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { userId }, updated.Application.Likes.Value);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldBeAddedWithOther(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, userId: userId.ToString(), userEmail: userEmail));
            }

            List<Guid> otherUsers = new List<Guid>();
            for (int i = 0; i < 100; i++)
            {
                otherUsers.Add(Guid.NewGuid());
            }

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "test2@test.sk", likeUsers: otherUsers);

            QueryExecutionListener listener = new QueryExecutionListener();
            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/applications/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Application.Updated.Value, updated.Application.Updated.Value);
                Assert.Equal(otherUsers.Union(new[] { userId }), updated.Application.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers, updated.Application.Likes.Value);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldBeRemovedWithOther(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, userId: userId.ToString(), userEmail: userEmail));
            }

            List<Guid> otherUsers = new List<Guid>();
            for (int i = 0; i < 100; i++)
            {
                otherUsers.Add(Guid.NewGuid());
            }

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "test2@test.sk", likeUsers: otherUsers.Union(new[] { userId }));

            QueryExecutionListener listener = new QueryExecutionListener();
            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/applications/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Application.Updated.Value, updated.Application.Updated.Value);
                Assert.Equal(otherUsers, updated.Application.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers.Union(new[] { userId }), updated.Application.Likes.Value);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldNotBeAddedForOtherUser(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, userId: userId.ToString(), userEmail: userEmail));
            }

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "test2@test.sk");

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = Guid.NewGuid()
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/applications/likes", JsonContent.Create(input));
            if (role is not null)
            {
                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Application.Updated.Value, updated.Application.Updated.Value);
                Assert.Empty(updated.Application.Likes.Value);

                Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Empty(updated.Application.Likes.Value);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldNotBeRemovedForOtherUser(string? role)
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, userId: userId.ToString(), userEmail: userEmail));
            }

            Guid otherUserId = Guid.NewGuid();

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(Guid.NewGuid(), "test2@test.sk", likeUsers: new[] { otherUserId });

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = otherUserId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/applications/likes", JsonContent.Create(input));
            if (role is not null)
            {
                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Application.Updated.Value, updated.Application.Updated.Value);
                Assert.Equal(new[] { otherUserId }, updated.Application.Likes.Value);

                Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                ApplicationPost? updated = await api.FindOneApplication(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { otherUserId }, updated.Application.Likes.Value);
            }
        }

        [Fact]
        public async Task AuthorShouldBeNotifiedOnComment()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(assignedUserId, assignedUserEmail);

            CommentDto post = new CommentDto
            {
                Id = Guid.NewGuid(),
                ContentId = original.Id,
                UserId = userId,
                Body = "Test body input",
                Created = DateTime.MinValue,
                ParentId = Guid.Empty
            };
            using HttpResponseMessage response = await client.PostAsync("/cms/comments", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Single(f.TestNotificationService.Notifications);
            Assert.Equal("test-contact@example.com", f.TestNotificationService.Notifications[0].Item1);
        }

        [Fact]
        public async Task AuthorShouldNotBeNotifiedAfterDelete()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(assignedUserId, assignedUserEmail);

            CommentDto post = new CommentDto
            {
                Id = Guid.NewGuid(),
                ContentId = original.Id,
                UserId = userId,
                Body = "Test body input",
                Created = DateTime.MinValue,
                ParentId = Guid.Empty
            };
            using HttpResponseMessage response = await client.PostAsync("/cms/comments", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            using HttpResponseMessage response2 = await client.DeleteAsync($"/cms/applications/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);

            Assert.Empty(f.TestNotificationService.Notifications);
        }

        [Fact]
        public async Task NotificationShouldBeCanceledOnCommentDelete()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            ApplicationPost original = await api.CreateApplication(assignedUserId, assignedUserEmail);

            Guid otherUser = Guid.NewGuid();
            string otherUserEmail = "other@test.sk";

            await api.CreateComment(otherUser, otherUserEmail, original.Id);

            CommentDto post = new CommentDto
            {
                Id = Guid.NewGuid(),
                ContentId = original.Id,
                UserId = userId,
                Body = "Test body input",
                Created = DateTime.MinValue,
                ParentId = Guid.Empty
            };
            using HttpResponseMessage response = await client.PostAsync("/cms/comments", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Guid id = await response.Content.ReadFromJsonAsync<Guid>();

            using HttpResponseMessage response2 = await client.DeleteAsync($"/cms/comments/{id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);

            Assert.Empty(f.TestNotificationService.Notifications);
        }

        [Fact]
        public async Task PublisherShouldBeNotifiedOnCreate()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";
            string publisherEmail = "publisher@example.com";

            FoafAgent agent = FoafAgent.Create(new Uri(publisher));
            agent.EmailAddress = publisherEmail;
            f.FileStorage.InsertFile(agent.ToString(), agent.UpdateMetadata() with { IsPublic = true }, false, new AllAccessFilePolicy());

            DcatDataset dataset = DcatDataset.Create();
            dataset.Publisher = new Uri(publisher);
            f.FileStorage.InsertFile(dataset.ToString(), dataset.UpdateMetadata(true, agent), false, new AllAccessFilePolicy());

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();

            ApplicationDto post = CreateInput(userId);
            post.DatasetURIs = new List<string> { dataset.Uri.OriginalString };
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Single(f.TestNotificationService.Notifications);
            Assert.Equal(publisherEmail, f.TestNotificationService.Notifications[0].Item1);
        }

        [Fact]
        public async Task NotificationSholdBeCancelledOnDelete()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";
            string publisherEmail = "publisher@example.com";

            FoafAgent agent = FoafAgent.Create(new Uri(publisher));
            agent.EmailAddress = publisherEmail;
            f.FileStorage.InsertFile(agent.ToString(), agent.UpdateMetadata() with { IsPublic = true }, false, new AllAccessFilePolicy());

            DcatDataset dataset = DcatDataset.Create();
            dataset.Publisher = new Uri(publisher);
            f.FileStorage.InsertFile(dataset.ToString(), dataset.UpdateMetadata(true, agent), false, new AllAccessFilePolicy());

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();

            ApplicationDto post = CreateInput(userId);
            post.DatasetURIs = new List<string> { dataset.Uri.OriginalString };
            using HttpResponseMessage response = await client.PostAsync("/cms/applications", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Guid id = await response.Content.ReadFromJsonAsync<Guid>();

            Assert.Single(f.TestNotificationService.Notifications);
            Assert.Equal(publisherEmail, f.TestNotificationService.Notifications[0].Item1);

            using HttpResponseMessage response2 = await client.DeleteAsync($"/cms/applications/{id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);

            Assert.Empty(f.TestNotificationService.Notifications);
        }
    }
}
