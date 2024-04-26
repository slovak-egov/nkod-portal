using System.Text.Json.Serialization;

namespace CMS.Suggestions
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum SuggestionStates
    {
        C,
        P,
        R
    }
}