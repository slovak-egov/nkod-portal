using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Suggestions
{
    public class CommentDto
	{
		public Guid Id { get; set; }        

		[Required]
		public Guid ContentId { get; set; }

		[Required]
		public Guid UserId { get; set; }
		
		[Required]
		public string Author { get; set; }
				
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		public string Body { get; set; }

		public DateTime Created { get; set; }
	}
}