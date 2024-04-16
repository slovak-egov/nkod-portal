using Piranha.Extend;
using Piranha.Extend.Fields;

namespace CMS.Applications
{
    public class ApplicationRegion
    {
        [Field] public TextField Description { get; set; }
        [Field] public SelectField<ApplicationTypes> Type { get; set; }
        [Field] public StringField Url { get; set; }
        [Field] public StringField OwnerName { get; set; }
        [Field] public StringField OwnerSurname { get; set; }
        [Field] public StringField OwnerEmail { get; set; }
    }
}