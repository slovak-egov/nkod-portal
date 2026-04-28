using Abstractions;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text;
using System.Web;

namespace ValidationCore
{
    public class PublicCodelistItemValidator : ICodelistItemValidation
    {
        private readonly Dictionary<Uri, HashSet<Uri>> items = new Dictionary<Uri, HashSet<Uri>>(UriEqualityComparer.Instance);

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private readonly Uri endpoint = new Uri("https://data.slovensko.sk/codelists");

        public async Task<bool> IsValid(Uri codelist, Uri item)
        {
            bool? isValid = IsValidFromCache(codelist, item);
            if (isValid.HasValue)
            {
                return isValid.Value;
            }

            await semaphore.WaitAsync();
            try
            {
                isValid = IsValidFromCacheWithoutLock(codelist, item);
                if (isValid.HasValue)
                {
                    return isValid.Value;
                }

                using HttpClient client = new HttpClient { BaseAddress =  endpoint, Timeout = TimeSpan.FromMinutes(1) };
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "?keys[]=" + HttpUtility.UrlEncode(codelist.OriginalString));
                using HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                List<Codelist>? loadedCodelists = await response.Content.ReadFromJsonAsync<List<Codelist>>();
                if (loadedCodelists is not null)
                {
                    Codelist? loadedCodelist = loadedCodelists.FirstOrDefault(c => c.Id == codelist.OriginalString);
                    if (loadedCodelist?.Items is not null)
                    {
                        HashSet<Uri> set = new HashSet<Uri>(UriEqualityComparer.Instance);
                        foreach (CodelistItem loadedItem in loadedCodelist.Items)
                        {
                            if (!string.IsNullOrEmpty(loadedItem.Id) && Uri.TryCreate(loadedItem.Id, UriKind.Absolute, out Uri? uri))
                            {
                                set.Add(uri);
                            }
                        }
                        items[codelist] = set;
                    }
                }

                return IsValidFromCacheWithoutLock(codelist, item).GetValueOrDefault();
            }
            finally
            {
                semaphore.Release();
            }
        }

        private bool? IsValidFromCache(Uri codelist, Uri item)
        {
            semaphore.Wait();
            try
            {
                return IsValidFromCacheWithoutLock(codelist, item);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private bool? IsValidFromCacheWithoutLock(Uri codelist, Uri item)
        {
            if (items.TryGetValue(codelist, out HashSet<Uri>? codelistItems))
            {
                return codelistItems.Contains(item);
            }
            return null;
        }

        private class Codelist
        {
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string? Id { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("values")]
            public List<CodelistItem>? Items { get; set; }
        }

        private class CodelistItem
        {
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string? Id { get; set; }
        }
    }
}
