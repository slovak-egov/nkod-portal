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
        // so only IRIs are passed to the storage. Values which can not be translated are ignored, when no value remains the filter is not applied.
        public static async Task ResolveShortIds(Dictionary<string, string[]>? filters, ICodelistProviderClient codelistProviderClient)
        {
            if (filters is null)
            {
                return;
            }

            foreach (string key in filters.Keys.ToList())
            {
                string[] values = filters[key];
                if (!codelistFilterKeys.Contains(key) || values.All(IsIri))
                {
                    continue;
                }

                List<string> resolved = new List<string>(values.Length);
                foreach (string value in values)
                {
                    if (value is null)
                    {
                        continue;
                    }
                    if (IsIri(value))
                    {
                        resolved.Add(value);
                    }
                    else
                    {
                        CodelistItem? item = await codelistProviderClient.GetCodelistItem(key, value).ConfigureAwait(false);
                        if (item is not null)
                        {
                            resolved.Add(item.Id);
                        }
                    }
                }
                filters[key] = resolved.ToArray();
            }
        }

        private static bool IsIri(string? value) => value is not null && Uri.IsWellFormedUriString(value, UriKind.Absolute);
    }
}
