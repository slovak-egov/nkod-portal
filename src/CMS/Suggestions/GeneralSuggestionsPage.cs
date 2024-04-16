using Piranha.AttributeBuilder;
using Piranha.Models;

namespace CMS.Suggestions
{
    [PageType(Title = "General suggestions", UseBlocks = false, IsArchive = true)]
    [PageTypeArchiveItem(typeof(SuggestionPost))]
    public class GeneralSuggestionsPage : Page<GeneralSuggestionsPage>
    {
        public static readonly string WellKnownSlug = "internal-general-suggestions";
    }
}