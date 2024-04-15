using CMS.Models;
using Piranha.Extend;
using Piranha.Extend.Fields;

namespace CMS
{
    public class SuggestionRegion
    {
        [Field] public StringField UserId { get; set; }
        [Field] public StringField UserOrgUri { get; set; }
        [Field] public StringField OrgToUri { get; set; }
        [Field] public TextField Description { get; set; }
        [Field] public SelectField<ContentTypes> Type { get; set; }
        [Field] public SelectField<States> State { get; set; }
    }
}