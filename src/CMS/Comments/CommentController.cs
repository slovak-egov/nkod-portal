using CMS.Applications;
using CMS.Datasets;
using CMS.Suggestions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CMS.Comments
{
	[Route("comments")]
	[ApiController]	
	public class CommentController : ControllerBase
	{
		private readonly IApi api;

		public CommentController(IApi api)
		{
			this.api = api;
		}

		[HttpGet]
		[Route("")]
		public async Task<GetApplicationsResponse> GetComments(Guid? contentId, int? pageNumber, int? pageSize)
		{
			IEnumerable<Comment> res;
			int pn = 0;
			int ps = 100000;
			PaginationMetadata paginationMetadata = null;

			if (contentId != null)
			{
				res = await api.Posts.GetAllCommentsAsync(postId: contentId, onlyApproved: false);
			}
			else 
			{
				res = await api.Posts.GetAllCommentsAsync(onlyApproved: false);
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

			return new GetApplicationsResponse()
			{
				Items = res.Select(c => Convert(c)),
				PaginationMetadata = paginationMetadata
			};
		}

		[HttpGet("{id}")]
		public async Task<CommentDto> GetByID(Guid id)
		{
			return Convert(await api.Posts.GetCommentByIdAsync(id));
		}

		private static CommentDto Convert(Comment c)
		{
			string userFormattedName = string.Empty;
			int pos = c.Body.LastIndexOf('|');
			if (pos >= 0)
			{
				userFormattedName = c.Body.Length > pos ? c.Body.Substring(pos + 1) : string.Empty;
				c.Body = c.Body.Substring(0, pos);
			}

			return new CommentDto
			{
				Id = c.Id,
				ContentId = c.ContentId,
				UserId = (!string.IsNullOrEmpty(c.UserId)) ? Guid.Parse(c.UserId) : Guid.Empty,
				UserFormattedName = userFormattedName,
				Body = c.Body,
				Created = c.Created,
				ParentId = (!string.IsNullOrEmpty(c.Author)) ? Guid.Parse(c.Author) : Guid.Empty,
			};
		}

		[HttpPost]
		[Route("")]
		[Authorize]
		public async Task<IResult> AddComment(CommentDto dto)
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

			var comment = new PageComment()
			{
				UserId = dto.UserId.ToString("D"),
				Author = dto.ParentId.ToString("D"),
				Email = userEmail,
				Body = dto.Body + "|" + userFormattedName,
				Created = DateTime.UtcNow
			};

			await api.Posts.SaveCommentAsync(dto.ContentId, comment);
			return Results.Ok<Guid>(comment.Id);
		}

		[HttpPut("{id}")]
		[Authorize]
		public async Task<IResult> Update(Guid id, CommentDto dto)
		{
			ClaimsPrincipal user = HttpContext.User;

			if (user == null)
			{
				return Results.Forbid();
			}

            string userFormattedName = user?.FindFirstValue(ClaimTypes.Name);

            Guid userId = Guid.Parse(user?.Claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value);
			Comment comment = await api.Posts.GetCommentByIdAsync(id);

			if (!((user.IsInRole("Superadmin") ||
				user.IsInRole("Publisher") ||
				user.IsInRole("PublisherAdmin") ||
				user.IsInRole("CommunityUser")) && 
				userId == Guid.Parse(comment.UserId)))
			{
				return Results.Forbid();
			}

			comment.Body = dto.Body + "|" + userFormattedName;

			await api.Posts.SaveCommentAsync(dto.ContentId, comment);
			return Results.Ok();
		}

		[HttpDelete("{id}")]
		[Authorize]
		public async Task<IResult> Delete(Guid id)
		{
			ClaimsPrincipal user = HttpContext.User;

			if (user == null)
			{
				return Results.Forbid();
			}

			Guid userId = Guid.Parse(user?.Claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value);
			Comment comment = await api.Posts.GetCommentByIdAsync(id);
			IEnumerable<Comment> comments = await api.Posts.GetAllCommentsAsync(comment.ContentId);
			comments = comments.Where(c => Guid.Parse(c.Author) == comment.Id);

			if (!(user.IsInRole("Superadmin") ||
				((user.IsInRole("Publisher") ||
				user.IsInRole("PublisherAdmin") ||
				user.IsInRole("CommunityUser")
				) && 
				userId == Guid.Parse(comment.UserId) && 
				comments.Count() == 0)
				))
			{
				return Results.Forbid();
			}
			
			if (comments.Count() >= 0)
			{
				foreach (Comment c in comments) 
				{
					await this.Delete(c.Id);
				}
			}

			await api.Posts.DeleteCommentAsync(id);
			return Results.Ok();
		}
	}
}
