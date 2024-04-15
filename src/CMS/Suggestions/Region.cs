using Piranha.Extend;
using Piranha.Extend.Fields;

namespace CMS.Suggestions
{
    public class Region
    {
        [Field] public StringField UserId { get; set; }
        [Field] public StringField UserOrgUri { get; set; }
        [Field] public StringField OrgToUri { get; set; }
        [Field] public TextField Description { get; set; }
        [Field] public SelectField<ContentTypes> Type { get; set; }
        [Field] public SelectField<States> State { get; set; }
    }
}