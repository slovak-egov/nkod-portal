using CMS.Comments;
using CMS.Likes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;
using SixLabors.ImageSharp.Formats.Gif;
using System.Linq;
using System.Text.Json.Serialization;
using CMS.Applications;
using CMS.Suggestions;
using System.Security.Claims;
using NkodSk.Abstractions;

namespace CMS.Datasets
{
    [Route("datasets")]
    [ApiController]
	public class DatasetController : ControllerBase
	{
        private readonly IApi api;

        private readonly INotificationService notificationService;

        private readonly IDocumentStorageClient documentStorageClient;

        public DatasetController(IApi api, INotificationService notificationService, IDocumentStorageClient documentStorageClient)
        {
            this.api = api;
			this.notificationService = notificationService;
			this.documentStorageClient = documentStorageClient;
        }       
        
		[HttpGet]
		[Route("")]
		public async Task<DatasetSearchResponse> Get(string datasetUri, int? pageNumber, int? pageSize)
		{			
			int pn = 0;
            int ps = 100000;
			PaginationMetadata paginationMetadata = null;

			var blogId = await GetBlogGuidAsync();
			IEnumerable<DatasetPost> res = await api.Posts.GetAllAsync<DatasetPost>(blogId);
			
			if (!string.IsNullOrEmpty(datasetUri))
			{
				res = res.Where(p => p.Title == datasetUri);
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

			return new DatasetSearchResponse()
			{
				Items = res.Select(p => Convert(p)),
				PaginationMetadata = paginationMetadata
			};
		}     
        
		[HttpGet("{id}")]		
		public async Task<ActionResult<DatasetDto>> GetByID(Guid id)
		{
			var post = await api.Posts.GetByIdAsync<DatasetPost>(id);

			if (post == null)
			{
				return NotFound();
			}
			else
			{
				return Convert(post);
			}
		}

		private static DatasetDto Convert(DatasetPost p)
        {
            return new DatasetDto
			{
                Id = p.Id,
				Created = p.Published.Value,
				Updated = (p.Dataset.Updated != null && p.Dataset.Updated.Value > DateTime.MinValue) ? p.Dataset.Updated.Value : p.Published.Value,
				CommentCount = p.CommentCount,
                LikeCount = (p.Dataset.Likes?.Value != null) ? p.Dataset.Likes.Value.Count() : 0,				
                DatasetUri = p.Title
            };
        }
        
		[HttpPost]
        [Route("")]
		[Authorize]
		public async Task<IResult> Save(DatasetDto dto)
        {
			ClaimsPrincipal user = HttpContext.User;

			if (user == null)
			{
				return Results.Forbid();
			}

			if (!(user.IsInRole("Superadmin") ||
				user.IsInRole("Publisher") ||
				user.IsInRole("PublisherAdmin") ||
				user.IsInRole("CommunityUser")
				))
			{
				return Results.Forbid();
			}

			var blogId = await GetBlogGuidAsync();
            var post = await api.Posts.CreateAsync<DatasetPost>();
			post.Title = dto.DatasetUri;
			post.Dataset = new DatasetRegion
            {
                Likes = new MultiSelectField<Guid>(),
				Updated = new CustomField<DateTime> { Value = DateTime.UtcNow }
			};

            post.Category = new Taxonomy
            {
                Title = "Dataset",
                Slug = "dataset",
                Type = TaxonomyType.Category
            };
            post.BlogId = blogId;
            post.Published = DateTime.UtcNow;
			post.Slug = String.Format("{0}-{1}-{2:yyyy-MM-dd-HH-mm-ss-fff}",
				post.Category.Slug,
				SlugUtil.Slugify(post.Title),
				post.Published.Value);

			await api.Posts.SaveAsync(post);
			return Results.Ok<Guid>(post.Id);
		}

		[HttpPut("{id}")]
		[Authorize]
		public async Task<ActionResult> Update(Guid id, DatasetDto dto)
		{
			ClaimsPrincipal user = HttpContext.User;

			if (user == null)
			{
				return Forbid();
			}

			if (!user.IsInRole("Superadmin"))
			{
				return Forbid();
			}

			var post = await api.Posts.GetByIdAsync<DatasetPost>(id);

			if (post == null)
			{
				return NotFound();
			}

			post.Title = dto.DatasetUri;
			post.Dataset.Updated.Value = DateTime.UtcNow;

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

			if (!user.IsInRole("Superadmin"))
			{
				return Forbid();
			}

			var post = await api.Posts.GetByIdAsync<DatasetPost>(id);

			if (post == null)
			{
				return NotFound();
			}

			await api.Posts.DeleteAsync(id);
			return Ok();
		}

		private async Task<Guid> GetBlogGuidAsync()
        {
            var page = await api.Pages.GetBySlugAsync(DatasetsPage.WellKnownSlug);
            return page?.Id ?? (await CreatePage()).Id;
        }

        private async Task<PageBase> CreatePage()
        {
            PageBase newPage = await api.Pages.CreateAsync<DatasetsPage>();

            newPage.Title = DatasetsPage.WellKnownSlug;
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
		public async Task<IResult> AddRemoveLike(DatasetLikeDto dto)
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

			DatasetPost post = null;

			if (dto.ContentId == null && string.IsNullOrWhiteSpace(dto.DatasetUri))
			{
				return Results.Problem("ContentId or DatasetUri must be set!");
			}

			if (dto.ContentId == null)
			{
				var blogId = await GetBlogGuidAsync();
				IEnumerable<DatasetPost> posts = await api.Posts.GetAllAsync<DatasetPost>(blogId);
				post = posts.Where(p => p.Title == dto.DatasetUri).SingleOrDefault();
				
				IResult res = null;
				Ok<Guid> resOK;

				if (post == null)
				{
					DatasetDto ds = new DatasetDto()
					{
						DatasetUri = dto.DatasetUri
					};

					res = await this.Save(ds);
					resOK = res as Ok<Guid>;

					if (resOK == null)
					{
						return res;
					}

					if (resOK.Value != Guid.Empty)
					{
						post = await api.Posts.GetByIdAsync<DatasetPost>(resOK.Value);
					}
					else
					{						
						post = posts.Where(p => p.Title == dto.DatasetUri).SingleOrDefault();
					}
				}
			}
			else
			{
				post = await api.Posts.GetByIdAsync<DatasetPost>(dto.ContentId.Value);

				if (post == null)
				{
					return Results.Problem("Dataset with this ContentId not exists!");
				}
			}
						
			if (post.Dataset.Likes?.Value != null)
            {
                var likes = post.Dataset.Likes.Value.ToList();

                if (likes.Contains(dto.UserId))
                {
					likes.Remove(dto.UserId);
                }
                else
                {
                    likes.Add(dto.UserId);
                }

				post.Dataset.Likes.Value = likes;
			}
            else 
            {
                List<Guid> userIds = new List<Guid>();
                userIds.Add(dto.UserId);
				post.Dataset.Likes = new MultiSelectField<Guid> { Value = userIds };
			}

			await api.Posts.SaveAsync(post);
			return Results.Ok<DatasetDto>(Convert(post));
		}

		[HttpPost]
		[Route("comments")]
		[Authorize]
		public async Task<IResult> AddComment(DatasetCommentDto dto)
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
			IEnumerable<DatasetPost> posts = await api.Posts.GetAllAsync<DatasetPost>(blogId);
			DatasetPost post = posts.Where(p => p.Title == dto.DatasetUri).SingleOrDefault();

            PageComment comment = null;
			IResult res = null;
			Ok<Guid> resOK;

			if (post == null)
			{ 
				DatasetDto ds = new DatasetDto()
				{
					DatasetUri = dto.DatasetUri
				};

				res = await this.Save(ds);
				resOK = res as Ok<Guid>;

				if (resOK == null) 
				{
					return res;
				}

				if (resOK.Value != Guid.Empty)
				{
					post = await api.Posts.GetByIdAsync<DatasetPost>(resOK.Value);
				}
				else
				{
					post = posts.Where(p => p.Title == dto.DatasetUri).SingleOrDefault();
				}
            }

			comment = new PageComment()
			{
				UserId = dto.UserId.ToString("D"),
				Author = Guid.Empty.ToString("D"),
				Email = userEmail,
				Body = dto.Body + "|" + userFormattedName,
				Created = DateTime.UtcNow
			};

			await api.Posts.SaveCommentAsync(post.Id, comment);

			(string publisherEmail, string title, Guid? datasetId) = await documentStorageClient.GetEmailForDataset(dto.DatasetUri);
            if (!string.IsNullOrEmpty(publisherEmail) && datasetId.HasValue)
            {
                string commentText = dto.Body.Trim();
                if (commentText.Length > 300)
                {
                    commentText = string.Concat(commentText.AsSpan(0, 300), "...");
                }
                string commentUrl = $"/datasety/{datasetId.Value}";

                notificationService.Notify(publisherEmail, commentUrl, title, $"K datasetu bol pridaný komentár {commentText}", new List<string> { comment.Id.ToString(), dto.DatasetUri.ToString() });
            }

            return Results.Ok<Guid>(post.Id);
		}
	}
}