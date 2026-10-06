using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NkodSk.Abstractions
{
    public class AppSettings
    {
        [JsonPropertyName("allowedIframeHosts")]
        public string? AllowedIframeHosts { get; set; }

        public bool IsHostAllowed(string host)
        {
            foreach (string line in (AllowedIframeHosts ?? string.Empty).Split('\n'))
            {
                string entry = line.Trim();
                if (string.IsNullOrEmpty(entry))
                {
                    continue;
                }

                if (!entry.Contains('*'))
                {
                    if (string.Equals(entry, host, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else
                {
                    string regexPattern = "^" + Regex.Escape(entry).Replace("\\*", ".*") + "$";
                    try
                    {
                        if (Regex.IsMatch(host, regexPattern, RegexOptions.IgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch (RegexMatchTimeoutException) { }
                    catch (RegexParseException) { }
                }
            }
            return false;
        }
    }
}
