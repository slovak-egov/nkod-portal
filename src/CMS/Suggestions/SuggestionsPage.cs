using Piranha.AttributeBuilder;
using Piranha.Models;

namespace CMS.Suggestions
{
    [PageType(Title = "Suggestions", UseBlocks = false, IsArchive = true)]
    [PageTypeArchiveItem(typeof(SuggestionPost))]
    public class SuggestionsPage : Page<SuggestionsPage>
    {
        public static readonly string WellKnownSlug = "internal-suggestions";
    }
}