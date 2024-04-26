using CMS.Comments;
using CMS.Likes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;
using System.Linq;
using System.Text.Json.Serialization;

namespace CMS.Datasets
{
    [Route("datasets")]
    [ApiController]
    [AllowAnonymous]
    public class DatasetController
	{
        private readonly IApi api;

        public DatasetController(IApi api)
        {
            this.api = api;
        }       
        
		[HttpGet]
		[Route("")]
		public async Task<DatasetSearchResponse> Get(int? pageNumber, int? pageSize)
		{
			var page = await api.Pages.GetBySlugAsync(DatasetsPage.WellKnownSlug);
			var archive = await api.Archives.GetByIdAsync<DatasetPost>(page.Id);
			int pn = 0;
            int ps = 100000;
			PaginationMetadata paginationMetadata = null;
            IEnumerable<DatasetPost> res = archive.Posts;

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
				Title = p.Title,
				Created = p.Created,
				Updated = p.LastModified,
                CommentCount = p.CommentCount,
                LikeCount = (p.Dataset.Likes?.Value != null) ? p.Dataset.Likes.Value.Count() : 0,				
                DatasetUri = p.Dataset.DatasetUri.Value
            };
        }
        
		[HttpPost]
        [Route("")]
        public async Task<IResult> Save(DatasetDto dto)
        {
            var archiveId = await GetArchiveGuidAsync();
            var post = await api.Posts.CreateAsync<DatasetPost>();
			post.Title = dto.Title;
			post.Dataset = new DatasetRegion
            {
                DatasetUri = dto.DatasetUri,
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
            return Results.Ok();
        }

		[HttpPut("{id}")]
		public async Task<IResult> Update(Guid id, DatasetDto dto)
		{
			var post = await api.Posts.GetByIdAsync<DatasetPost>(id);

			post.Title = dto.Title;
			post.Dataset.DatasetUri = dto.DatasetUri;

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
		public async Task<IResult> AddLike(LikeDto dto)
		{
			var post = await api.Posts.GetByIdAsync<DatasetPost>(dto.ContentId);

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
			return Results.Ok();
		}
	}
}