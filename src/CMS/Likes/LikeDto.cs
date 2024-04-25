using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Likes
{
    public class LikeDto
	{   
		[Required]
		public Guid ContentId { get; set; }

		[Required]
		public Guid UserId { get; set; }		
	}
}