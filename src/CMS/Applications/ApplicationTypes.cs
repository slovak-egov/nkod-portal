using System.Text.Json.Serialization;

namespace CMS.Applications
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum ApplicationTypes
    {		
		MA,
        WA,
        WP,
        V,
        A
    }
}