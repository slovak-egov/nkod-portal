using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;

namespace CMS.Applications
{
    [Route("applications")]
    [ApiController]
    [AllowAnonymous]
    public class ApplicationController
    {
        private readonly IApi api;

        public ApplicationController(IApi api)
        {
            this.api = api;
        }
        
        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<ApplicationDto>> Get()
        {
            var id = await GetArchiveGuidAsync();
            var archive = await api.Archives.GetByIdAsync<ApplicationPost>(id);
            return archive.Posts.Select(Convert);
        }
        
        private static ApplicationDto Convert(ApplicationPost p)
        {
            return new ApplicationDto
            {
                Type = p.Application.Type.Value,
                Description = p.Application.Description,
                Title = p.Title,
                OwnerName = p.Application.OwnerName,
                OwnerSurname = p.Application.OwnerSurname,
                OwnerEmail = p.Application.OwnerEmail,
                Url = p.Application.Url
            };
        }

        [HttpPost]
        [Route("")]
        public async Task<IResult> Save(ApplicationDto dto)
        {
            var archiveId = await GetArchiveGuidAsync();
            var post = await api.Posts.CreateAsync<ApplicationPost>();
            post.Title = dto.Title;
            post.Application = new ApplicationRegion
            {
                Description = dto.Description,
                Url = dto.Url,
                OwnerName = dto.OwnerName,
                OwnerSurname = dto.OwnerSurname,
                OwnerEmail = dto.OwnerEmail,
                Type = new SelectField<ApplicationTypes>
                {
                    Value = dto.Type
                }
            };

            post.Category = new Taxonomy
            {
                Title = "Application",
                Slug = "application",
                Type = TaxonomyType.Category
            };
            post.BlogId = archiveId;
            post.Published = DateTime.Now;

            await api.Posts.SaveAsync(post);
            return Results.Ok();
        }

        private async Task<Guid> GetArchiveGuidAsync()
        {
            var page = await api.Pages.GetBySlugAsync(ApplicationsPage.WellKnownSlug);
            return page?.Id ?? (await CreatePage()).Id;
        }

        private async Task<ApplicationsPage> CreatePage()
        {
            var newPage = await api.Pages.CreateAsync<ApplicationsPage>();

            newPage.Title = ApplicationsPage.WellKnownSlug;
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