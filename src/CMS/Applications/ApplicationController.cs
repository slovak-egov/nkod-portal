using CMS.Comments;
using CMS.Likes;
using CMS.Suggestions;
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
		public async Task<IEnumerable<ApplicationDto>> GetByDataset(string datasetUri, int? pageNumber, int? pageSize)
		{
			var page = await api.Pages.GetBySlugAsync(ApplicationsPage.WellKnownSlug);
			var archive = await api.Archives.GetByIdAsync<ApplicationPost>(page.Id);
			int pn = 0;
			int ps = 100000;

            if (string.IsNullOrEmpty(datasetUri))
            {
                if (pageNumber != null || pageSize != null)
                {
                    pn = (pageNumber != null) ? pageNumber.Value : pn;
                    ps = (pageSize != null) ? pageSize.Value : ps;

                    return archive.Posts.Select(p => Convert(p)).OrderByDescending(c => c.Created).Skip(pn * ps).Take(ps);
				}
                else 
                {
					return archive.Posts.Select(p => Convert(p)).OrderByDescending(c => c.Created);
				}
            }
            else
            {
                if (pageNumber != null || pageSize != null)
                {
                    pn = (pageNumber != null) ? pageNumber.Value : pn;
                    ps = (pageSize != null) ? pageSize.Value : ps;

                    return archive.Posts.Select(p => Convert(p)).Where(p => p.DatasetURIs.Contains(datasetUri)).OrderByDescending(c => c.Created).Skip(pn * ps).Take(ps);
				}
                else 
                {
					return archive.Posts.Select(p => Convert(p)).Where(p => p.DatasetURIs.Contains(datasetUri)).OrderByDescending(c => c.Created);
				}
            }
		} 

		[HttpGet("{id}")]
		public async Task<ApplicationDto> GetByID(Guid id)
		{
			var post = await api.Posts.GetByIdAsync<ApplicationPost>(id);
            return Convert(post);
		}

		private static ApplicationDto Convert(ApplicationPost p)
        {
            return new ApplicationDto
            {
                Id = p.Id,				
				Created = p.Created,
                Updated = p.LastModified,
                CommentCount = p.CommentCount,
				LikeCount = (p.Application.Likes?.Value != null) ? p.Application.Likes.Value.Count() : 0,
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
				LogoFileName = p.Application.LogoFileName,
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
                LogoFileName = dto.LogoFileName,
                ContactName = dto.ContactName,
                ContactSurname = dto.ContactSurname,
                ContactEmail = dto.ContactEmail,
                DatasetURIs = (dto.DatasetURIs != null) ? new MultiSelectField<string> { Value = dto.DatasetURIs } : null,
				Likes = new MultiSelectField<Guid>()
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

        [HttpPut("{id}")]
        public async Task<IResult> Update(Guid id, ApplicationDto dto)
        {
            var post = await api.Posts.GetByIdAsync<ApplicationPost>(id);
            post.Title = dto.Title;

			post.Application.UserId = dto.UserId.ToString("D");
			post.Application.Description = dto.Description;
            post.Application.Type.Value = dto.Type;
            post.Application.Theme.Value = dto.Theme;
            post.Application.Url = dto.Url;
            post.Application.Logo = dto.Logo;
			post.Application.LogoFileName = dto.LogoFileName;
			post.Application.ContactName = dto.ContactName;
            post.Application.ContactSurname = dto.ContactSurname;
            post.Application.ContactEmail = dto.ContactEmail;
            post.Application.DatasetURIs = (dto.DatasetURIs != null) ? new MultiSelectField<string> { Value = dto.DatasetURIs } : null;

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

		[HttpPost]
		[Route("likes")]
		public async Task<IResult> AddLike(LikeDto dto)
		{
			var post = await api.Posts.GetByIdAsync<ApplicationPost>(dto.ContentId);

			if (post.Application.Likes?.Value != null)
			{
				var likes = post.Application.Likes.Value.ToList();

				if (likes.Contains(dto.UserId))
				{
					return Results.Conflict("Attempt to add next like by same user!");
				}
				else
				{
					likes.Add(dto.UserId);
					post.Application.Likes.Value = likes;
				}
			}
			else
			{
				List<Guid> userIds = new List<Guid>();
				userIds.Add(dto.UserId);
				post.Application.Likes = new MultiSelectField<Guid> { Value = userIds };
			}

			await api.Posts.SaveAsync(post);
			return Results.Ok();
		}
	}
}