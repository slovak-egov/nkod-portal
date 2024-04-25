using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Comments
{
    public class GetApplicationsResponse
	{
		public IEnumerable<CommentDto> Items { get; set; }

		public PaginationMetadata PaginationMetadata { get; set; }
	}
}