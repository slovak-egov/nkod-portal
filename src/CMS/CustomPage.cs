using Piranha.AttributeBuilder;
using Piranha.Models;

namespace CMS
{
    [PageType(Title = "Simple Page", UseBlocks = false, IsArchive = true)]
    public class CustomPage : PageInfo
    {
    }
}