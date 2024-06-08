using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CMS.Datasets
{
    public class DatasetSearchResponse
	{
		public IEnumerable<DatasetDto> Items { get; set; }

		public PaginationMetadata PaginationMetadata { get; set; }
	}
}