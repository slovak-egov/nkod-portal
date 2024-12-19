using CMS.Applications;
using CMS.Comments;
using CMS.Datasets;
using CMS.Suggestions;
using Piranha;
using Piranha.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CMS.Test
{
    public static class Extensions
    {
        private static async Task<Guid> GetSiteGuidAsync(this IApi api)
        {
            return (await api.Sites.GetDefaultAsync()).Id;
        }

        private static async Task<Guid> GetBlogGuidAsync(this IApi api, string slug)
        {
            var page = await api.Pages.GetBySlugAsync(slug);
            return page?.Id ?? (await api.CreatePage(slug)).Id;
        }

        private static async Task<PageBase> CreatePage(this IApi api, string slug)
        {
            PageBase newPage = await api.Pages.CreateAsync<SuggestionsPage>();

            newPage.Title = slug;
            newPage.EnableComments = true;
            newPage.Published = DateTime.UtcNow;
            newPage.SiteId = await api.GetSiteGuidAsync();
            await api.Pages.SaveAsync(newPage);
            return newPage;
        }

        public static async Task<SuggestionPost> CreateSuggestion(this IApi api, Guid? userId = null, string? userEmail = null, string? userPublisher = null, SuggestionStates state = SuggestionStates.C, string? targetPublisher = null, IEnumerable<Guid>? likeUsers = null, bool refresh = true, string? datasetUri = null)
        {
            SuggestionPost post = await api.Posts.CreateAsync<SuggestionPost>();
            post.Title = "Test title existing";
            post.Suggestion = new SuggestionRegion
            {
                Description = "Test description existing",
                UserId = (userId ?? Guid.NewGuid()).ToString("D"),
                UserEmail = userEmail ?? "test@test.sk",
                UserOrgUri = userPublisher,
                OrgToUri = targetPublisher ?? "https://example.com/some-publisher",
                Type = new Piranha.Extend.Fields.SelectField<ContentTypes> { Value = ContentTypes.PN },
                DatasetUri = new Piranha.Extend.Fields.StringField { Value = datasetUri },
                Status = new Piranha.Extend.Fields.SelectField<SuggestionStates> { Value = state },
                Likes = new MultiSelectField<Guid> { Value = likeUsers ?? Enumerable.Empty<Guid>() },
            };
            post.Category = new Taxonomy
            {
                Title = "Suggestion",
                Slug = "suggestion",
                Type = TaxonomyType.Category
            };
            post.Slug = Guid.NewGuid().ToString();
            post.BlogId = await api.GetBlogGuidAsync(SuggestionsPage.WellKnownSlug);
            post.Published = DateTime.UtcNow;
            await api.Posts.SaveAsync(post);
            return refresh ? await api.Posts.GetByIdAsync<SuggestionPost>(post.Id) : post;
        }

        public static async Task<SuggestionPost?> FindOneSuggestion(this IApi api, Guid id)
        {
            return await api.Posts.GetByIdAsync<SuggestionPost>(id);
        }

        public static async Task<IEnumerable<SuggestionPost>> GetAllSuggestions(this IApi api)
        {
            return await api.Posts.GetAllAsync<SuggestionPost>(await api.GetBlogGuidAsync(SuggestionsPage.WellKnownSlug));
        }

        public static async Task<ApplicationPost> CreateApplication(this IApi api, Guid? userId = null, string? userEmail = null, IEnumerable<Guid>? likeUsers = null)
        {
            ApplicationPost post = await api.Posts.CreateAsync<ApplicationPost>();
            post.Title = "Test title existing";
            post.Application = new ApplicationRegion
            {
                Description = "Test description existing",
                UserId = (userId ?? Guid.NewGuid()).ToString("D"),
                UserEmail = userEmail ?? "test@test.sk",
                Url = "https://example.com/some-application",
                Type = new Piranha.Extend.Fields.SelectField<ApplicationTypes> { Value = ApplicationTypes.A },
                Theme = new Piranha.Extend.Fields.SelectField<ApplicationThemes> { Value = ApplicationThemes.HE },
                Likes = new MultiSelectField<Guid> { Value = likeUsers ?? Enumerable.Empty<Guid>() },
                ContactEmail = "test-contact@example.com",
                ContactName = "Name",
                ContactSurname = "Surname",
            };
            post.Category = new Taxonomy
            {
                Title = "Application",
                Slug = "application",
                Type = TaxonomyType.Category
            };
            post.Slug = Guid.NewGuid().ToString();
            post.BlogId = await api.GetBlogGuidAsync(ApplicationsPage.WellKnownSlug);
            post.Published = DateTime.UtcNow;
            await api.Posts.SaveAsync(post);
            return await api.Posts.GetByIdAsync<ApplicationPost>(post.Id);
        }

        public static async Task<ApplicationPost?> FindOneApplication(this IApi api, Guid id)
        {
            return await api.Posts.GetByIdAsync<ApplicationPost>(id);
        }

        public static async Task<IEnumerable<ApplicationPost>> GetAllApplications(this IApi api)
        {
            return await api.Posts.GetAllAsync<ApplicationPost>(await api.GetBlogGuidAsync(ApplicationsPage.WellKnownSlug));
        }

        public static async Task<PageComment> CreateComment(this IApi api, Guid? userId = null, string? userEmail = null, Guid? contentId = null, Guid? parentId = null)
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

            PageComment comment = new PageComment
            {
                UserId = (userId ?? Guid.NewGuid()).ToString("D"),
                Author = parentId.GetValueOrDefault().ToString("D"),
                Email = userEmail ?? "test@test.sk",
                Body = "Test body|Meno Priezvisko",
                Created = DateTime.UtcNow
            };
            await api.Posts.SaveCommentAsync(effectiveContentId, comment);
            return comment;
        }

        public static async Task<Comment?> FindOneComment(this IApi api, Guid id)
        {
            return (await api.Posts.GetCommentByIdAsync(id));
        }

        public static async Task<IEnumerable<Comment>> GetAllComments(this IApi api)
        {
            return await api.Posts.GetAllCommentsAsync();
        }

        public static async Task<DatasetPost> CreateDataset(this IApi api, string? datasetUri = null, IEnumerable<Guid>? likeUsers = null)
        {
            DatasetPost post = await api.Posts.CreateAsync<DatasetPost>();
            post.Title = datasetUri ?? ("https://example.com/some-dataset/" + Guid.NewGuid().ToString("N"));
            post.Dataset = new DatasetRegion
            {
                Likes = new MultiSelectField<Guid> { Value = likeUsers ?? Enumerable.Empty<Guid>() },
            };
            post.Category = new Taxonomy
            {
                Title = "Dataset",
                Slug = "dataset",
                Type = TaxonomyType.Category
            };
            post.Slug = Guid.NewGuid().ToString();
            post.BlogId = await api.GetBlogGuidAsync(DatasetsPage.WellKnownSlug);
            post.Published = DateTime.UtcNow;
            await api.Posts.SaveAsync(post);
            return await api.Posts.GetByIdAsync<DatasetPost>(post.Id);
        }

        public static async Task<DatasetPost?> FindOneDataset(this IApi api, Guid id)
        {
            return await api.Posts.GetByIdAsync<DatasetPost>(id);
        }

        public static async Task<IEnumerable<DatasetPost>> GetAllDatasets(this IApi api)
        {
            return await api.Posts.GetAllAsync<DatasetPost>(await api.GetBlogGuidAsync(DatasetsPage.WellKnownSlug));
        }
    }
}
