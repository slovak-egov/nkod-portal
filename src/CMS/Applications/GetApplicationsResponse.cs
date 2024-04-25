using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Applications
{
    public class GetApplicationsResponse
	{
		public IEnumerable<ApplicationDto> Items { get; set; }

		public PaginationMetadata PaginationMetadata { get; set; }
	}
}