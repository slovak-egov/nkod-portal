using Piranha.AttributeBuilder;
using Piranha.Models;

namespace CMS.Applications
{
    [PageType(Title = "Applications", UseBlocks = false, IsArchive = true)]
    [PageTypeArchiveItem(typeof(ApplicationPost))]
    public class ApplicationsPage : Page<ApplicationsPage>
    {
        public static readonly string WellKnownSlug = "internal-applications";
    }
}