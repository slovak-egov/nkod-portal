using System.Text.Json.Serialization;

namespace CMS.Applications
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum ApplicationThemes
	{
        ED,
        HE,
        EN,
        TR,
        CU,
        TU,
        EC,
        SO,
        PA,
        O
    }
}