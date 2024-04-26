using System.Text.Json.Serialization;

namespace CMS.Suggestions
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum ContentTypes
    {
        PN,
        DQ,
        MQ,
        O
    }
}