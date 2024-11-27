using CMS.Applications;
using CMS.Comments;
using CMS.Likes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Piranha;
using Piranha.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Test
{
    public class CommentTests
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

            using HttpResponseMessage response = await client.GetAsync("/cms/comments");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            GetApplicationsResponse? r = await response.Content.ReadFromJsonAsync<GetApplicationsResponse>();
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
            Comment post = await api.CreateComment();

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/comments");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            GetApplicationsResponse? r = await response.Content.ReadFromJsonAsync<GetApplicationsResponse>();
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
                await api.CreateComment();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/comments?pageNumber=0&pageSize=10");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            GetApplicationsResponse? r = await response.Content.ReadFromJsonAsync<GetApplicationsResponse>();
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
                await api.CreateComment();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/comments?pageNumber=1&pageSize=10");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            GetApplicationsResponse? r = await response.Content.ReadFromJsonAsync<GetApplicationsResponse>();
            Assert.NotNull(r);
            Assert.Equal(10, r.Items.Count());
            Assert.NotNull(r.PaginationMetadata);
            Assert.Equal(10, r.PaginationMetadata.PageSize);
            Assert.Equal(1, r.PaginationMetadata.CurrentPage);
            Assert.Equal(100, r.PaginationMetadata.TotalItemCount);
        }

        private async Task<CommentDto> CreateInput(IApi api, Guid userId, Guid? contentId = null, Guid? parentId = null)
        {
            Guid effectiveContentId;

            if (!contentId.HasValue)
            {
                ApplicationPost app = await api.CreateApplication();
                effectiveContentId = app.Id;
            }
            else
            {
                effectiveContentId = contentId.Value;
            }

            return new CommentDto
            {
                Id = Guid.NewGuid(),
                ContentId = effectiveContentId,                
                UserId = userId,
                Body = "Test body input",
                Created = DateTime.MinValue,
                ParentId = parentId ?? Guid.Empty
            };
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task CreateNewComment(string? role)
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
            await api.CreateComment();

            using QueryExecutionListener listener = new QueryExecutionListener();

            CommentDto post = await CreateInput(api, userId);
            using HttpResponseMessage response = await client.PostAsync("/cms/comments", JsonContent.Create(post));

            if (role is not null)
            {
                if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
                {
                    Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
                }

                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                Guid id = await response.Content.ReadFromJsonAsync<Guid>();
                Assert.NotEqual(Guid.Empty, id);

                Comment? created = await api.FindOneComment(id);
                Assert.NotNull(created);
                Assert.Equal(post.ContentId, created.ContentId);
                Assert.Equal(userId.ToString("D"), created.UserId);
                Assert.NotNull(created.Email);
                Assert.Equal(post.Body + "|" + userFormattedName, created.Body);
                Assert.True((DateTime.UtcNow - created.Created).Duration().TotalMinutes < 1);
                Assert.Equal(post.ParentId.ToString("D"), created.Author);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task CommentCanNotBeCreatedForOtherUser(string? role)
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
            await api.CreateComment();

            CommentDto post = await CreateInput(api, Guid.NewGuid());
            using HttpResponseMessage response = await client.PostAsync("/cms/comments", JsonContent.Create(post));

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
        public async Task CreateNewCommentShouldNotBeEnabledWithInvalidToken(string role)
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
            await api.CreateComment();

            CommentDto post = await CreateInput(api, userId);
            using HttpResponseMessage response = await client.PostAsync("/cms/comments", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task BodyShouldBeRequired(string role)
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
            await api.CreateComment();

            int beforeCount = (await api.GetAllComments()).Count();

            CommentDto post = await CreateInput(api, userId);
            post.Body = null;
            using HttpResponseMessage response = await client.PostAsync("/cms/comments", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task BodyShouldNotBeEmpty(string role)
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
            await api.CreateComment();

            int beforeCount = (await api.GetAllComments()).Count();

            CommentDto post = await CreateInput(api, userId);
            post.Body = string.Empty;
            using HttpResponseMessage response = await client.PostAsync("/cms/comments", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task BodyShouldNotBeWhitespace(string role)
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
            await api.CreateComment();

            int beforeCount = (await api.GetAllComments()).Count();

            CommentDto post = await CreateInput(api, userId);
            post.Body = " ";
            using HttpResponseMessage response = await client.PostAsync("/cms/comments", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());
        }

        [Fact]
        public async Task CommentShouldBeUpdatedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string userFormattedName = "Meno Priezvisko";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail, userFormattedName: userFormattedName));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);

            int beforeCount = (await api.GetAllComments()).Count();
            using QueryExecutionListener listener = new QueryExecutionListener();

            CommentDto post = await CreateInput(api, userId, original.ContentId, Guid.Parse(original.Author));
            using HttpResponseMessage response = await client.PutAsync($"/cms/comments/{original.Id}", JsonContent.Create(post));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());

            Comment? updated = await api.FindOneComment(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.ContentId, updated.ContentId);
            Assert.Equal(original.UserId, updated.UserId);
            Assert.Equal(original.Email, updated.Email);
            Assert.Equal(post.Body + "|" + userFormattedName, updated.Body);
            Assert.Equal(original.Created, updated.Created);
            Assert.Equal(original.Author, updated.Author);
        }

        [Fact]
        public async Task OtherUserCommentShouldNotBeUpdatedForCommunity()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(Guid.NewGuid(), "other@test.sk");

            int beforeCount = (await api.GetAllComments()).Count();

            CommentDto post = await CreateInput(api, userId, original.ContentId, Guid.Parse(original.Author));
            using HttpResponseMessage response = await client.PutAsync($"/cms/comments/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task CommentShouldNotBeUpdatedForCommunityUserToOtherUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);

            int beforeCount = (await api.GetAllComments()).Count();

            CommentDto post = await CreateInput(api, Guid.NewGuid(), original.ContentId, Guid.Parse(original.Author));
            using HttpResponseMessage response = await client.PutAsync($"/cms/comments/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());

            Comment? updated = await api.FindOneComment(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.UserId, updated.UserId);
            Assert.Equal(original.Email, updated.Email);
        }

        [Fact]
        public async Task CommentShouldBeUpdatedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";
            string userFormattedName = "Meno Priezvisko";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail, userFormattedName: userFormattedName));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);

            int beforeCount = (await api.GetAllComments()).Count();
            using QueryExecutionListener listener = new QueryExecutionListener();

            CommentDto post = await CreateInput(api, userId, original.ContentId, Guid.Parse(original.Author));
            using HttpResponseMessage response = await client.PutAsync($"/cms/comments/{original.Id}", JsonContent.Create(post));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());

            Comment? updated = await api.FindOneComment(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.ContentId, updated.ContentId);
            Assert.Equal(original.UserId, updated.UserId);
            Assert.Equal(original.Email, updated.Email);
            Assert.Equal(post.Body + "|" + userFormattedName, updated.Body);
            Assert.Equal(original.Created, updated.Created);
            Assert.Equal(original.Author, updated.Author);
        }

        [Fact]
        public async Task OtherUserCommentShouldNotBeUpdatedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(Guid.NewGuid(), "other@test.sk");

            int beforeCount = (await api.GetAllComments()).Count();

            CommentDto post = await CreateInput(api, userId, original.ContentId, Guid.Parse(original.Author));
            using HttpResponseMessage response = await client.PutAsync($"/cms/comments/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task CommentShouldNotBeUpdatedToOtherUserForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);

            int beforeCount = (await api.GetAllComments()).Count();

            CommentDto post = await CreateInput(api, Guid.NewGuid(), original.ContentId, Guid.Parse(original.Author));
            using HttpResponseMessage response = await client.PutAsync($"/cms/comments/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());

            Comment? updated = await api.FindOneComment(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.UserId, updated.UserId);
            Assert.Equal(original.Email, updated.Email);
        }

        [Fact]
        public async Task CommentShouldBeUpdatedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";
            string userFormattedName = "Meno Priezvisko";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail, userFormattedName: userFormattedName));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);

            int beforeCount = (await api.GetAllComments()).Count();
            using QueryExecutionListener listener = new QueryExecutionListener();

            CommentDto post = await CreateInput(api, userId, original.ContentId, Guid.Parse(original.Author));
            using HttpResponseMessage response = await client.PutAsync($"/cms/comments/{original.Id}", JsonContent.Create(post));

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());

            Comment? updated = await api.FindOneComment(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.ContentId, updated.ContentId);
            Assert.Equal(original.UserId, updated.UserId);
            Assert.Equal(original.Email, updated.Email);
            Assert.Equal(post.Body + "|" + userFormattedName, updated.Body);
            Assert.Equal(original.Created, updated.Created);
            Assert.Equal(original.Author, updated.Author);
        }

        [Fact]
        public async Task OtherUserCommentShouldNotBeUpdatedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(Guid.NewGuid(), "other@test.sk");

            int beforeCount = (await api.GetAllComments()).Count();

            CommentDto post = await CreateInput(api, userId, original.ContentId, Guid.Parse(original.Author));
            using HttpResponseMessage response = await client.PutAsync($"/cms/comments/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task CommentShouldNotBeUpdatedToOtherUserForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher: publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);

            int beforeCount = (await api.GetAllComments()).Count();

            CommentDto post = await CreateInput(api, Guid.NewGuid(), original.ContentId, Guid.Parse(original.Author));
            using HttpResponseMessage response = await client.PutAsync($"/cms/comments/{original.Id}", JsonContent.Create(post));
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());

            Comment? updated = await api.FindOneComment(original.Id);
            Assert.NotNull(updated);
            Assert.Equal(original.UserId, updated.UserId);
            Assert.Equal(original.Email, updated.Email);
        }

        [Fact]
        public async Task CommentShouldBeDeletedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllComments()).Count());

            Assert.Null(await api.FindOneComment(original.Id));
        }

        [Fact]
        public async Task CommentWithReplyShouldNotBeDeletedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);
            await api.CreateComment(Guid.NewGuid(), "test2@test.sk", original.ContentId, original.Id);

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());
            Assert.NotNull(await api.FindOneComment(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OtherUserCommentShouldNotBeDeletedForCommunityUser()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(CommunityUser, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());
            Assert.NotNull(await api.FindOneComment(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CommentShouldBeDeletedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllComments()).Count());

            Assert.Null(await api.FindOneComment(original.Id));
        }

        [Fact]
        public async Task OtherUserCommentShouldNotBeDeletedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());
            Assert.NotNull(await api.FindOneComment(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CommentWithReplyShouldNotBeDeletedForPublisher()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);
            await api.CreateComment(Guid.NewGuid(), "test2@test.sk", original.ContentId, original.Id);

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());
            Assert.NotNull(await api.FindOneComment(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CommentShouldBeDeletedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);

            int beforeCount = (await api.GetAllComments()).Count();


            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllComments()).Count());

            Assert.Null(await api.FindOneComment(original.Id));
        }

        [Fact]
        public async Task OtherUserCommentShouldNotBeDeletedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";
            string publisher = "http://example.com/publisher";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, publisher, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());
            Assert.NotNull(await api.FindOneComment(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CommentWithReplyShouldNotBeDeletedForPublisherAdmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(PublisherAdmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);
            await api.CreateComment(Guid.NewGuid(), "test2@test.sk", original.ContentId, original.Id);

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());
            Assert.NotNull(await api.FindOneComment(original.Id));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CommentShouldBeDeletedForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(Guid.NewGuid(), "test2@test.sk");

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllComments()).Count());

            Assert.Null(await api.FindOneComment(original.Id));
        }

        [Fact]
        public async Task CommentWithReplyShouldBeDeletedForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);
            await api.CreateComment(Guid.NewGuid(), "test2@test.sk", original.ContentId, original.Id);

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 2, (await api.GetAllComments()).Count());

            Assert.Null(await api.FindOneComment(original.Id));
        }

        [Fact]
        public async Task CommentWithDoubleReplyShouldBeDeletedForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            Comment original = await api.CreateComment(userId, userEmail);
            Comment reply = await api.CreateComment(Guid.NewGuid(), "test2@test.sk", original.ContentId, original.Id);
            await api.CreateComment(Guid.NewGuid(), "test2@test.sk", original.ContentId, reply.Id);

            int beforeCount = (await api.GetAllComments()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/comments/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 3, (await api.GetAllComments()).Count());

            Assert.Null(await api.FindOneComment(original.Id));
        }
    }
}
