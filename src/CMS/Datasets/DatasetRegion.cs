using Piranha.Extend;
using Piranha.Extend.Fields;

namespace CMS.Datasets
{
    public class DatasetRegion
	{
		[Field] public MultiSelectField<Guid> Likes { get; set; }
	}
}