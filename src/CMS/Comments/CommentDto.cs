using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Comments
{
    public class CommentDto
	{
		public Guid Id { get; set; }        

		[Required]
		public Guid ContentId { get; set; }

		[Required]
		public Guid UserId { get; set; }

		public string UserFormattedName { get; set; }
	
		[Required]
		public string Body { get; set; }

		public DateTime Created { get; set; }

		public Guid ParentId { get; set; }
	}
}