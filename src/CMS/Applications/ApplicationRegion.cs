using Piranha.Extend;
using Piranha.Extend.Fields;

namespace CMS.Applications
{
    public class ApplicationRegion
    {
        [Field] public TextField Description { get; set; }
        [Field] public SelectField<ApplicationTypes> Type { get; set; }
		[Field] public SelectField<ApplicationThemes> Theme { get; set; }
		[Field] public StringField Url { get; set; }
		[Field] public StringField Logo { get; set; }
		[Field] public MultiSelectField DatasetURIs { get; set; }
		[Field] public StringField ContactName { get; set; }
        [Field] public StringField ContactSurname { get; set; }
        [Field] public StringField ContactEmail { get; set; }
	}
}