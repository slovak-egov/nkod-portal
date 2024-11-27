using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Suggestions
{
    public class SuggestionDto
	{
		public Guid Id { get; set; }

        [Required]
		public Guid UserId { get; set; }

		public string UserFormattedName { get; set; }

		[Url]
		public string UserOrgUri { get; set; }

		[Required]
		[Url]
		public string OrgToUri { get; set; }

		[Required]
		[JsonConverter(typeof(JsonStringEnumConverter))]        
        public ContentTypes Type { get; set; }

		[Url]
		public string DatasetUri { get; set; }

		[Required]
		public string Title { get; set; }

		[Required]
		public string Description { get; set; }

		[Required]
		[JsonConverter(typeof(JsonStringEnumConverter))]        
        public SuggestionStates Status { get; set; }

		public DateTime Created { get; set; }

		public DateTime Updated { get; set; }

		public int CommentCount { get; set; }

		public int LikeCount { get; set; }
	}
}