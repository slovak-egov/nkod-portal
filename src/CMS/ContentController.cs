using CMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace CMS
{
    [Route("content")]
    [ApiController]
    [AllowAnonymous]
    public class ContentController
    {
        private readonly IApi api;

        public ContentController(IApi api)
        {
            this.api = api;
        }

        [HttpGet]
        [Route("suggestion")]
        public async Task<IEnumerable<Suggestion>> GetSuggestions([FromQuery] string datasetUri)
        {
            var page = await api.Pages.GetBySlugAsync(datasetUri);
            var datasetPage = await api.Pages.GetByIdAsync<DatasetPage>(page.Id);
            datasetPage.Archive = await api.Archives.GetByIdAsync<SuggestionPost>(page.Id);
            return datasetPage.Archive.Posts.Select(p => new Suggestion
            {
                State = p.Suggestion.State.Value,
                OrgToUri = p.Suggestion.OrgToUri,
                Type = p.Suggestion.Type.Value,
                Description = p.Suggestion.Description,
                UserId = p.Suggestion.UserId,
                UserOrgUri = p.Suggestion.UserOrgUri,
                DatasetUri = datasetUri,
                Title = p.Title
            });
        }

        [HttpPost]
        [Route("suggestion")]
        public async Task<IResult> Suggestion(Suggestion suggestion)
        {
            var archiveId = await GetDatasetGuidAsync(suggestion.DatasetUri);
            var post = await api.Posts.CreateAsync<SuggestionPost>();
            post.Title = suggestion.Title;
            post.Suggestion = new SuggestionRegion
            {
                Description = suggestion.Description,
                UserId = suggestion.UserId,
                UserOrgUri = suggestion.UserOrgUri,
                OrgToUri = suggestion.OrgToUri,
                Type = new SelectField<ContentTypes>
                {
                    Value = suggestion.Type
                },
                State = new SelectField<States>
                {
                    Value = suggestion.State
                }
            };

            post.Category = new Taxonomy
            {
                Title = "Suggestion",
                Slug = "suggestion",
                Type = TaxonomyType.Category,
            };
            post.BlogId = archiveId;
            post.Published = DateTime.Now;

            await api.Posts.SaveAsync(post);
            return Results.Ok();
        }

        private async Task<Guid> GetDatasetGuidAsync(string datasetUri)
        {
            var page = await api.Pages.GetBySlugAsync<DatasetPage>(Utils.GenerateSlug(datasetUri));
            if (page != null)
            {
                return page.Id;
            }

            page = await api.Pages.CreateAsync<DatasetPage>();
            page.Title = datasetUri;
            page.Slug = datasetUri;
            page.EnableComments = true;
            page.Published = DateTime.Now;
            page.SiteId = await GetSiteGuidAsync();
            await api.Pages.SaveAsync(page);

            return page.Id;
        }

        private async Task<Guid> GetSiteGuidAsync()
        {
            var site = await api.Sites.GetDefaultAsync();
            return site.Id;
        }
    }
}