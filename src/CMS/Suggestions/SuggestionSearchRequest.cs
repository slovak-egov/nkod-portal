using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Suggestions
{
    public class SuggestionSearchRequest
	{
		public string SearchQuery { get; set; }

		public IList<string> OrgToUris { get; set; }

		[JsonConverter(typeof(JsonStringEnumConverter))]
		public IList<ContentTypes> Types { get; set; }

		[JsonConverter(typeof(JsonStringEnumConverter))]
		public IList<SuggestionStates> Statuses { get; set; }

		[JsonConverter(typeof(JsonStringEnumConverter))]
		public OrderByTypes? OrderBy { get; set; }

		public int? PageNumber { get; set; }

		public int? PageSize { get; set; }		
	}
}