using CMS.Suggestions;
using Piranha.AttributeBuilder;
using Piranha.Models;

namespace CMS
{
    [PageType(Title = "Dataset Page", UseBlocks = false, IsArchive = true)]
    [PageTypeArchiveItem(typeof(Post))]
    public class DatasetPage : Page<DatasetPage>
    {
        public PostArchive<Post> Archive { get; set; }
    }
}