using System.Text.Json.Serialization;

namespace CMS.Suggestions
{
    public class SuggestionDto
    {
        public string UserId { get; set; }
        public string UserOrgUri { get; set; }
        public string OrgToUri { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContentTypes Type { get; set; }
        public string DatasetUri { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SuggestionStates State { get; set; }
    }
}