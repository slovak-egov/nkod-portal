using CMS.Applications;
using CMS.Comments;
using CMS.Datasets;
using CMS.Likes;
using DocumentStorageClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NkodSk.Abstractions;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CMS.Suggestions
{
    [Route("suggestions")]
    [ApiController]
	public class SuggestionController : ControllerBase
	{
        private readonly IApi api;

		private readonly INotificationService notificationService;

        private readonly IDocumentStorageClient documentStorageClient;

        public SuggestionController(IApi api, INotificationService notificationService, IDocumentStorageClient documentStorageClient)
        {
            this.api = api;
			this.notificationService = notificationService;
			this.documentStorageClient = documentStorageClient;
        }       
        
		[HttpGet]
		[Route("")]
		public async Task<SuggestionSearchResponse> Get(string datasetUri, int? pageNumber, int? pageSize)
		{
			int pn = 0;
            int ps = 100000;
			PaginationMetadata paginationMetadata = null;

			var blogId = await GetBlogGuidAsync();
			IEnumerable<SuggestionPost> res = await api.Posts.GetAllAsync<SuggestionPost>(blogId);

			if (!string.IsNullOrEmpty(datasetUri))
            {
				res = res.Where(p => p.Suggestion.DatasetUri == datasetUri);
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

			return new SuggestionSearchResponse()
			{
				Items = res.Select(p => Convert(p)),
				PaginationMetadata = paginationMetadata
			};
		}     
        
		[HttpGet("{id}")]		
		public async Task<ActionResult<SuggestionDto>> GetByID(Guid id)
		{
			var post = await api.Posts.GetByIdAsync<SuggestionPost>(id);

			if (post == null)
			{
				return NotFound();
			}
			else
			{
				return Convert(post);
			}
		}

		private static SuggestionDto Convert(SuggestionPost p)
        {
            return new SuggestionDto
			{
                Id = p.Id,
				Created = p.Published.Value,
				Updated = (p.Suggestion.Updated != null && p.Suggestion.Updated.Value > DateTime.MinValue) ? p.Suggestion.Updated.Value: p.Published.Value,
				CommentCount = p.CommentCount,
                LikeCount = (p.Suggestion.Likes?.Value != null) ? p.Suggestion.Likes.Value.Count() : 0,
				Status = p.Suggestion.Status.Value,
                OrgToUri = p.Suggestion.OrgToUri,
                Type = p.Suggestion.Type.Value,
                Description = p.Suggestion.Description,
                UserId = Guid.Parse(p.Suggestion.UserId.Value),
				UserFormattedName = p.Suggestion.UserFormattedName,
                UserOrgUri = p.Suggestion.UserOrgUri,
                DatasetUri = p.Suggestion.DatasetUri.Value,
                Title = p.Title
            };
        }

		[HttpPost]
		[Route("search")]
		public async Task<SuggestionSearchResponse> Search(SuggestionSearchRequest filter)
		{			
			int pn = 0;
			int ps = 100000;
			PaginationMetadata paginationMetadata = null;

			var blogId = await GetBlogGuidAsync();
			IEnumerable<SuggestionPost> res = await api.Posts.GetAllAsync<SuggestionPost>(blogId);
			
			if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
			{
				var searchQuery = filter.SearchQuery.Trim();
				res = res.Where(p => p.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
					|| (p.Suggestion.Description != null && p.Suggestion.Description.Value != null && p.Suggestion.Description.Value.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));
			}

			if (filter.OrgToUris != null)
			{
				res = res.Where(p => filter.OrgToUris.Select(u => u.ToUpper()).Contains(p.Suggestion.OrgToUri.Value.ToUpper()));
			}

			if (filter.Types != null)
			{
				res = res.Where(p => filter.Types.Contains(p.Suggestion.Type.Value));
			}

			if (filter.Statuses != null)
			{
				res = res.Where(p => filter.Statuses.Contains(p.Suggestion.Status.Value));
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
							res = res.OrderByDescending(p => p.Suggestion.Updated);
							break;
						};
					case OrderByTypes.Title:
						{
							res = res.OrderBy(p => p.Title);
							break;
						};
					case OrderByTypes.Popularity:
						{
							res = res.OrderByDescending(p => (p.Suggestion != null && p.Suggestion.Likes != null && p.Suggestion.Likes.Value != null) ? p.Suggestion.Likes.Value.Count() : 0);
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

			return new SuggestionSearchResponse()
			{
				Items = res.Select(p => Convert(p)),
				PaginationMetadata = paginationMetadata
			};
		}

		[HttpPost]
        [Route("")]
		[Authorize]
		public async Task<IResult> Save(SuggestionDto dto)
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
            var post = await api.Posts.CreateAsync<SuggestionPost>();
            post.Title = dto.Title;
            post.Suggestion = new SuggestionRegion
            {
                Description = dto.Description,
                UserId = dto.UserId.ToString("D"),
				UserEmail = userEmail,
				UserFormattedName = userFormattedName,
                UserOrgUri = dto.UserOrgUri,
                OrgToUri = dto.OrgToUri,
                DatasetUri = dto.DatasetUri,
                Type = new SelectField<ContentTypes>
                {
                    Value = dto.Type
                },
                Status =  new SelectField<SuggestionStates>
                {
                    Value = SuggestionStates.C
				},
                Likes = new MultiSelectField<Guid>(), 
				Updated = new CustomField<DateTime> { Value = DateTime.UtcNow }
			};

            post.Category = new Taxonomy
            {
                Title = "Suggestion",
                Slug = "suggestion",
                Type = TaxonomyType.Category
            };
            post.BlogId = blogId;
            post.Published = DateTime.UtcNow;
			post.Slug = String.Format("{0}-{1}-{2:yyyy-MM-dd-HH-mm-ss-fff}", 
				post.Category.Slug, 
				SlugUtil.Slugify(post.Title), 
				post.Published.Value);			

			await api.Posts.SaveAsync(post);

            string publisherEmail = await documentStorageClient.GetEmailForPublisher(dto.OrgToUri);
            if (!string.IsNullOrEmpty(publisherEmail))
            {
                string suggestionUrl = $"/podnet/{post.Id}";

                notificationService.Notify(publisherEmail, suggestionUrl, post.Title, $"Bol pridaný nový podnet", new List<string> { post.Id.ToString() });
            }

            return Results.Ok<Guid>(post.Id);
        }

		[HttpPut("{id}")]
		[Authorize]
		public async Task<ActionResult> Update(Guid id, SuggestionDto dto)
		{
			ClaimsPrincipal user = HttpContext.User;
			Uri userPublisher = null;
			Uri orgToUri = null;

			if (user == null)
			{
				return Forbid();
			}			

			Guid userId = Guid.Parse(user?.Claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value);
			var post = await api.Posts.GetByIdAsync<SuggestionPost>(id);

			if (post == null)
			{
				return NotFound();
			}

			SuggestionStates originalState = post.Suggestion.Status.Value;

			if (user.IsInRole("Publisher") || user.IsInRole("PublisherAdmin"))
			{
				string publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;

				if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out userPublisher))
				{
					userPublisher = null;
				}

				if (string.IsNullOrEmpty(post.Suggestion.OrgToUri) || !Uri.TryCreate(post.Suggestion.OrgToUri, UriKind.Absolute, out orgToUri))
				{
					orgToUri = null;
				}
			}			

			if (!(user.IsInRole("Superadmin") 
				||
				((user.IsInRole("Publisher") || user.IsInRole("PublisherAdmin")) && 
				((userId == Guid.Parse(post.Suggestion.UserId.Value) &&
				post.Suggestion.Status.Value == SuggestionStates.C) || 
				(userPublisher != null && orgToUri !=null && userPublisher == orgToUri))) 
				||
				(user.IsInRole("CommunityUser") && 
				userId == Guid.Parse(post.Suggestion.UserId.Value) && 
				post.Suggestion.Status.Value == SuggestionStates.C)
				))
			{
				return Forbid();
			}

			if (user.IsInRole("Superadmin"))
			{
				post.Title = dto.Title;
				post.Suggestion.Description = dto.Description;
				post.Suggestion.OrgToUri = dto.OrgToUri;
				post.Suggestion.DatasetUri = dto.DatasetUri;
				post.Suggestion.Type.Value = dto.Type;
				post.Suggestion.Status.Value = dto.Status;
			}
			else if (user.IsInRole("CommunityUser"))
			{
				post.Title = dto.Title;
				post.Suggestion.Description = dto.Description;
				post.Suggestion.OrgToUri = dto.OrgToUri;
				post.Suggestion.DatasetUri = dto.DatasetUri;
				post.Suggestion.Type.Value = dto.Type;
			}
			if (user.IsInRole("Publisher") || user.IsInRole("PublisherAdmin"))
			{
				if (userPublisher != null && orgToUri != null && userPublisher == orgToUri)
				{
					post.Suggestion.Status.Value = dto.Status;
				}
				else 
				{
					post.Title = dto.Title;
					post.Suggestion.Description = dto.Description;
					post.Suggestion.OrgToUri = dto.OrgToUri;
					post.Suggestion.DatasetUri = dto.DatasetUri;
					post.Suggestion.Type.Value = dto.Type;
				}
			}
			
			post.Suggestion.Updated.Value = DateTime.UtcNow;

			await api.Posts.SaveAsync(post);


			if (post.Suggestion.Status.Value != originalState)
			{
				string stateAsText = post.Suggestion.Status.Value switch
				{
					SuggestionStates.C => "zaevidovaný",
					SuggestionStates.P => "v riešení",
					SuggestionStates.R => "vyriešený",
					_ => "",
				};

                HashSet<string> usersToNotify = new HashSet<string> { post.Suggestion.UserEmail };

                foreach (Comment c in await api.Posts.GetAllCommentsAsync(postId: post.Id, onlyApproved: false))
                {
                    usersToNotify.Add(c.Email);
                }

                string suggestionUrl = $"/podnet/{post.Id}";

                foreach (string email in usersToNotify)
				{
                    notificationService.Notify(email, suggestionUrl, post.Title, $"Stav podnetu bol zmenený na {stateAsText}", new List<string> { post.Id.ToString() });
                }
			}

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
			var post = await api.Posts.GetByIdAsync<SuggestionPost>(id);

			if (post == null)
			{
				return NotFound();
			}

			if (!(user.IsInRole("Superadmin") ||
				((user.IsInRole("Publisher") ||
				user.IsInRole("PublisherAdmin") ||
				user.IsInRole("CommunityUser")
				) && (userId == Guid.Parse(post.Suggestion.UserId.Value) && post.Suggestion.Status.Value == SuggestionStates.C))
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
            var page = await api.Pages.GetBySlugAsync(SuggestionsPage.WellKnownSlug);
            return page?.Id ?? (await CreatePage()).Id;
        }

        private async Task<PageBase> CreatePage()
        {
            PageBase newPage = await api.Pages.CreateAsync<SuggestionsPage>();

            newPage.Title = SuggestionsPage.WellKnownSlug;
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

			var post = await api.Posts.GetByIdAsync<SuggestionPost>(dto.ContentId);

            if (post.Suggestion.Likes?.Value != null)
            {
                var likes = post.Suggestion.Likes.Value.ToList();

				if (likes.Contains(dto.UserId))
				{
					likes.Remove(dto.UserId);
				}
				else
				{
					likes.Add(dto.UserId);
				}

				post.Suggestion.Likes.Value = likes;
			}
            else 
            {
                List<Guid> userIds = new List<Guid>();
                userIds.Add(dto.UserId);
				post.Suggestion.Likes = new MultiSelectField<Guid> { Value = userIds };
			}

			await api.Posts.SaveAsync(post);
			return Results.Ok<SuggestionDto>(Convert(post));
		}
	}
}