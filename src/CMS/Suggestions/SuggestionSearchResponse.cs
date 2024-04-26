using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Suggestions
{
    public class SuggestionSearchResponse
	{
		public IEnumerable<SuggestionDto> Items { get; set; }

		public PaginationMetadata PaginationMetadata { get; set; }
	}
}