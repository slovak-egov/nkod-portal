using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Models;

namespace CMS.Datasets
{
    [PostType(Title = "Dataset post")]
    public class DatasetPost : Post<DatasetPost>
    {
        [Region] public DatasetRegion Dataset { get; set; }
    }
}