using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS.Applications
{
    public class ApplicationDto
    {
        public string Title { get; set; }
        public string Description { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApplicationTypes Type { get; set; }

        public string Url { get; set; }
        public string OwnerName { get; set; }
        public string OwnerSurname { get; set; }
        [EmailAddress] public string OwnerEmail { get; set; }
    }
}