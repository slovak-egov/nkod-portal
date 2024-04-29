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
	[Authorize(AuthenticationSchemes = "Bearer", Policy = "MustBeAuthenticated")]
	public class ApplicationController
    {
        private readonly IApi api;

        public ApplicationController(IApi api)
        {
            this.api = api;
        }
		
		[HttpGet]
		[Route("")]
		public async Task<ApplicationSearchResponse> Get(string datasetUri, int? pageNumber, int? pageSize)
		{
			var page = await api.Pages.GetBySlugAsync(ApplicationsPage.WellKnownSlug);
			var archive = await api.Archives.GetByIdAsync<ApplicationPost>(page.Id);
			int pn = 0;
			int ps = 100000;
			PaginationMetadata paginationMetadata = null;
			IEnumerable<ApplicationPost> res = archive.Posts;

			if (!string.IsNullOrEmpty(datasetUri))
			{
				res = res.Where(p => p.Application.DatasetURIs != null && 
				p.Application.DatasetURIs.Value != null && 
				p.Application.DatasetURIs.Value.Contains(datasetUri));
			}

			res = res.OrderByDescending(c => c.Created);

			if (pageNumber != null || pageSize != null)
			{
				pn = (pageNumber != null) ? pageNumber.Value : pn;
				ps = (pageSize != null) ? pageSize.Value : ps;

				paginationMetadata = new PaginationMetadata()
				{
					TotalItemCount = res.Count(),
					CurrentPage = pn,
					PageSize = ps
				};

				res = res.Skip(pn * ps).Take(ps);
			}

            return new ApplicationSearchResponse()
            {
                Items = res.Select(p => Convert(p)),
                PaginationMetadata = paginationMetadata
            };
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
				UserEmail = p.Application.UserEmail,
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
		[Route("search")]
		public async Task<ApplicationSearchResponse> Search(ApplicationSearchRequest filter)
		{
			var page = await api.Pages.GetBySlugAsync(ApplicationsPage.WellKnownSlug);
			var archive = await api.Archives.GetByIdAsync<ApplicationPost>(page.Id);
			int pn = 0;
			int ps = 100000;
			PaginationMetadata paginationMetadata = null;
			IEnumerable<ApplicationPost> res = archive.Posts;

			if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
			{
				var searchQuery = filter.SearchQuery.Trim();
				res = res.Where(p => p.Title.Contains(searchQuery)
					|| (p.Application.Description != null && p.Application.Description.Value != null && p.Application.Description.Value.Contains(searchQuery)));
			}

			if (filter.Types != null)
			{
				res = res.Where(p => filter.Types.Contains(p.Application.Type.Value));
			}

			if (filter.Themes != null)
			{
				res = res.Where(p => filter.Themes.Contains(p.Application.Theme.Value));
			}

			if (filter.OrderBy != null)
			{
				switch (filter.OrderBy)
				{
					case OrderByTypes.Created:
						{
							res = res.OrderByDescending(p => p.Created);
							break;
						};
					case OrderByTypes.Updated:
						{
							res = res.OrderByDescending(p => p.LastModified);
							break;
						};
					case OrderByTypes.Title:
						{
							res = res.OrderBy(p => p.Title);
							break;
						};
					case OrderByTypes.Popularity:
						{
							res = res.OrderByDescending(p => (p.Application != null && p.Application.Likes != null && p.Application.Likes.Value != null) ? p.Application.Likes.Value.Count() : 0);
							break;
						}
				}
			}
			else
			{
				res = res.OrderByDescending(c => c.Created);
			}

			if (filter.PageNumber != null || filter.PageSize != null)
			{
				pn = (filter.PageNumber != null) ? filter.PageNumber.Value : pn;
				ps = (filter.PageSize != null) ? filter.PageSize.Value : ps;

				paginationMetadata = new PaginationMetadata()
				{
					TotalItemCount = res.Count(),
					CurrentPage = pn,
					PageSize = ps
				};

				res = res.Skip(pn * ps).Take(ps);
			}

			return new ApplicationSearchResponse()
			{
				Items = res.Select(p => Convert(p)),
				PaginationMetadata = paginationMetadata
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
				UserEmail = dto.UserEmail,
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
            return Results.Ok<Guid>(post.Id);
        }

        [HttpPut("{id}")]
        public async Task<IResult> Update(Guid id, ApplicationDto dto)
        {
            var post = await api.Posts.GetByIdAsync<ApplicationPost>(id);
            post.Title = dto.Title;

			post.Application.UserId = dto.UserId.ToString("D");
			post.Application.UserEmail = dto.UserEmail;
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

		[HttpDelete("{id}")]
		public async Task<IResult> Delete(Guid id)
		{
			await api.Posts.DeleteAsync(id);
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