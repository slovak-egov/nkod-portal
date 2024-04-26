using System.Text.Json.Serialization;

namespace CMS
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum OrderByTypes
	{
		Created,
		Updated,
		Title
    }
}