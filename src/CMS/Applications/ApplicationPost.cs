using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Models;

namespace CMS.Applications
{
    [PostType(Title = "Application post")]
    public class ApplicationPost : Post<ApplicationPost>
    {
        [Region] public ApplicationRegion Application { get; set; }
    }
}