using Piranha.AttributeBuilder;
using Piranha.Models;

namespace CMS.Suggestions
{
    [PageType(Title = "New dataset suggestions", UseBlocks = false, IsArchive = true)]
    [PageTypeArchiveItem(typeof(SuggestionPost))]
    public class NewDatasetSuggestionsPage : Page<NewDatasetSuggestionsPage>
    {
        public static readonly string WellKnownSlug = "internal-new-dataset-suggestions";
    }
}