using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace IAM
{
    public class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}
