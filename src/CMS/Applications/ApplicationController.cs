﻿using CMS.Comments;
using CMS.Likes;
using CMS.Suggestions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;
using CMS.Datasets;
using System.Security.Claims;
using DocumentStorageClient;
using NkodSk.Abstractions;
using System.Data;

namespace CMS.Applications
{
    [Route("applications")]
    [ApiController]
	public class ApplicationController : ControllerBase
    {
        private readonly IApi api;

        private readonly INotificationService notificationService;

        private readonly IDocumentStorageClient documentStorageClient;

        public ApplicationController(IApi api, INotificationService notificationService, IDocumentStorageClient documentStorageClient)
        {
            this.api = api;
			this.notificationService = notificationService;
			this.documentStorageClient = documentStorageClient;
        }
		
		[HttpGet]
		[Route("")]		
		public async Task<ApplicationSearchResponse> Get(string datasetUri, int? pageNumber, int? pageSize)
		{
			int pn = 0;
			int ps = 100000;
			PaginationMetadata paginationMetadata = null;

			var blogId = await GetBlogGuidAsync();
			IEnumerable<ApplicationPost> res = await api.Posts.GetAllAsync<ApplicationPost>(blogId);

			if (!string.IsNullOrEmpty(datasetUri))
			{
				res = res.Where(p => p.Application.DatasetURIs != null && 
				p.Application.DatasetURIs.Value != null && 
				p.Application.DatasetURIs.Value.Contains(datasetUri));
			}

			res = res.OrderByDescending(c => c.Published);

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
		public async Task<ActionResult<ApplicationDto>> GetByID(Guid id)
		{
			var post = await api.Posts.GetByIdAsync<ApplicationPost>(id);

			if (post == null)
			{
				return NotFound();
			}
			else
			{
				return Convert(post);
			}
		}

		private static ApplicationDto Convert(ApplicationPost p)
        {
            return new ApplicationDto
            {
                Id = p.Id,				
				Created = p.Published.Value,
				Updated = (p.Application.Updated != null && p.Application.Updated.Value > DateTime.MinValue) ? p.Application.Updated.Value : p.Published.Value,
				CommentCount = p.CommentCount,
				LikeCount = (p.Application.Likes?.Value != null) ? p.Application.Likes.Value.Count() : 0,
				UserId = (p.Application.UserId.Value != null) ? Guid.Parse(p.Application.UserId.Value) : Guid.Empty,
				UserFormattedName = p.Application.UserFormattedName,
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
			int pn = 0;
			int ps = 100000;
			PaginationMetadata paginationMetadata = null;

			var blogId = await GetBlogGuidAsync();
			IEnumerable<ApplicationPost> res = await api.Posts.GetAllAsync<ApplicationPost>(blogId);

			if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
			{
				var searchQuery = filter.SearchQuery.Trim();
				res = res.Where(p => p.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
					|| (p.Application.Description != null && p.Application.Description.Value != null && p.Application.Description.Value.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));
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
							res = res.OrderByDescending(p => p.Published);
							break;
						};
					case OrderByTypes.Updated:
						{
							res = res.OrderByDescending(p => p.Application.Updated);
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
				res = res.OrderByDescending(c => c.Published);
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
		[Authorize]
		public async Task<IResult> Save(ApplicationDto dto)
        {
			ClaimsPrincipal user = HttpContext.User;
			Guid userId = Guid.Parse(user?.Claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value);
            string userEmail = userId + "@data.slovensko.sk";
            string userFormattedName = user?.FindFirstValue(ClaimTypes.Name);

            if (user == null)
			{
				return Results.Forbid();
			}
			
			if (!((user.IsInRole("Superadmin") ||
				user.IsInRole("Publisher") ||
				user.IsInRole("PublisherAdmin") ||
				user.IsInRole("CommunityUser")
				) && userId == dto.UserId))
			{
				return Results.Forbid();
			}

			var blogId = await GetBlogGuidAsync();
            var post = await api.Posts.CreateAsync<ApplicationPost>();
            post.Title = dto.Title;
            post.Application = new ApplicationRegion
            {
				UserId = dto.UserId.ToString("D"),
				UserEmail = userEmail,
				UserFormattedName = userFormattedName,
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
				Likes = new MultiSelectField<Guid>(),
				Updated = new CustomField<DateTime> { Value = DateTime.UtcNow }
			};

            post.Category = new Taxonomy
            {
                Title = "Application",
                Slug = "application",
                Type = TaxonomyType.Category
            };
            post.BlogId = blogId;
            post.Published = DateTime.UtcNow;
            post.EnableComments = true;
			post.Slug = String.Format("{0}-{1}-{2:yyyy-MM-dd-HH-mm-ss-fff}",
				post.Category.Slug,
				SlugUtil.Slugify(post.Title),
				post.Published.Value);

			await api.Posts.SaveAsync(post);

			HashSet<string> emails = new HashSet<string>();

			if (dto.DatasetURIs is not null)
			{
				foreach (string uri in dto.DatasetURIs)
				{
                    (string publisherEmail, _, _) = await documentStorageClient.GetEmailForDataset(uri);
                    if (!string.IsNullOrEmpty(publisherEmail))
                    {
						emails.Add(publisherEmail);
                    }
                }
            }

			foreach (string email in emails)
			{
                string commentUrl = $"/aplikacia/{post.Id}";

                notificationService.Notify(email, commentUrl, post.Title, $"Nová aplikácia využíva Váš dataset", new List<string> { post.Id.ToString() });
            }

            return Results.Ok<Guid>(post.Id);
        }

        [HttpPut("{id}")]
		[Authorize]
		public async Task<ActionResult> Update(Guid id, ApplicationDto dto)
        {
			ClaimsPrincipal user = HttpContext.User;					

			if (user == null)
			{
				return Forbid();
			}

			Guid userId = Guid.Parse(user?.Claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value);
			var post = await api.Posts.GetByIdAsync<ApplicationPost>(id);

			if (post == null)
			{
				return NotFound();
			}

			if (!(user.IsInRole("Superadmin") || 
				((user.IsInRole("Publisher") ||
				user.IsInRole("PublisherAdmin") ||
				user.IsInRole("CommunityUser")
				) && userId == Guid.Parse(post.Application.UserId.Value))
				))
			{
				return Forbid();
			}

			post.Title = dto.Title;
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
			post.Application.Updated.Value = DateTime.UtcNow;

			await api.Posts.SaveAsync(post);
            return Ok();
        }

		[HttpDelete("{id}")]
		[Authorize]
		public async Task<ActionResult> Delete(Guid id)
		{
			ClaimsPrincipal user = HttpContext.User;			

			if (user == null)
			{
				return Forbid();
			}

			Guid userId = Guid.Parse(user?.Claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value);
			var post = await api.Posts.GetByIdAsync<ApplicationPost>(id);

			if (post == null)
			{
				return NotFound();
			}

			if (!(user.IsInRole("Superadmin") ||
				((user.IsInRole("Publisher") ||
				user.IsInRole("PublisherAdmin") ||
				user.IsInRole("CommunityUser")
				) && userId == Guid.Parse(post.Application.UserId.Value))
				))
			{
				return Forbid();
			}

			await api.Posts.DeleteAsync(id);

			notificationService.Delete(post.Id.ToString());

			return Ok();
		}

		private async Task<Guid> GetBlogGuidAsync()
        {
            var page = await api.Pages.GetBySlugAsync(ApplicationsPage.WellKnownSlug);
            return page?.Id ?? (await CreatePage()).Id;
        }

        private async Task<ApplicationsPage> CreatePage()
        {
            var newPage = await api.Pages.CreateAsync<ApplicationsPage>();

            newPage.Title = ApplicationsPage.WellKnownSlug;
            newPage.EnableComments = true;
            newPage.Published = DateTime.UtcNow;
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
		[Authorize]
		public async Task<IResult> AddRemoveLike(LikeDto dto)
		{
			ClaimsPrincipal user = HttpContext.User;
			Guid userId = Guid.Parse(user?.Claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value);

			if (user == null)
			{
				return Results.Forbid();
			}

			if (!((user.IsInRole("Superadmin") ||
				user.IsInRole("Publisher") ||
				user.IsInRole("PublisherAdmin") ||
				user.IsInRole("CommunityUser")
				) && userId == dto.UserId))
			{
				return Results.Forbid();
			}

			var post = await api.Posts.GetByIdAsync<ApplicationPost>(dto.ContentId);

			if (post.Application.Likes?.Value != null)
			{
				var likes = post.Application.Likes.Value.ToList();

				if (likes.Contains(dto.UserId))
				{
					likes.Remove(dto.UserId);
				}
				else
				{
					likes.Add(dto.UserId);
				}

				post.Application.Likes.Value = likes;
			}
			else
			{
				List<Guid> userIds = new List<Guid>();
				userIds.Add(dto.UserId);
				post.Application.Likes = new MultiSelectField<Guid> { Value = userIds };
			}

			await api.Posts.SaveAsync(post);
			return Results.Ok<ApplicationDto>(Convert(post));
		}
	}
}