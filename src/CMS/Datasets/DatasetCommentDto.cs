using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Datasets
{
    public class DatasetCommentDto
	{
		[Required]
		[Url]
		public string DatasetUri { get; set; }

		[Required]
		public Guid UserId { get; set; }
		
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		public string Body { get; set; }
	}
}