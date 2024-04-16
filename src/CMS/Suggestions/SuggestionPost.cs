using Piranha.AttributeBuilder;
using Piranha.Extend;
using Piranha.Models;

namespace CMS.Suggestions
{
    [PostType(Title = "Suggestion post")]
    public class SuggestionPost : Post<SuggestionPost>
    {
        [Region] public SuggestionRegion Suggestion { get; set; }
    }
}