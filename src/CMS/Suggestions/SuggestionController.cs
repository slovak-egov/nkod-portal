using CMS.Comments;
using CMS.Likes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;
using System.Linq;
using System.Text.Json.Serialization;

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
		public async Task<SuggestionSearchResponse> Get(string datasetUri, int? pageNumber, int? pageSize)
		{
			var page = await api.Pages.GetBySlugAsync(SuggestionsPage.WellKnownSlug);
			var archive = await api.Archives.GetByIdAsync<SuggestionPost>(page.Id);
			int pn = 0;
            int ps = 100000;
			PaginationMetadata paginationMetadata = null;
            IEnumerable<SuggestionPost> res = archive.Posts;

			if (!string.IsNullOrEmpty(datasetUri))
            {
				res = res.Where(p => p.Suggestion.DatasetUri == datasetUri);
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

			return new SuggestionSearchResponse()
			{
				Items = res.Select(p => Convert(p)),
				PaginationMetadata = paginationMetadata
			};
		}     
        
		[HttpGet("{id}")]		
		public async Task<SuggestionDto> GetByID(Guid id)
		{
			var post = await api.Posts.GetByIdAsync<SuggestionPost>(id);
			return Convert(post);
		}

		private static SuggestionDto Convert(SuggestionPost p)
        {
            return new SuggestionDto
			{
                Id = p.Id,
				Created = p.Created,
				Updated = p.LastModified,
                CommentCount = p.CommentCount,
                LikeCount = (p.Suggestion.Likes?.Value != null) ? p.Suggestion.Likes.Value.Count() : 0,
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
		[Route("search")]
		public async Task<SuggestionSearchResponse> Search(SuggestionSearchRequest filter)
		{
			var page = await api.Pages.GetBySlugAsync(SuggestionsPage.WellKnownSlug);
			var archive = await api.Archives.GetByIdAsync<SuggestionPost>(page.Id);
			int pn = 0;
			int ps = 100000;
			PaginationMetadata paginationMetadata = null;
			IEnumerable<SuggestionPost> res = archive.Posts;

			if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
			{
				var searchQuery = filter.SearchQuery.Trim();
				res = res.Where(p => p.Title.Contains(searchQuery)
					|| (p.Suggestion.Description != null && p.Suggestion.Description.Value != null && p.Suggestion.Description.Value.Contains(searchQuery)));
			}

			if (filter.OrgToUris != null)
			{
				res = res.Where(p => filter.OrgToUris.ToList().Contains(p.Suggestion.OrgToUri));
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

			return new SuggestionSearchResponse()
			{
				Items = res.Select(p => Convert(p)),
				PaginationMetadata = paginationMetadata
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
                },
                Likes = new MultiSelectField<Guid>()
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

		[HttpPut("{id}")]
		public async Task<IResult> Update(Guid id, SuggestionDto dto)
		{
			var post = await api.Posts.GetByIdAsync<SuggestionPost>(id);
			
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

		[HttpDelete("{id}")]
		public async Task<IResult> Delete(Guid id)
		{
			await api.Posts.DeleteAsync(id);
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

		[HttpPost]
		[Route("likes")]
		public async Task<IResult> AddLike(LikeDto dto)
		{
			var post = await api.Posts.GetByIdAsync<SuggestionPost>(dto.ContentId);

            if (post.Suggestion.Likes?.Value != null)
            {
                var likes = post.Suggestion.Likes.Value.ToList();

                if (likes.Contains(dto.UserId))
                {
                    return Results.Problem("Attempt to add next like by same user!");
                }
                else
                {
                    likes.Add(dto.UserId);
                    post.Suggestion.Likes.Value = likes;
                }
            }
            else 
            {
                List<Guid> userIds = new List<Guid>();
                userIds.Add(dto.UserId);
				post.Suggestion.Likes = new MultiSelectField<Guid> { Value = userIds };
			}

			await api.Posts.SaveAsync(post);
			return Results.Ok();
		}
	}
}