using CMS.Likes;
using CMS.Suggestions;
using Markdig.Extensions.Yaml;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Internal;
using Piranha;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace CMS.Test
{
    public class SuggestionTests
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

            using HttpResponseMessage response = await client.GetAsync("/cms/suggestions");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            SuggestionSearchResponse? r = await response.Content.ReadFromJsonAsync<SuggestionSearchResponse>();
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
            SuggestionPost post = await api.CreateSuggestion();

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/suggestions");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            SuggestionSearchResponse? r = await response.Content.ReadFromJsonAsync<SuggestionSearchResponse>();
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
                await api.CreateSuggestion(refresh: false);
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/suggestions?pageNumber=0&pageSize=10");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            SuggestionSearchResponse? r = await response.Content.ReadFromJsonAsync<SuggestionSearchResponse>();
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
                await api.CreateSuggestion();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/suggestions?pageNumber=1&pageSize=10");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            SuggestionSearchResponse? r = await response.Content.ReadFromJsonAsync<SuggestionSearchResponse>();
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

            SuggestionSearchRequest request = new SuggestionSearchRequest
            {

            };
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions/search", JsonContent.Create(request));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            SuggestionSearchResponse? r = await response.Content.ReadFromJsonAsync<SuggestionSearchResponse>();
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
            SuggestionPost post = await api.CreateSuggestion();

            using QueryExecutionListener listener = new QueryExecutionListener();

            SuggestionSearchRequest request = new SuggestionSearchRequest
            {

            };
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions/search", JsonContent.Create(request));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            SuggestionSearchResponse? r = await response.Content.ReadFromJsonAsync<SuggestionSearchResponse>();
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
                await api.CreateSuggestion();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            SuggestionSearchRequest request = new SuggestionSearchRequest
            {
                PageNumber = 0,
                PageSize = 10
            };
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions/search", JsonContent.Create(request));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            SuggestionSearchResponse? r = await response.Content.ReadFromJsonAsync<SuggestionSearchResponse>();
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
                await api.CreateSuggestion();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            SuggestionSearchRequest request = new SuggestionSearchRequest
            {
                PageNumber = 1,
                PageSize = 10
            };
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions/search", JsonContent.Create(request));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            SuggestionSearchResponse? r = await response.Content.ReadFromJsonAsync<SuggestionSearchResponse>();
            Assert.NotNull(r);
            Assert.Equal(10, r.Items.Count());
            Assert.NotNull(r.PaginationMetadata);
            Assert.Equal(10, r.PaginationMetadata.PageSize);
            Assert.Equal(1, r.PaginationMetadata.CurrentPage);
            Assert.Equal(100, r.PaginationMetadata.TotalItemCount);
        }

        private SuggestionDto CreateInput(Guid userId, string? publisher = null, string? targetPublisher = null)
        {
            return new SuggestionDto
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrgToUri = targetPublisher ?? "https://example.com/company",
                UserOrgUri = publisher,
                Type = ContentTypes.O,
                Status = SuggestionStates.C,
                Description = "Test Description",
                Title = "Test Title",
                DatasetUri = "https://example.com/dataset",
                Created = DateTime.MinValue,
                Updated = DateTime.MinValue,
                CommentCount = 100,
                LikeCount = 200
            };
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task CreateNewSuggestion(string? role)
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
            await api.CreateSuggestion();

            using QueryExecutionListener listener = new QueryExecutionListener();

            SuggestionDto post = CreateInput(userId, publisher);
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

            if (role is not null)
            {
                if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
                {
                    Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
                }

                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                Guid id = await response.Content.ReadFromJsonAsync<Guid>();
                Assert.NotEqual(Guid.Empty, id);

                SuggestionPost? created = await api.FindOneSuggestion(id);
                Assert.NotNull(created);
                Assert.Equal(post.Title, created.Title);
                Assert.NotNull(created.Suggestion);
                Assert.Equal(post.UserId.ToString(), created.Suggestion.UserId.Value);
                Assert.NotNull(created.Suggestion.UserEmail.Value);
                Assert.Equal(userFormattedName, created.Suggestion.UserFormattedName.Value);
                Assert.Equal(post.UserOrgUri, created.Suggestion.UserOrgUri.Value);
                Assert.Equal(post.OrgToUri, created.Suggestion.OrgToUri.Value);
                Assert.Equal(post.Type, created.Suggestion.Type.Value);
                Assert.Equal(post.DatasetUri, created.Suggestion.DatasetUri.Value);
                Assert.Equal(post.Description, created.Suggestion.Description.Value);
                Assert.Equal(post.Status, created.Suggestion.Status.Value);
                Assert.True((DateTime.UtcNow - created.Published!.Value).Duration().TotalMinutes < 1);
                Assert.Null(created.Suggestion.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task SuggestionCanNotBeCreatedForOtherUser(string? role)
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
            await api.CreateSuggestion();

            SuggestionDto post = CreateInput(Guid.NewGuid(), "http://example.com/company2");
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

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
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task SuggestionCanNotBeCreatedInPState(string? role)
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
            await api.CreateSuggestion();

            SuggestionDto post = CreateInput(userId, publisher);
            post.Status = SuggestionStates.P;
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                Guid id = await response.Content.ReadFromJsonAsync<Guid>();
                Assert.NotEqual(Guid.Empty, id);

                SuggestionPost? created = await api.FindOneSuggestion(id);
                Assert.NotNull(created);
                Assert.NotNull(created.Suggestion);
                Assert.NotEqual(post.Status, created.Suggestion.Status.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task SuggestionCanNotBeCreatedInRState(string? role)
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
            await api.CreateSuggestion();

            SuggestionDto post = CreateInput(userId, publisher);
            post.Status = SuggestionStates.R;
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                Guid id = await response.Content.ReadFromJsonAsync<Guid>();
                Assert.NotEqual(Guid.Empty, id);

                SuggestionPost? created = await api.FindOneSuggestion(id);
                Assert.NotNull(created?.Suggestion);
                Assert.NotEqual(post.Status, created.Suggestion.Status.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task CreateNewSuggestionShouldNotBeEnabledWithInvalidToken(string role)
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
            await api.CreateSuggestion();

            SuggestionDto post = CreateInput(userId, publisher);
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

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
            await api.CreateSuggestion();

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher);
            post.Title = null;
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
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
            await api.CreateSuggestion();

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher);
            post.Title = string.Empty;
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
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
            await api.CreateSuggestion();

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher);
            post.Title = " ";
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
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
            await api.CreateSuggestion();

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher);
            post.Description = null;
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
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
            await api.CreateSuggestion();

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher);
            post.Description = string.Empty;
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
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
            await api.CreateSuggestion();

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher);
            post.Description = " ";
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task OrgShouldNotBeEmpty(string role)
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
            await api.CreateSuggestion();

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher);
            post.OrgToUri = null;
            using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
        }

        //[Theory]
        //[MemberData(nameof(GetExplicitRoles))]
        //public async Task DatasetShouldBeRequiredForTypePN(string role)
        //{
        //    using ApiApplicationFactory f = new ApiApplicationFactory();
        //    using HttpClient client = f.CreateClient();
        //    Guid userId = Guid.NewGuid();
        //    string userEmail = "test@test.sk";
        //    string publisher = "http://example.com/publisher";

        //    if (role is not null)
        //    {
        //        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
        //    }

        //    using IApi api = f.CreateApi();
        //    await api.CreateSuggestion();

        //    int beforeCount = (await api.GetAllSuggestions()).Count();

        //    SuggestionDto post = CreateInput(userId, publisher);
        //    post.Type = ContentTypes.PN;
        //    post.DatasetUri = null;
        //    using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

        //    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        //    Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
        //}

        //[Theory]
        //[MemberData(nameof(GetExplicitRoles))]
        //public async Task DatasetShouldBeRequiredForTypeDQ(string role)
        //{
        //    using ApiApplicationFactory f = new ApiApplicationFactory();
        //    using HttpClient client = f.CreateClient();
        //    Guid userId = Guid.NewGuid();
        //    string userEmail = "test@test.sk";
        //    string publisher = "http://example.com/publisher";

        //    if (role is not null)
        //    {
        //        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
        //    }

        //    using IApi api = f.CreateApi();
        //    await api.CreateSuggestion();

        //    int beforeCount = (await api.GetAllSuggestions()).Count();

        //    SuggestionDto post = CreateInput(userId, publisher);
        //    post.Type = ContentTypes.DQ;
        //    post.DatasetUri = null;
        //    using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

        //    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        //    Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
        //}

        //[Theory]
        //[MemberData(nameof(GetExplicitRoles))]
        //public async Task DatasetShouldBeRequiredForTypeMQ(string role)
        //{
        //    using ApiApplicationFactory f = new ApiApplicationFactory();
        //    using HttpClient client = f.CreateClient();
        //    Guid userId = Guid.NewGuid();
        //    string userEmail = "test@test.sk";
        //    string publisher = "http://example.com/publisher";

        //    if (role is not null)
        //    {
        //        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
        //    }

        //    using IApi api = f.CreateApi();
        //    await api.CreateSuggestion();

        //    int beforeCount = (await api.GetAllSuggestions()).Count();

        //    SuggestionDto post = CreateInput(userId, publisher);
        //    post.Type = ContentTypes.DQ;
        //    post.DatasetUri = null;
        //    using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

        //    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        //    Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
        //}

        //[Theory]
        //[MemberData(nameof(GetExplicitRoles))]
        //public async Task DatasetShouldBeRequiredForTypeO(string role)
        //{
        //    using ApiApplicationFactory f = new ApiApplicationFactory();
        //    using HttpClient client = f.CreateClient();
        //    Guid userId = Guid.NewGuid();
        //    string userEmail = "test@test.sk";
        //    string publisher = "http://example.com/publisher";

        //    if (role is not null)
        //    {
        //        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
        //    }

        //    using IApi api = f.CreateApi();
        //    await api.CreateSuggestion();

        //    int beforeCount = (await api.GetAllSuggestions()).Count();

        //    SuggestionDto post = CreateInput(userId, publisher);
        //    post.Type = ContentTypes.O;
        //    post.DatasetUri = null;
        //    using HttpResponseMessage response = await client.PostAsync("/cms/suggestions", JsonContent.Create(post));

        //    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        //    Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
        //}

        [Fact]
        public async Task SuggestionInStateCShouldBeUpdatedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail);

            int beforeCount = (await api.GetAllSuggestions()).Count();
            using QueryExecutionListener listener = new QueryExecutionListener();

            SuggestionDto post = CreateInput(userId);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStateCShouldNotBeUpdatedToStatePForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail);

            int beforeCount = (await api.GetAllSuggestions()).Count();
            
            SuggestionDto post = CreateInput(userId);
            post.Status = SuggestionStates.P;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotEqual(post.Status, updated?.Suggestion.Status.Value);
        }

        [Fact]
        public async Task SuggestionInStateCShouldNotBeUpdatedToStateRForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId);
            post.Status = SuggestionStates.R;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotEqual(post.Status, updated?.Suggestion.Status.Value);
        }

        [Fact]
        public async Task SuggestionInStatePShouldNotBeUpdatedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, state: SuggestionStates.P);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId);
            post.Status = SuggestionStates.P;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            //TODO equals
        }

        [Fact]
        public async Task SuggestionInStateRShouldNotBeUpdatedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, state: SuggestionStates.R);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId);
            post.Status = SuggestionStates.R;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            //TODO equals

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OtherUserSuggestionShouldNotBeUpdatedForCommunity()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@test.sk");

            int beforeCount = (await api.GetAllSuggestions()).Count();
            
            SuggestionDto post = CreateInput(userId);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task SuggestionShouldNotBeUpdatedForCommunityUserToOtherUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.NewGuid());
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
        }

        [Fact]
        public async Task SuggestionInStateCShouldBeUpdatedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));
            
            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();
            using QueryExecutionListener listener = new QueryExecutionListener();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(publisher, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStateCShouldNotBeUpdatedToStatePForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            post.Status = SuggestionStates.P;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotEqual(post.Status, updated?.Suggestion.Status.Value);
        }

        [Fact]
        public async Task SuggestionInStateCShouldNotBeUpdatedToStateRForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            post.Status = SuggestionStates.R;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotEqual(post.Status, updated?.Suggestion.Status.Value);
        }

        [Fact]
        public async Task SuggestionInStatePShouldNotBeUpdatedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, SuggestionStates.P, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            post.Status = SuggestionStates.P;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            //TODO equals

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionInStateRShouldNotBeUpdatedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, SuggestionStates.R, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            post.Status = SuggestionStates.R;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            //TODO equals

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OtherUserSuggestionShouldNotBeUpdatedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@test.sk", targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task SuggestionShouldNotBeUpdatedToOtherUserForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.NewGuid(), null, targetPublisher);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
        }

        [Fact]
        public async Task SuggestionInStateCShouldBeUpdatedToStatePForPublisherWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = publisher;

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.Parse(original.Suggestion.UserId.Value), original.Suggestion.UserOrgUri.Value, targetPublisher);
            post.Status = SuggestionStates.P;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(original.Suggestion.OrgToUri.Value, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(original.Suggestion.Type.Value, updated.Suggestion.Type.Value);
            Assert.Equal(original.Suggestion.DatasetUri.Value, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(original.Suggestion.Description.Value, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);
        }

        [Fact]
        public async Task SuggestionInStateCShouldBeUpdatedToStateRForPublisherWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = publisher;

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.Parse(original.Suggestion.UserId.Value), original.Suggestion.UserOrgUri.Value, targetPublisher);
            post.Status = SuggestionStates.R;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(original.Suggestion.OrgToUri.Value, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(original.Suggestion.Type.Value, updated.Suggestion.Type.Value);
            Assert.Equal(original.Suggestion.DatasetUri.Value, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(original.Suggestion.Description.Value, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);
        }

        [Fact]
        public async Task SuggestionInStatePShouldBeUpdatedForPublisherWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = publisher;

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", state: SuggestionStates.P, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.Parse(original.Suggestion.UserId.Value), original.Suggestion.UserOrgUri.Value, targetPublisher);
            post.Status = SuggestionStates.P;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(original.Suggestion.OrgToUri.Value, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(original.Suggestion.Type.Value, updated.Suggestion.Type.Value);
            Assert.Equal(original.Suggestion.DatasetUri.Value, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(original.Suggestion.Description.Value, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO equals
        }

        [Fact]
        public async Task SuggestionInStateRShouldBeUpdatedForPublisherWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = publisher;

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", state: SuggestionStates.R, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.Parse(original.Suggestion.UserId.Value), original.Suggestion.UserOrgUri.Value, targetPublisher);
            post.Status = SuggestionStates.R;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(original.Suggestion.OrgToUri.Value, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(original.Suggestion.Type.Value, updated.Suggestion.Type.Value);
            Assert.Equal(original.Suggestion.DatasetUri.Value, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(original.Suggestion.Description.Value, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO equals
        }

        [Fact]
        public async Task SuggestionShouldNotBeUpdatedToOtherPublisherForPublisherWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", state: SuggestionStates.P, targetPublisher: publisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.Parse(original.Suggestion.UserId.Value), original.Suggestion.UserOrgUri.Value, "https://example.com/new-other-publisher");

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Suggestion.OrgToUri.Value, updated.Suggestion.OrgToUri.Value);
        }

        [Fact]
        public async Task SuggestionShouldNotBeUpdatedToOtherUserForPublisherWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = publisher;

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.NewGuid(), targetPublisher: targetPublisher);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
        }

        [Fact]
        public async Task SuggestionInStateCShouldBeUpdatedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();
            using QueryExecutionListener listener = new QueryExecutionListener();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(publisher, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStateCShouldNotBeUpdatedToStatePForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            post.Status = SuggestionStates.P;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotEqual(post.Status, updated?.Suggestion.Status.Value);
        }

        [Fact]
        public async Task SuggestionInStateCShouldNotBeUpdatedToStateRForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            post.Status = SuggestionStates.R;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotEqual(post.Status, updated?.Suggestion.Status.Value);
        }

        [Fact]
        public async Task SuggestionInStatePShouldNotBeUpdatedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, SuggestionStates.P, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            post.Status = SuggestionStates.P;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            //TODO equals

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionInStateRShouldNotBeUpdatedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, SuggestionStates.R, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            post.Status = SuggestionStates.R;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            //TODO equals

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OtherUserSuggestionShouldNotBeUpdatedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@test.sk", targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(userId, publisher, targetPublisher);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task SuggestionShouldNotBeUpdatedToOtherUserForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = "http://example.com/publisher2";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.NewGuid(), null, targetPublisher);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
        }

        [Fact]
        public async Task SuggestionInStateCShouldBeUpdatedToStatePForPublisherAdminWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = publisher;

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.Parse(original.Suggestion.UserId.Value), original.Suggestion.UserOrgUri.Value, targetPublisher);
            post.Status = SuggestionStates.P;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(original.Suggestion.OrgToUri.Value, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(original.Suggestion.Type.Value, updated.Suggestion.Type.Value);
            Assert.Equal(original.Suggestion.DatasetUri.Value, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(original.Suggestion.Description.Value, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);
        }

        [Fact]
        public async Task SuggestionInStateCShouldBeUpdatedToStateRForPublisherAdminWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = publisher;

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.Parse(original.Suggestion.UserId.Value), original.Suggestion.UserOrgUri.Value, targetPublisher);
            post.Status = SuggestionStates.R;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(original.Suggestion.OrgToUri.Value, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(original.Suggestion.Type.Value, updated.Suggestion.Type.Value);
            Assert.Equal(original.Suggestion.DatasetUri.Value, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(original.Suggestion.Description.Value, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);
        }

        [Fact]
        public async Task SuggestionInStatePShouldBeUpdatedForPublisherAdminWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = publisher;

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", state: SuggestionStates.P, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.Parse(original.Suggestion.UserId.Value), original.Suggestion.UserOrgUri.Value, targetPublisher);
            post.Status = SuggestionStates.P;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(original.Suggestion.OrgToUri.Value, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(original.Suggestion.Type.Value, updated.Suggestion.Type.Value);
            Assert.Equal(original.Suggestion.DatasetUri.Value, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(original.Suggestion.Description.Value, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO equals
        }

        [Fact]
        public async Task SuggestionInStateRShouldBeUpdatedForPublisherAdminWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = publisher;

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", state: SuggestionStates.R, targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.Parse(original.Suggestion.UserId.Value), original.Suggestion.UserOrgUri.Value, targetPublisher);
            post.Status = SuggestionStates.R;

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(original.Suggestion.OrgToUri.Value, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(original.Suggestion.Type.Value, updated.Suggestion.Type.Value);
            Assert.Equal(original.Suggestion.DatasetUri.Value, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(original.Suggestion.Description.Value, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO equals
        }

        [Fact]
        public async Task SuggestionShouldNotBeUpdatedToOtherPublisherForPublisherAdminWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", state: SuggestionStates.P, targetPublisher: publisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.Parse(original.Suggestion.UserId.Value), original.Suggestion.UserOrgUri.Value, "https://example.com/new-other-publisher");

            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Suggestion.OrgToUri.Value, updated.Suggestion.OrgToUri.Value);
        }

        [Fact]
        public async Task SuggestionShouldNotBeUpdatedToOtherUserForPublisherAdminWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            string? targetPublisher = publisher;

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "other@example.com", targetPublisher: targetPublisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(Guid.NewGuid(), targetPublisher: targetPublisher);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
        }

        [Fact]
        public async Task SuggestionInStateCShouldBeUpdatedForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(assignedUserId, assignedUserEmail);

            int beforeCount = (await api.GetAllSuggestions()).Count();
            using QueryExecutionListener listener = new QueryExecutionListener();

            SuggestionDto post = CreateInput(assignedUserId);
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStatePShouldBeUpdatedForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(assignedUserId, assignedUserEmail, state: SuggestionStates.P);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(assignedUserId);
            post.Status = SuggestionStates.P;
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStateRShouldBeUpdatedForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(assignedUserId, assignedUserEmail, state: SuggestionStates.R);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(assignedUserId);
            post.Status = SuggestionStates.R;
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStateCShouldBeUpdatedToStatePForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(assignedUserId, assignedUserEmail, state: SuggestionStates.C);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(assignedUserId);
            post.Status = SuggestionStates.P;
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStateCShouldBeUpdatedToStateRForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(assignedUserId, assignedUserEmail, state: SuggestionStates.C);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(assignedUserId);
            post.Status = SuggestionStates.R;
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStatePShouldBeUpdatedToStateCForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(assignedUserId, assignedUserEmail, state: SuggestionStates.P);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(assignedUserId);
            post.Status = SuggestionStates.C;
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStatePShouldBeUpdatedToStateRForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(assignedUserId, assignedUserEmail, state: SuggestionStates.P);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(assignedUserId);
            post.Status = SuggestionStates.R;
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStateRShouldBeUpdatedToStateCForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(assignedUserId, assignedUserEmail, state: SuggestionStates.R);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(assignedUserId);
            post.Status = SuggestionStates.C;
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionInStateRShouldBeUpdatedToStatePForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            Guid assignedUserId = Guid.NewGuid();
            string assignedUserEmail = "user@test.sk";

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(assignedUserId, assignedUserEmail, state: SuggestionStates.R);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            SuggestionDto post = CreateInput(assignedUserId);
            post.Status = SuggestionStates.P;
            using HttpResponseMessage response = await client.PutAsync($"/cms/suggestions/{original.Id}", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());

            SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(post.Title, updated.Title);
            Assert.NotNull(updated.Suggestion);
            Assert.Equal(original.Suggestion.UserId.Value, updated.Suggestion.UserId.Value);
            Assert.Equal(original.Suggestion.UserEmail.Value, updated.Suggestion.UserEmail.Value);
            Assert.Equal(original.Suggestion.UserOrgUri.Value, updated.Suggestion.UserOrgUri.Value);
            Assert.Equal(post.OrgToUri, updated.Suggestion.OrgToUri.Value);
            Assert.Equal(post.Type, updated.Suggestion.Type.Value);
            Assert.Equal(post.DatasetUri, updated.Suggestion.DatasetUri.Value);
            Assert.Equal(post.Description, updated.Suggestion.Description.Value);
            Assert.Equal(post.Status, updated.Suggestion.Status.Value);
            Assert.Equal(original.Published, updated.Published);
            Assert.True((DateTime.UtcNow - updated.Suggestion.Updated.Value).Duration().TotalMinutes < 1);
            Assert.Empty(updated.Suggestion.Likes.Value);

            //TODO comments + likes
        }

        [Fact]
        public async Task SuggestionShouldBeDeletedInStateCForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllSuggestions()).Count());

            Assert.Null(await api.FindOneSuggestion(original.Id));
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStatePForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, state: SuggestionStates.P);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStateRForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, state: SuggestionStates.R);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OtherUserSuggestionShouldNotBeDeletedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldBeDeletedInStateCForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllSuggestions()).Count());

            Assert.Null(await api.FindOneSuggestion(original.Id));
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStatePForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, state: SuggestionStates.P);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStateRForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, state: SuggestionStates.R);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStateCForPublisherWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", targetPublisher: publisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStatePForPublisherWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", state: SuggestionStates.P, targetPublisher: publisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStateRForPublisherWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", state: SuggestionStates.R, targetPublisher: publisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OtherUserSuggestionShouldNotBeDeletedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldBeDeletedInStateCForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();


            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllSuggestions()).Count());

            Assert.Null(await api.FindOneSuggestion(original.Id));
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStatePForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, state: SuggestionStates.P);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStateRForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(userId, userEmail, publisher, state: SuggestionStates.R);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStateCForPublisherAdminWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", targetPublisher: publisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStatePForPublisherAdminWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", state: SuggestionStates.P, targetPublisher: publisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldNotBeDeletedInStateRForPublisherAdminWhenAssigned()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", state: SuggestionStates.R, targetPublisher: publisher);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OtherUserSuggestionShouldNotBeDeletedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllSuggestions()).Count());
            Assert.NotNull(await api.FindOneSuggestion(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SuggestionShouldBeDeletedInStateCForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllSuggestions()).Count());

            Assert.Null(await api.FindOneSuggestion(original.Id));
        }

        [Fact]
        public async Task SuggestionShouldBeDeletedInStatePForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", state: SuggestionStates.P);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllSuggestions()).Count());

            Assert.Null(await api.FindOneSuggestion(original.Id));
        }

        [Fact]
        public async Task SuggestionShouldBeDeletedInStateRForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", state: SuggestionStates.R);

            int beforeCount = (await api.GetAllSuggestions()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/suggestions/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllSuggestions()).Count());

            Assert.Null(await api.FindOneSuggestion(original.Id));
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
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk");

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/suggestions/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Suggestion.Updated.Value, updated.Suggestion.Updated.Value);
                Assert.Equal(new[] { userId }, updated.Suggestion.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Empty(updated.Suggestion.Likes.Value);
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
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", likeUsers: new[] { userId });

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/suggestions/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Suggestion.Updated.Value, updated.Suggestion.Updated.Value);
                Assert.Empty(updated.Suggestion.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { userId }, updated.Suggestion.Likes.Value);
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
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", likeUsers: otherUsers);

            QueryExecutionListener listener = new QueryExecutionListener();
            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/suggestions/likes", JsonContent.Create(input));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Suggestion.Updated.Value, updated.Suggestion.Updated.Value);
                Assert.Equal(otherUsers.Union(new[] { userId }), updated.Suggestion.Likes.Value);                
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers, updated.Suggestion.Likes.Value);
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
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", likeUsers: otherUsers.Union(new[] { userId }));

            QueryExecutionListener listener = new QueryExecutionListener();
            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/suggestions/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Suggestion.Updated.Value, updated.Suggestion.Updated.Value);
                Assert.Equal(otherUsers, updated.Suggestion.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers.Union(new[] { userId }), updated.Suggestion.Likes.Value);
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
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk");

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = Guid.NewGuid()
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/suggestions/likes", JsonContent.Create(input));
            if (role is not null)
            {
                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Suggestion.Updated.Value, updated.Suggestion.Updated.Value);
                Assert.Empty(updated.Suggestion.Likes.Value);

                Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Empty(updated.Suggestion.Likes.Value);
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
            SuggestionPost original = await api.CreateSuggestion(Guid.NewGuid(), "test2@test.sk", likeUsers: new[] { otherUserId });

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = otherUserId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/suggestions/likes", JsonContent.Create(input));
            if (role is not null)
            {
                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(original.Published, updated.Published);
                Assert.Equal(original.Suggestion.Updated.Value, updated.Suggestion.Updated.Value);
                Assert.Equal(new[] { otherUserId }, updated.Suggestion.Likes.Value);

                Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                SuggestionPost? updated = await api.FindOneSuggestion(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { otherUserId }, updated.Suggestion.Likes.Value);
            }
        }
    }
}
