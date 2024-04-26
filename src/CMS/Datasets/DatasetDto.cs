using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Datasets
{
    public class DatasetDto
	{
		public Guid Id { get; set; }

		[Required]
		public string Title { get; set; }

		[Required]
		[Url]
		public string DatasetUri { get; set; }

		public DateTime Created { get; set; }

		public DateTime Updated { get; set; }

		public int CommentCount { get; set; }

		public int LikeCount { get; set; }
	}
}