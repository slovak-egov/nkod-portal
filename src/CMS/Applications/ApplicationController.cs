using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;
using System.Text.Json.Serialization;

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
                Id = p.Id,
                Type = p.Application.Type.Value,
                Theme = p.Application.Theme.Value,
                Description = p.Application.Description,
                Title = p.Title,
                ContactName = p.Application.ContactName,
                ContactSurname = p.Application.ContactSurname,
                ContactEmail = p.Application.ContactEmail,
                Url = p.Application.Url,
                Logo = p.Application.Logo,
                DatasetURIs = (p.Application.DatasetURIs != null) ? p.Application.DatasetURIs.Select(d => d.Value).ToList() : null
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
                Type = new SelectField<ApplicationTypes>
                {
                    Value = dto.Type    
                },
				Theme = new SelectField<ApplicationThemes>
				{
					Value = dto.Theme
				},
                Url = dto.Url,
				Logo = dto.Logo,
				ContactName = dto.ContactName,
				ContactSurname = dto.ContactSurname,
				ContactEmail = dto.ContactEmail,
				DatasetURIs = (dto.DatasetURIs != null) ? dto.DatasetURIs.Select(d => (StringField)d).ToList() : null
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