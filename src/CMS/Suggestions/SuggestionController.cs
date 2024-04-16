using System.ComponentModel.DataAnnotations;
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
        public async Task<IEnumerable<SuggestionDto>> Get([FromQuery] SuggestionQueryDto query)
        {
            var page = await api.Pages.GetBySlugAsync(GetSlug(query.Id, query.Type));
            if (!IsValidType(query.Type, page))
            {
                return Enumerable.Empty<SuggestionDto>();
            }
            var archive = await api.Archives.GetByIdAsync<SuggestionPost>(page.Id);
            return archive.Posts.Select(p => Convert(query.Id, p));
        }

        private static bool IsValidType(ContentTypes type, ContentBase content)
        {
            return content?.TypeId == GetType(type);
        }
        
        private static string GetType(ContentTypes type)
        {
            return type switch
            {
                ContentTypes.DQ => nameof(DatasetPage),
                ContentTypes.MQ => nameof(DatasetPage),
                ContentTypes.PN => nameof(NewDatasetSuggestionsPage),
                ContentTypes.O => nameof(GeneralSuggestionsPage),
                _ => string.Empty
            };
        }

        private static string GetSlug(string id, ContentTypes type)
        {
            return type switch
            {
                ContentTypes.PN => NewDatasetSuggestionsPage.WellKnownSlug,
                ContentTypes.O => GeneralSuggestionsPage.WellKnownSlug,
                _ => Utils.GenerateSlug(id)
            };
        }

        private static SuggestionDto Convert(string datasetUri, SuggestionPost p)
        {
            return new SuggestionDto
            {
                State = p.Suggestion.State.Value,
                OrgToUri = p.Suggestion.OrgToUri,
                Type = p.Suggestion.Type.Value,
                Description = p.Suggestion.Description,
                UserId = p.Suggestion.UserId,
                UserOrgUri = p.Suggestion.UserOrgUri,
                DatasetUri = datasetUri,
                Title = p.Title
            };
        }

        [HttpPost]
        [Route("")]
        public async Task<IResult> Save(SuggestionDto dto)
        {
            var archiveId = await GetArchiveGuidAsync(dto);
            var post = await api.Posts.CreateAsync<SuggestionPost>();
            post.Title = dto.Title;
            post.Suggestion = new SuggestionRegion
            {
                Description = dto.Description,
                UserId = dto.UserId,
                UserOrgUri = dto.UserOrgUri,
                OrgToUri = dto.OrgToUri,
                Type = new SelectField<ContentTypes>
                {
                    Value = dto.Type
                },
                State = new SelectField<SuggestionStates>
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

        private async Task<Guid> GetArchiveGuidAsync(SuggestionDto dto)
        {
            var page = await api.Pages.GetBySlugAsync(GetSlug(dto.DatasetUri, dto.Type));
            return page?.Id ?? (await CreatePage(dto)).Id;
        }

        private async Task<PageBase> CreatePage(SuggestionDto dto)
        {
            PageBase newPage = dto.Type switch
            {
                ContentTypes.DQ => await api.Pages.CreateAsync<DatasetPage>(),
                ContentTypes.MQ => await api.Pages.CreateAsync<DatasetPage>(),
                ContentTypes.PN => await api.Pages.CreateAsync<NewDatasetSuggestionsPage>(),
                ContentTypes.O => await api.Pages.CreateAsync<GeneralSuggestionsPage>(),
                _ => await api.Pages.CreateAsync<GeneralSuggestionsPage>()
            };

            newPage.Title = GetSlug(dto.DatasetUri, dto.Type);
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