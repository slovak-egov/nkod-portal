using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS.Applications
{
    public class ApplicationDto
    {
		public Guid Id { get; set; }

		[Required]
		public Guid UserId { get; set; }

		[Required]
		public string Title { get; set; }

		[Required]
		public string Description { get; set; }

		[Required]
		[JsonConverter(typeof(JsonStringEnumConverter))]
        public ApplicationTypes Type { get; set; }

		[Required]
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public ApplicationThemes Theme { get; set; }
 
		[Url] 
        public string Url { get; set; }

		public string Logo { get; set; }

		public string LogoFileName { get; set; }

		public IList<string> DatasetURIs { get; set; }

		[Required]
		public string ContactName { get; set; }

		[Required]
		public string ContactSurname { get; set; }

		[Required]
		[EmailAddress] 
        public string ContactEmail { get; set; }

		public DateTime Created { get; set; }

		public DateTime Updated { get; set; }
	}
}