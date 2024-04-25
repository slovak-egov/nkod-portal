using Piranha.Extend;
using Piranha.Extend.Fields;

namespace CMS.Applications
{
    public class ApplicationRegion
    {
		[Field] public StringField UserId { get; set; }
		[Field] public TextField Description { get; set; }
        [Field] public SelectField<ApplicationTypes> Type { get; set; }
		[Field] public SelectField<ApplicationThemes> Theme { get; set; }
		[Field] public StringField Url { get; set; }
		[Field] public StringField Logo { get; set; }
		[Field] public StringField LogoFileName { get; set; }
		[Field] public MultiSelectField<string> DatasetURIs { get; set; }
		[Field] public StringField ContactName { get; set; }
        [Field] public StringField ContactSurname { get; set; }
        [Field] public StringField ContactEmail { get; set; }
		[Field] public MultiSelectField<Guid> Likes { get; set; }
	}
}