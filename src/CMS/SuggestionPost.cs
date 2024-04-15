using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Models;

namespace CMS
{
    [PostType(Title = "Standard post")]
    public class SuggestionPost : Post<SuggestionPost>
    {
        [Region] public SuggestionRegion Suggestion { get; set; }
    }
}