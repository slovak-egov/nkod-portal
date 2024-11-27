using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Applications
{
    public class ApplicationSearchRequest
	{
		public string SearchQuery { get; set; }

		public ApplicationTypes[] Types { get; set; }

		public ApplicationThemes[] Themes { get; set; }

		public OrderByTypes? OrderBy { get; set; }

		public int? PageNumber { get; set; }

		public int? PageSize { get; set; }		
	}
}