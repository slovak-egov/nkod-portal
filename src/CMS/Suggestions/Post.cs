using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Models;

namespace CMS.Suggestions
{
    [PostType(Title = "Suggestion post")]
    public class Post : Post<Post>
    {
        [Region] public Region Suggestion { get; set; }
    }
}