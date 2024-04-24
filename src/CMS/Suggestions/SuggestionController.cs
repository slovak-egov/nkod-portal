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
    public class SuggestionController
    {
        private readonly IApi api;

        public SuggestionController(IApi api)
        {
            this.api = api;
        }

        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<SuggestionDto>> Get()
        {
            var page = await api.Pages.GetBySlugAsync(SuggestionsPage.WellKnownSlug);
            var archive = await api.Archives.GetByIdAsync<SuggestionPost>(page.Id);
            return archive.Posts.Select(Convert);
        }

        private static SuggestionDto Convert(SuggestionPost p)
        {
            return new SuggestionDto
            {
                Id = p.Id,
				Created = p.Created,
				Updated = p.LastModified,
				Status = p.Suggestion.Status.Value,
                OrgToUri = p.Suggestion.OrgToUri,
                Type = p.Suggestion.Type.Value,
                Description = p.Suggestion.Description,
                UserId = Guid.Parse(p.Suggestion.UserId.Value),
                UserOrgUri = p.Suggestion.UserOrgUri,
                DatasetUri = p.Suggestion.DatasetUri.Value,
                Title = p.Title
            };
        }

        [HttpPost]
        [Route("")]
        public async Task<IResult> Save(SuggestionDto dto)
        {
            var archiveId = await GetArchiveGuidAsync();
            var post = await api.Posts.CreateAsync<SuggestionPost>();
            post.Title = dto.Title;
            post.Suggestion = new SuggestionRegion
            {
                Description = dto.Description,
                UserId = dto.UserId.ToString("D"),
                UserOrgUri = dto.UserOrgUri,
                OrgToUri = dto.OrgToUri,
                DatasetUri = dto.DatasetUri,
                Type = new SelectField<ContentTypes>
                {
                    Value = dto.Type
                },
                Status = new SelectField<SuggestionStates>
                {
                    Value = dto.Status
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

		[HttpPut]
		[Route("")]
		public async Task<IResult> Update(SuggestionDto dto)
		{
			var post = await api.Posts.GetByIdAsync<SuggestionPost>(dto.Id);
			post.Title = dto.Title;

			post.Suggestion.Description = dto.Description;
			post.Suggestion.UserId = dto.UserId.ToString("D");
			post.Suggestion.UserOrgUri = dto.UserOrgUri;
			post.Suggestion.OrgToUri = dto.OrgToUri;
			post.Suggestion.DatasetUri = dto.DatasetUri;
			post.Suggestion.Type.Value = dto.Type;
			post.Suggestion.Status.Value = dto.Status;

			await api.Posts.SaveAsync(post);
			return Results.Ok();
		}

		private async Task<Guid> GetArchiveGuidAsync()
        {
            var page = await api.Pages.GetBySlugAsync(SuggestionsPage.WellKnownSlug);
            return page?.Id ?? (await CreatePage()).Id;
        }

        private async Task<PageBase> CreatePage()
        {
            PageBase newPage = await api.Pages.CreateAsync<SuggestionsPage>();

            newPage.Title = SuggestionsPage.WellKnownSlug;
            newPage.EnableComments = true;
            newPage.Published = DateTime.Now;
            newPage.SiteId = await GetSiteGuidAsync();
            await api.Pages.SaveAsync(newPage);
            return newPage;
        }

        private async Task<Guid> GetSiteGuidAsync()
        {
            return (await api.Sites.GetDefaultAsync()).Id;
        }
    }
}