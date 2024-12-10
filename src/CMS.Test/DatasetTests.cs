using CMS.Applications;
using CMS.Comments;
using CMS.Datasets;
using CMS.Likes;
using CMS.Suggestions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NkodSk.Abstractions;
using Piranha;
using Piranha.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace CMS.Test
{
    public class DatasetTests
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

            using HttpResponseMessage response = await client.GetAsync("/cms/datasets");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            DatasetSearchResponse? r = await response.Content.ReadFromJsonAsync<DatasetSearchResponse>();
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
            DatasetPost post = await api.CreateDataset();

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/datasets");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            DatasetSearchResponse? r = await response.Content.ReadFromJsonAsync<DatasetSearchResponse>();
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
                await api.CreateDataset();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/datasets?pageNumber=0&pageSize=10");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            DatasetSearchResponse? r = await response.Content.ReadFromJsonAsync<DatasetSearchResponse>();
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
                await api.CreateDataset();
            }

            using QueryExecutionListener listener = new QueryExecutionListener();

            using HttpResponseMessage response = await client.GetAsync("/cms/datasets?pageNumber=1&pageSize=10");

            if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
            {
                Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
            }

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            DatasetSearchResponse? r = await response.Content.ReadFromJsonAsync<DatasetSearchResponse>();
            Assert.NotNull(r);
            Assert.Equal(10, r.Items.Count());
            Assert.NotNull(r.PaginationMetadata);
            Assert.Equal(10, r.PaginationMetadata.PageSize);
            Assert.Equal(1, r.PaginationMetadata.CurrentPage);
            Assert.Equal(100, r.PaginationMetadata.TotalItemCount);
        }

        private DatasetDto CreateInput(string? datasetUri = null)
        {
            return new DatasetDto
            {
                Id = Guid.NewGuid(),
                DatasetUri = datasetUri ?? "https://example.com/dataset/" + Guid.NewGuid().ToString("N"),
                Created = DateTime.MinValue,
                Updated = DateTime.MinValue,
                CommentCount = 100,
                LikeCount = 200
            };
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task CreateNewDataset(string? role)
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
            await api.CreateDataset();

            using QueryExecutionListener listener = new QueryExecutionListener();

            DatasetDto post = CreateInput();
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets", JsonContent.Create(post));

            if (role is not null)
            {
                if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
                {
                    Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
                }

                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                Guid id = await response.Content.ReadFromJsonAsync<Guid>();
                Assert.NotEqual(Guid.Empty, id);

                DatasetPost? created = await api.FindOneDataset(id);
                Assert.NotNull(created);
                Assert.NotNull(created.Dataset);
                Assert.Equal(post.DatasetUri, created.Title);
                Assert.True((DateTime.UtcNow - created.Published!.Value).Duration().TotalMinutes < 1);
                Assert.Null(created.Dataset.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task CreateNewDatasetShouldNotBeEnabledWithInvalidToken(string role)
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
            await api.CreateDataset();

            DatasetDto post = CreateInput();
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task DatasetUriShouldBeRequired(string role)
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
            await api.CreateDataset();

            int beforeCount = (await api.GetAllDatasets()).Count();

            DatasetDto post = CreateInput();
            post.DatasetUri = null;
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllDatasets()).Count());
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task DatasetUriShouldNotBeEmpty(string role)
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
            await api.CreateDataset();

            int beforeCount = (await api.GetAllDatasets()).Count();

            DatasetDto post = CreateInput();
            post.DatasetUri = string.Empty;
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllDatasets()).Count());
        }

        [Theory]
        [MemberData(nameof(GetExplicitRoles))]
        public async Task DatasetUriShouldNotBeWhitespace(string role)
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
            await api.CreateDataset();

            int beforeCount = (await api.GetAllDatasets()).Count();

            DatasetDto post = CreateInput();
            post.DatasetUri = " ";
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllDatasets()).Count());
        }

        [Fact]
        public async Task DatasetShouldBeDeletedForSuperadmin()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();
            Guid userId = Guid.NewGuid();
            string userEmail = "test@test.sk";

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(Superadmin, userId: userId.ToString(), userEmail: userEmail));

            using IApi api = f.CreateApi();
            DatasetPost original = await api.CreateDataset();

            int beforeCount = (await api.GetAllDatasets()).Count();

            using HttpResponseMessage response = await client.DeleteAsync($"/cms/datasets/{original.Id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(beforeCount - 1, (await api.GetAllDatasets()).Count());

            Assert.Null(await api.FindOneDataset(original.Id));
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
            DatasetPost original = await api.CreateDataset();

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { userId }, updated.Dataset.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Empty(updated.Dataset.Likes.Value);
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
            DatasetPost original = await api.CreateDataset(likeUsers: new[] { userId });

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Empty(updated.Dataset.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { userId }, updated.Dataset.Likes.Value);
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
            DatasetPost original = await api.CreateDataset(likeUsers: otherUsers);

            QueryExecutionListener listener = new QueryExecutionListener();
            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers.Union(new[] { userId }), updated.Dataset.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers, updated.Dataset.Likes.Value);
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
            DatasetPost original = await api.CreateDataset(likeUsers: otherUsers.Union(new[] { userId }));

            QueryExecutionListener listener = new QueryExecutionListener();
            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers, updated.Dataset.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers.Union(new[] { userId }), updated.Dataset.Likes.Value);
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
            DatasetPost original = await api.CreateDataset();

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = Guid.NewGuid()
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));
            if (role is not null)
            {
                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Empty(updated.Dataset.Likes.Value);

                Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Empty(updated.Dataset.Likes.Value);
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
            DatasetPost original = await api.CreateDataset(likeUsers: new[] { otherUserId });

            LikeDto input = new LikeDto
            {
                ContentId = original.Id,
                UserId = otherUserId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));
            if (role is not null)
            {
                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { otherUserId }, updated.Dataset.Likes.Value);

                Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { otherUserId }, updated.Dataset.Likes.Value);
            }
        }

        private async Task<DatasetCommentDto> CreateInput(IApi api, Guid userId, string? datasetUri = null)
        {
            if (datasetUri is null)
            {
                DatasetPost ds = await api.CreateDataset(datasetUri);
                datasetUri = ds.Title;
            }

            return new DatasetCommentDto
            {
                DatasetUri = datasetUri,
                UserId = userId,
                Body = "Test body input",
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

            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, f.CreateToken(role, publisher: publisher, userId: userId.ToString()));
            }

            using IApi api = f.CreateApi();
            await api.CreateComment();

            using QueryExecutionListener listener = new QueryExecutionListener();

            DatasetCommentDto post = await CreateInput(api, userId);
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets/comments", JsonContent.Create(post));

            if (role is not null)
            {
                if (QueryExecutionListener.Enabled && listener.ExecutedQueryCount >= 10)
                {
                    Assert.Fail($"Too many queries executed {listener.ExecutedQueryCount}");
                }

                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                Guid id = await response.Content.ReadFromJsonAsync<Guid>();
                Assert.NotEqual(Guid.Empty, id);

                //Comment? created = await api.FindOneComment(id);
                //Assert.NotNull(created);
                //Assert.Equal(userId.ToString("D"), created.UserId);
                //Assert.Equal(userEmail, created.Email);
                //Assert.Equal(post.Body, created.Body);
                //Assert.True((DateTime.UtcNow - created.Published).Duration().TotalMinutes < 1);
                //Assert.Equal(Guid.Empty.ToString("D"), created.Author);
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

            DatasetCommentDto post = await CreateInput(api, userId, userEmail);
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets/comments", JsonContent.Create(post));

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

            DatasetCommentDto post = await CreateInput(api, userId, userEmail);
            post.Body = null;
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets/comments", JsonContent.Create(post));

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

            DatasetCommentDto post = await CreateInput(api, userId, userEmail);
            post.Body = string.Empty;
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets/comments", JsonContent.Create(post));

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

            DatasetCommentDto post = await CreateInput(api, userId, userEmail);
            post.Body = " ";
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets/comments", JsonContent.Create(post));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(beforeCount, (await api.GetAllComments()).Count());
        }























        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldBeAddedWithUri(string? role)
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

            DatasetLikeDto input = new DatasetLikeDto
            {
                DatasetUri = "https://exampla.com/dataset/" + Guid.NewGuid().ToString("N"),
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));

            DatasetPost? updated = (await api.GetAllDatasets()).FirstOrDefault(d => d.Title == input.DatasetUri);

            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                Assert.NotNull(updated);
                Assert.Equal(new[] { userId }, updated.Dataset.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                Assert.Null(updated);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldBeRemovedWithUri(string? role)
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
            DatasetPost original = await api.CreateDataset("https://exampla.com/dataset/" + Guid.NewGuid().ToString("N"), likeUsers: new[] { userId });

            DatasetLikeDto input = new DatasetLikeDto
            {
                DatasetUri = original.Title,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Empty(updated.Dataset.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { userId }, updated.Dataset.Likes.Value);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldBeAddedWithOtherWithUri(string? role)
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
            DatasetPost original = await api.CreateDataset(likeUsers: otherUsers);

            QueryExecutionListener listener = new QueryExecutionListener();
            DatasetLikeDto input = new DatasetLikeDto
            {
                DatasetUri = original.Title,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));

            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers.Union(new[] { userId }), updated.Dataset.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers, updated.Dataset.Likes.Value);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldBeRemovedWithOtherWithUri(string? role)
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
            DatasetPost original = await api.CreateDataset(likeUsers: otherUsers.Union(new[] { userId }));

            QueryExecutionListener listener = new QueryExecutionListener();
            DatasetLikeDto input = new DatasetLikeDto
            {
                DatasetUri = original.Title,
                UserId = userId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));
            if (role is not null)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers, updated.Dataset.Likes.Value);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(otherUsers.Union(new[] { userId }), updated.Dataset.Likes.Value);
            }
        }

        [Theory]
        [MemberData(nameof(GetRolesWithAnonymous))]
        public async Task LikeShouldNotBeAddedForOtherUserWithUri(string? role)
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

            DatasetLikeDto input = new DatasetLikeDto
            {
                DatasetUri = "https://exampla.com/dataset/" + Guid.NewGuid().ToString("N"),
                UserId = Guid.NewGuid()
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));

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
        public async Task LikeShouldNotBeRemovedForOtherUserWithUri(string? role)
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
            DatasetPost original = await api.CreateDataset(likeUsers: new[] { otherUserId });

            DatasetLikeDto input = new DatasetLikeDto
            {
                DatasetUri = "https://exampla.com/dataset/" + Guid.NewGuid().ToString("N"),
                UserId = otherUserId
            };

            using HttpResponseMessage response = await client.PostAsync($"/cms/datasets/likes", JsonContent.Create(input));
            if (role is not null)
            {
                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { otherUserId }, updated.Dataset.Likes.Value);

                Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
            }
            else
            {
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

                DatasetPost? updated = await api.FindOneDataset(original.Id);
                Assert.NotNull(updated);
                Assert.Equal(new[] { otherUserId }, updated.Dataset.Likes.Value);
            }
        }

        [Fact]
        public async Task PublisherShouldBeNotifiedOnComment()
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

            DatasetCommentDto post = await CreateInput(api, userId);
            post.DatasetUri = dataset.Uri.OriginalString;
            using HttpResponseMessage response = await client.PostAsync("/cms/datasets/comments", JsonContent.Create(post));
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.Single(f.TestNotificationService.Notifications);
            Assert.Equal(publisherEmail, f.TestNotificationService.Notifications[0].Item1);
        }
    }
}
