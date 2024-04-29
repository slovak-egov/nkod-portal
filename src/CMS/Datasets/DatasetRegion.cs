using Piranha.Extend;
using Piranha.Extend.Fields;
using System.ComponentModel.DataAnnotations;

namespace CMS.Datasets
{
    public class DatasetRegion
	{
		//[Field] public StringField DatasetUri { get; set; }
		[Field] public MultiSelectField<Guid> Likes { get; set; }
	}
}