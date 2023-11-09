using Abstractions;
using CodelistProviderClient;
using DocumentStorageClient;
using NkodSk.Abstractions;
using System.Security.Policy;
using static Lucene.Net.Queries.Function.ValueSources.MultiFunction;

namespace WebApi
{
    public static class Extensions
    {
        public static async Task<CodelistItemView[]> MapCodelistValues(this ICodelistProviderClient codelistProviderClient, string codelistId, IEnumerable<string> keys, string language)
        {
            Codelist? codelist = await codelistProviderClient.GetCodelist(codelistId);
            if (codelist is not null)
            {
                List<CodelistItemView> values = new List<CodelistItemView>();
                foreach (string key in keys)
                {
                    string? label = null;
                    if (codelist.Items.TryGetValue(key, out CodelistItem? codelistItem))
                    {
                        label = codelistItem.GetCodelistValueLabel(language);
                    }
                    values.Add(new CodelistItemView(key, label ?? key));
                }
                return values.ToArray();
            }
            else
            {
                return Array.Empty<CodelistItemView>();
            }
        }

        public static string? GetLanguageValue(this Dictionary<string, string> labels, string language)
        {
            string? label;
            if (labels.TryGetValue(language, out label))
            {
                return label;
            }
            else if (language != "sk" && labels.TryGetValue("sk", out label))
            {
                return label;
            }
            else if (language != "en" && labels.TryGetValue("en", out label))
            {
                return label;
            }
            else
            {
                return null;
            }
        }

        public static string GetLabel(this Codelist codelist, string language)
        {
            return codelist.Labels.GetLanguageValue(language) ?? codelist.Id;
        }

        public static string GetCodelistValueLabel(this CodelistItem codelistItem, string language)
        {
            return codelistItem.Labels.GetLanguageValue(language) ?? codelistItem.Id;
        }

        public static async Task<CodelistItemView?> MapCodelistValue(this ICodelistProviderClient codelistProviderClient, string codelistId, string? key, string language)
        {
            if (key is not null)
            {
                string? label = null;
                Codelist? codelist = await codelistProviderClient.GetCodelist(codelistId);
                if (codelist is not null)
                {
                    if (codelist.Items.TryGetValue(key, out CodelistItem? codelistItem))
                    {
                        label = codelistItem.GetCodelistValueLabel(language);
                    }
                }
                return new CodelistItemView(key, label ?? key);
            }
            return null;
        }

        public static Uri? AsUri(this string? value) => !string.IsNullOrEmpty(value) ? new Uri(value, UriKind.Absolute) : null;

        public static async Task<FileState?> GetPublisherFileState(this IDocumentStorageClient documentStorageClient, string publisherId)
        {
            FileStorageQuery storageQuery = new FileStorageQuery
            {
                OnlyTypes = new List<FileType> { FileType.PublisherRegistration },
                OnlyPublishers = new List<string> { publisherId },
                MaxResults = 1
            };
            FileStorageResponse response = await documentStorageClient.GetFileStates(storageQuery).ConfigureAwait(false);
            return response.Files.Count > 0 ? response.Files[0] : null;
        }
    }
}
