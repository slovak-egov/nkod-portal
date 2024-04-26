using CMS.Suggestions;
using Piranha.AttributeBuilder;
using Piranha.Models;

namespace CMS.Datasets
{
    [PageType(Title = "Dataset Page", UseBlocks = false, IsArchive = true)]
    [PageTypeArchiveItem(typeof(DatasetPost))]
    public class DatasetPage : Page<DatasetPage>
    {
		public static readonly string WellKnownSlug = "internal-datasets";
    }
}