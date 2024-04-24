using CMS.Suggestions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;
using System;

namespace CMS.Comments
{
	[Route("comments")]
	[ApiController]
	[AllowAnonymous]
	public class CommentController
	{
		private readonly IApi api;

		public CommentController(IApi api)
		{
			this.api = api;
		}

		[HttpGet]
		[Route("")]
		public async Task<IEnumerable<CommentDto>> GetComments(Guid contentId, int? pageNumber, int? pageSize)
		{
			var res = await api.Posts.GetAllCommentsAsync(contentId, false, pageNumber, pageSize);
			return res.Select(c => Convert(c)).OrderByDescending(c => c.Created);
		}

		[HttpGet("{id}")]
		public async Task<CommentDto> GetByID(Guid id)
		{
			return Convert(await api.Posts.GetCommentByIdAsync(id));
		}

		private static CommentDto Convert(Comment c)
		{
			return new CommentDto
			{
				Id = c.Id,
				ContentId = c.ContentId,
				UserId = Guid.Parse(c.UserId),
				Author = c.Author,
				Email = c.Email,
				Body = c.Body,
				Created = c.Created
			};
		}

		[HttpPost]
		[Route("")]
		public async Task<IResult> AddComment(CommentDto dto)
		{
			var comment = new PageComment()
			{
				Id = dto.Id,
				ContentId = dto.ContentId,
				UserId = dto.UserId.ToString("D"),
				Author = dto.Author,
				Email = dto.Email,
				Body = dto.Body,
				Created = DateTime.Now
			};

			await api.Posts.SaveCommentAsync(comment.ContentId, comment);
			return Results.Ok();
		}
	}
}
