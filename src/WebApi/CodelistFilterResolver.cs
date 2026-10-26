using Abstractions;
using NkodSk.Abstractions;

namespace WebApi
{
    public static class CodelistFilterResolver
    {
        // Only these filters are resolved, so unknown filter keys do not cause requests to the codelist provider
        private static readonly HashSet<string> codelistFilterKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DcatDataset.TypeCodelist,
            DcatDataset.ThemeCodelist,
            DcatDataset.AccrualPeriodicityCodelist,
            DcatDistribution.FormatCodelist,
            DcatDistribution.LicenseCodelist,
            DcatDistribution.PersonalDataContainmentTypeCodelist,
            FoafAgent.LegalFormCodelist,
            DcatCatalog.LocalCatalogTypeCodelist
        };

        // Codelist filter values which are not IRIs (e.g. short ids) are translated by the codelist provider to IRIs of codelist items,
        // so only IRIs are passed to the storage. Returns false when some value can not be translated, so the result is empty.
        public static async Task<bool> ResolveShortIds(Dictionary<string, string[]>? filters, ICodelistProviderClient codelistProviderClient)
        {
            if (filters is null)
            {
                return true;
            }

            foreach (string key in filters.Keys.ToList())
            {
                string[] values = filters[key];
                if (!codelistFilterKeys.Contains(key) || values.All(IsIri))
                {
                    continue;
                }

                string[] resolved = new string[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    string value = values[i];
                    if (value is null)
                    {
                        return false;
                    }
                    if (!IsIri(value))
                    {
                        CodelistItem? item = await codelistProviderClient.GetCodelistItem(key, value).ConfigureAwait(false);
                        if (item is null)
                        {
                            return false;
                        }
                        value = item.Id;
                    }
                    resolved[i] = value;
                }
                filters[key] = resolved;
            }

            return true;
        }

        private static bool IsIri(string? value) => value is not null && Uri.IsWellFormedUriString(value, UriKind.Absolute);
    }
}
