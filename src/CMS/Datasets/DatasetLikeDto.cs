using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Datasets
{
    public class DatasetLikeDto
	{
		[Url]
		public string DatasetUri { get; set; }

		public Guid ContentId { get; set; }

		[Required]
		public Guid UserId { get; set; }		
	}
}