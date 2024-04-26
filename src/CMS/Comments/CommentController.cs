﻿using CMS.Applications;
using CMS.Suggestions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piranha;
using Piranha.Extend.Fields;
using Piranha.Models;
using System;
using System.ComponentModel.DataAnnotations;

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
			return new CommentDto
			{
				Id = c.Id,
				ContentId = c.ContentId,
				UserId = (!string.IsNullOrEmpty(c.UserId)) ? Guid.Parse(c.UserId) : Guid.Empty,
				Email = c.Email,
				Body = c.Body,
				Created = c.Created,
				ParentId = (!string.IsNullOrEmpty(c.Author)) ? Guid.Parse(c.Author) : Guid.Empty,
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
				Author = dto.ParentId.ToString("D"),
				Email = dto.Email,
				Body = dto.Body,
				Created = DateTime.Now
			};

			await api.Posts.SaveCommentAsync(comment.ContentId, comment);
			return Results.Ok();
		}

		[HttpDelete("{id}")]
		public async Task<IResult> Delete(Guid id)
		{
			await api.Posts.DeleteCommentAsync(id);
			return Results.Ok();
		}
	}
}