using System.Text.Json.Serialization;

namespace CMS.Suggestions
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum SuggestionStates
    {
        /// <summary>
        /// Created.
        /// </summary>
        C,
        /// <summary>
        /// In Progress.
        /// </summary>
        P,
        /// <summary>
        /// Resolved.
        /// </summary>
        R
    }
}