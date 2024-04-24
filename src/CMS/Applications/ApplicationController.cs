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
				Created = p.Created,
                Updated = p.LastModified,
				UserId = (p.Application.UserId.Value != null) ? Guid.Parse(p.Application.UserId.Value) : Guid.Empty,
				Type = p.Application.Type.Value,
                Theme = p.Application.Theme.Value,
                Description = p.Application.Description,
                Title = p.Title,
                ContactName = p.Application.ContactName,
                ContactSurname = p.Application.ContactSurname,
                ContactEmail = p.Application.ContactEmail,
                Url = p.Application.Url,
                Logo = p.Application.Logo,
                DatasetURIs = (p.Application.DatasetURIs?.Value != null) ? p.Application.DatasetURIs?.Value.ToList() : new List<string>()
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
				UserId = dto.UserId.ToString("D"),
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
                DatasetURIs = (dto.DatasetURIs != null) ? new MultiSelectField { Value = dto.DatasetURIs } : null
            };

            post.Category = new Taxonomy
            {
                Title = "Application",
                Slug = "application",
                Type = TaxonomyType.Category
            };
            post.BlogId = archiveId;
            post.Published = DateTime.Now;
            post.EnableComments = true;

            await api.Posts.SaveAsync(post);
            return Results.Ok();
        }

        [HttpPut]
        [Route("")]
        public async Task<IResult> Update(ApplicationDto dto)
        {
            var post = await api.Posts.GetByIdAsync<ApplicationPost>(dto.Id);
            post.Title = dto.Title;

			post.Application.UserId = dto.UserId.ToString("D");
			post.Application.Description = dto.Description;
            post.Application.Type.Value = dto.Type;
            post.Application.Theme.Value = dto.Theme;
            post.Application.Url = dto.Url;
            post.Application.Logo = dto.Logo;
            post.Application.ContactName = dto.ContactName;
            post.Application.ContactSurname = dto.ContactSurname;
            post.Application.ContactEmail = dto.ContactEmail;
            post.Application.DatasetURIs = (dto.DatasetURIs != null) ? new MultiSelectField { Value = dto.DatasetURIs } : null;

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