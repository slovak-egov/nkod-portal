using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS
{
	public class PaginationMetadata
	{
		public int TotalItemCount { get; set; }
		public int PageSize { get; set; }
		public int CurrentPage { get; set; }
	}
}
