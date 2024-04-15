using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace CMS.Suggestions
{
    [Route("suggestions")]
    [ApiController]
    [AllowAnonymous]
    public class Controller
    {
        private readonly IApi api;

        public Controller(IApi api)
        {
            this.api = api;
        }

        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<Dto>> GetSuggestions([FromQuery] string datasetUri)
        {
            var page = await api.Pages.GetBySlugAsync(datasetUri);
            var datasetPage = await api.Pages.GetByIdAsync<DatasetPage>(page.Id);
            datasetPage.Archive = await api.Archives.GetByIdAsync<Post>(page.Id);
            return datasetPage.Archive.Posts.Select(p => new Dto
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
        [Route("")]
        public async Task<IResult> Suggestion(Dto dto)
        {
            var archiveId = await GetDatasetGuidAsync(dto.DatasetUri);
            var post = await api.Posts.CreateAsync<Post>();
            post.Title = dto.Title;
            post.Suggestion = new Region
            {
                Description = dto.Description,
                UserId = dto.UserId,
                UserOrgUri = dto.UserOrgUri,
                OrgToUri = dto.OrgToUri,
                Type = new SelectField<ContentTypes>
                {
                    Value = dto.Type
                },
                State = new SelectField<States>
                {
                    Value = dto.State
                }
            };

            post.Category = new Taxonomy
            {
                Title = "Suggestion",
                Slug = "suggestion",
                Type = TaxonomyType.Category
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