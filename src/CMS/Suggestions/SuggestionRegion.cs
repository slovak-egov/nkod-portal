using Piranha.Extend;
using Piranha.Extend.Fields;

namespace CMS.Suggestions
{
    public class SuggestionRegion
    {
        [Field] public StringField UserId { get; set; }
        [Field] public StringField UserOrgUri { get; set; }
        [Field] public StringField OrgToUri { get; set; }
        [Field] public StringField DatasetUri { get; set; }
        [Field] public TextField Description { get; set; }
        [Field] public SelectField<ContentTypes> Type { get; set; }
        [Field] public SelectField<SuggestionStates> Status { get; set; }
		[Field] public MultiSelectField<Guid> Likes { get; set; }
	}
}