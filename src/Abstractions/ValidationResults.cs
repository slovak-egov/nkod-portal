using Abstractions;
using NkodSk.Abstractions;
using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NkodSk.Abstractions
{
    public class ValidationResults : Dictionary<string, string>
    {
        public bool IsValid => Count == 0;

        public void AddError(string key, string message)
        {
            Add(key.ToLowerInvariant(), message);
        }

        private void RemoveEmptyValues(ICollection<string>? values)
        {
            if (values is not null)
            {
                foreach (string? value in values.Where(string.IsNullOrWhiteSpace).ToList())
                {
                    values.Remove(value);
                }
            }                
        }

        private void RemoveEmptyValues(IDictionary<string, string>? values)
        {
            if (values is not null)
            {
                foreach (string value in values.Where(k => string.IsNullOrWhiteSpace(k.Value)).Select(k => k.Key).ToList())
                {
                    values.Remove(value);
                }
            }
        }

        private void RemoveEmptyValues(IDictionary<string, List<string>>? values)
        {
            if (values is not null)
            {
                foreach (List<string> value in values.Values)
                {
                    RemoveEmptyValues(value);
                }
            }
        }

        public bool ValidateLanguageTexts(string key, Dictionary<string, string>? values, ICollection<string>? languages, bool isRequired)
        {
            bool isValid = true;
            RemoveEmptyValues(values);
            foreach (string language in languages ?? values?.Keys ?? Enumerable.Empty<string>())
            {
                string? value = values is not null && values.ContainsKey(language) ? values[language] : null;
                bool isEmpty = string.IsNullOrWhiteSpace(value);

                if (isEmpty && isRequired)
                {
                    AddError(key + language, "Hodnota musí byť zadaná");
                    isValid = false;
                }
            }
            return isValid;
        }

        public async Task<bool> ValidateCodelistValue(string? value, string codelistId, ICodelistProviderClient codelistProvider)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (Uri.IsWellFormedUriString(value, UriKind.Absolute))
                {
                    return await codelistProvider.GetCodelistItem(codelistId, value) is not null;
                }
            }
            return false;
        }

        public async Task<bool> ValidateRequiredCodelistValues(string key, ICollection<string>? values, string codelistId, ICodelistProviderClient codelistProvider)
        {
            bool hasValue = false;
            bool isValid = true;
            RemoveEmptyValues(values);
            if (values is not null)
            {
                foreach (string? value in values)
                {
                    hasValue = true;
                    if (!string.IsNullOrEmpty(value) && !await ValidateCodelistValue(value, codelistId, codelistProvider))
                    {
                        isValid = false;
                    }
                }
            }
            if (!isValid || !hasValue)
            {
                AddError(key, "Hodnota musí byť zadaná z číselníka");
            }
            return isValid;
        }

        public async Task<bool> ValidateRequiredCodelistValue(string key, string? value, string codelistId, ICodelistProviderClient codelistProvider)
        {
            if (!await ValidateCodelistValue(value, codelistId, codelistProvider))
            {
                AddError(key, "Hodnota musí byť zadaná z číselníka");
                return false;
            }
            return true;
        }

        public async Task<bool> ValidateCodelistValues(string key, ICollection<string>? values, string codelistId, ICodelistProviderClient codelistProvider)
        {
            RemoveEmptyValues(values);
            if (values is not null)
            {
                bool isValid = true;
                foreach (string? value in values)
                {
                    if (!string.IsNullOrEmpty(value) && !await ValidateCodelistValue(value, codelistId, codelistProvider))
                    {
                        isValid = false;
                    }
                }
                if (!isValid)
                {
                    AddError(key, "Hodnota musí byť zadaná z číselníka");
                }
                return isValid;
            }
            return true;
        }

        public async Task<bool> ValidateCodelistValue(string key, string? value, string codelistId, ICodelistProviderClient codelistProvider)
        {
            if (!string.IsNullOrEmpty(value) && !await ValidateCodelistValue(value, codelistId, codelistProvider))
            {
                AddError(key, "Hodnota musí byť zadaná z číselníka");
                return false;
            }
            return true;
        }

        public bool ValidateKeywords(string key, Dictionary<string, List<string>>? values, IEnumerable<string> languages)
        {
            RemoveEmptyValues(values);
            bool isValid = true;
            foreach (string language in languages)
            {
                if (values is null || !values.ContainsKey(language) || !values[language].Any() || values[language].All(string.IsNullOrWhiteSpace))
                {
                    AddError(key + language, "Hodnota musí byť zadaná");
                    isValid = false;
                }
            }
            return isValid;
        }

        public bool ValidateLanguageTexts(string key, Dictionary<string, List<string>>? values, IEnumerable<string> languages)
        {
            RemoveEmptyValues(values);
            bool isValid = true;
            foreach (string language in languages)
            {
                if (values is not null && values.ContainsKey(language) && values[language].All(string.IsNullOrWhiteSpace))
                {
                    AddError(key + language, "Hodnota nie je správne zadaná");
                    isValid = false;
                }
            }
            return isValid;
        }

        public bool ValidateEmail(string key, string? value, bool isRequired)
        {
            if ((string.IsNullOrEmpty(value) && isRequired) || (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, @"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])")))
            {
                AddError(key, "E-mailová adresa nie je platná");
                return false;
            }
            return true;
        }

        public bool ValidateRequiredText(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(key, "Hodnota nie je správne zadaná");
                return false;
            }
            return true;
        }

        public bool ValidateUrl(string key, string? value, bool isRequired)
        {
            if ((string.IsNullOrEmpty(value) && isRequired) || (!string.IsNullOrEmpty(value) && !Uri.IsWellFormedUriString(value, UriKind.Absolute)))
            {
                AddError(key, "URL adresa nie je platná");
                return false;
            }
            return true;
        }

        public bool ValidateNumber(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value) && !decimal.TryParse(value, System.Globalization.CultureInfo.CurrentCulture, out _))
            {
                AddError(key, "Číslo nie je platné");
                return false;
            }
            return true;
        }

        public bool ValidateDate(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value) && !DateOnly.TryParse(value, System.Globalization.CultureInfo.CurrentCulture, out _))
            {
                AddError(key, "Dátum nie je platný");
                return false;
            }
            return true;
        }

        public bool ValidateTemporalResolution(string key, string? value)
        {
            return true;
        }

        public bool ValidateApplicableLegislations(string key, List<string>? values)
        {
            bool isValid = true;
            if (values is not null)
            {
                foreach (string url in values)
                {
                    if (url is null || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || !uri.LocalPath.Contains("/eli/"))
                    {
                        AddError(key, "URL musí byť platné a obsahovať /eli/");
                        isValid = false;
                    }
                }
            }
            return isValid;
        }

        public async Task<bool> ValidateDataset(string key, string? id, string? childId, string publisher, IDocumentStorageClient documentStorage)
        {
            if (!string.IsNullOrEmpty(id) && Guid.TryParse(id, out Guid datasetId))
            {
                if (Guid.TryParse(childId, out Guid partId) && !await documentStorage.CanBeAddedToSerie(datasetId, partId))
                {
                    AddError(key, "Dataset nie je platný");
                    return false;
                }

                FileStorageQuery query = new FileStorageQuery
                {
                    OnlyPublishers = new List<string> { publisher },
                    OnlyTypes = new List<FileType> { FileType.DatasetRegistration },
                    OnlyIds = new List<Guid> { datasetId },
                    AdditionalFilters = new Dictionary<string, string[]>
                    {
                        { "serie", new string[] { "1" } },
                    }
                };
                FileStorageResponse response = await documentStorage.GetFileStates(query);
                if (response.Files.Count == 1)
                {
                    return true;
                }
                else
                {
                    AddError(key, "Dataset nie je platný");
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> ValidateEuroVocValues(string key, ICollection<string>? values, Dictionary<string, List<string>> labels)
        {
            RemoveEmptyValues(values);
            if (values is not null && values.Count > 0)
            {
                using HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });

                string[] languages = new[] { "sk", "en" };

                bool isValid = true;
                foreach (string? value in values)
                {
                    string? uri = value;

                    for (int maxRedirect = 0; maxRedirect < 10; maxRedirect++)
                    {
                        if (!string.IsNullOrEmpty(uri) && Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                        {
                            UriBuilder uriBuilder = new UriBuilder(uri);
                            if (uriBuilder.Scheme == "http")
                            {
                                uriBuilder.Scheme = "https";
                                uriBuilder.Port = 443;
                                uri = uriBuilder.Uri.ToString();
                            }

                            using HttpResponseMessage response = await client.GetAsync(uri);

                            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                            {
                                uri = response.Headers.Location?.ToString();
                            }
                            else if (response.IsSuccessStatusCode)
                            {
                                using Stream stream = await response.Content.ReadAsStreamAsync();
                                SkosConcept? concept = SkosConcept.ParseXml(stream);
                                if (concept is not null)
                                {
                                    foreach (string language in languages)
                                    {
                                        string? label = concept.GetLabel(language);
                                        if (!string.IsNullOrEmpty(label))
                                        {
                                            if (!labels.TryGetValue(language, out List<string>? languageLabels))
                                            {
                                                languageLabels = new List<string>();
                                                labels[language] = languageLabels;
                                            }
                                            languageLabels.Add(label);
                                        }
                                        else
                                        {
                                            isValid = false;
                                        }
                                    }
                                }
                                else
                                {
                                    isValid = false;
                                }
                            }
                            else
                            {
                                isValid = false;
                            }
                        }
                        else
                        {
                            isValid = false;
                        }
                    }
                }
                if (!isValid)
                {
                    AddError(key, "Hodnota musí byť zadaná z číselníka EuroVoc");
                }
                return isValid;
            }
            return true;
        }
    }
}
