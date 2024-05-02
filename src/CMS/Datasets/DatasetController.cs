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

namespace CMS.Datasets
{
    [Route("datasets")]
    [ApiController]
    [AllowAnonymous]
	[Authorize(AuthenticationSchemes = "Bearer", Policy = "MustBeAuthenticated")]
	public class DatasetController : ControllerBase
	{
        private readonly IApi api;

        public DatasetController(IApi api)
        {
            this.api = api;
        }       
        
		[HttpGet]
		[Route("")]
		public async Task<DatasetSearchResponse> Get(string datasetUri, int? pageNumber, int? pageSize)
		{
			var pageId = await GetArchiveGuidAsync();			
			var archive = await api.Archives.GetByIdAsync<DatasetPost>(pageId);
			int pn = 0;
            int ps = 100000;
			PaginationMetadata paginationMetadata = null;
            IEnumerable<DatasetPost> res = archive.Posts;

			if (!string.IsNullOrEmpty(datasetUri))
			{
				res = res.Where(p => p.Title == datasetUri);
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

			return new DatasetSearchResponse()
			{
				Items = res.Select(p => Convert(p)),
				PaginationMetadata = paginationMetadata
			};
		}     
        
		[HttpGet("{id}")]		
		public async Task<DatasetDto> GetByID(Guid id)
		{
			var post = await api.Posts.GetByIdAsync<DatasetPost>(id);
			return Convert(post);
		}

		private static DatasetDto Convert(DatasetPost p)
        {
            return new DatasetDto
			{
                Id = p.Id,
				Created = p.Created,
				Updated = p.LastModified,
                CommentCount = p.CommentCount,
                LikeCount = (p.Dataset.Likes?.Value != null) ? p.Dataset.Likes.Value.Count() : 0,				
                DatasetUri = p.Title
            };
        }
        
		[HttpPost]
        [Route("")]
        public async Task<IResult> Save(DatasetDto dto)
        {
            var archiveId = await GetArchiveGuidAsync();
            var post = await api.Posts.CreateAsync<DatasetPost>();
			post.Title = dto.DatasetUri;
			post.Dataset = new DatasetRegion
            {
                Likes = new MultiSelectField<Guid>()
			};

            post.Category = new Taxonomy
            {
                Title = "Dataset",
                Slug = "dataset",
                Type = TaxonomyType.Category
            };
            post.BlogId = archiveId;
            post.Published = DateTime.Now;

            await api.Posts.SaveAsync(post);
			return Results.Ok<Guid>(post.Id);
		}

		[HttpPut("{id}")]
		public async Task<IResult> Update(Guid id, DatasetDto dto)
		{
			var post = await api.Posts.GetByIdAsync<DatasetPost>(id);

			post.Title = dto.DatasetUri;
			post.LastModified = DateTime.Now;

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
            var page = await api.Pages.GetBySlugAsync(DatasetsPage.WellKnownSlug);
            return page?.Id ?? (await CreatePage()).Id;
        }

        private async Task<PageBase> CreatePage()
        {
            PageBase newPage = await api.Pages.CreateAsync<DatasetsPage>();

            newPage.Title = DatasetsPage.WellKnownSlug;
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
		public async Task<IResult> AddLike(DatasetLikeDto dto)
		{
			DatasetPost post = null;

			if (dto.ContentId == null && string.IsNullOrWhiteSpace(dto.DatasetUri))
			{
				return Results.Problem("ContentId or DatasetUri must be set!");
			}

			if (dto.ContentId == null)
			{
				var pageId = await GetArchiveGuidAsync();
				var archive = await api.Archives.GetByIdAsync<DatasetPost>(pageId);
				post = archive.Posts.Where(p => p.Title == dto.DatasetUri).SingleOrDefault();
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
						archive = await api.Archives.GetByIdAsync<DatasetPost>(pageId);
						post = archive.Posts.Where(p => p.Title == dto.DatasetUri).SingleOrDefault();
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
                    return Results.Problem("Attempt to add next like by same user!");
                }
                else
                {
                    likes.Add(dto.UserId);
                    post.Dataset.Likes.Value = likes;
                }
            }
            else 
            {
                List<Guid> userIds = new List<Guid>();
                userIds.Add(dto.UserId);
				post.Dataset.Likes = new MultiSelectField<Guid> { Value = userIds };
			}

			await api.Posts.SaveAsync(post);
			return Results.Ok<Guid>(post.Id);
		}

		[HttpPost]
		[Route("comments")]
		public async Task<IResult> AddComment(DatasetCommentDto dto)
		{
			var pageId = await GetArchiveGuidAsync();
			var archive = await api.Archives.GetByIdAsync<DatasetPost>(pageId);
			DatasetPost post = archive.Posts.Where(p => p.Title == dto.DatasetUri).SingleOrDefault();
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
					archive = await api.Archives.GetByIdAsync<DatasetPost>(pageId);
					post = archive.Posts.Where(p => p.Title == dto.DatasetUri).SingleOrDefault();
				}
            }

			comment = new PageComment()
			{
				UserId = dto.UserId.ToString("D"),
				Author = Guid.Empty.ToString("D"),
				Email = dto.Email,
				Body = dto.Body,
				Created = DateTime.Now
			};

			await api.Posts.SaveCommentAsync(post.Id, comment);
			return Results.Ok<Guid>(post.Id);
		}
	}
}