using Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Xml;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace ValidationCore
{
    public partial class Validator
    {
        private static readonly Uri stringDataType = new Uri(XmlSpecsHelper.XmlSchemaDataTypeString);

        private static readonly Uri langStringDataType = new Uri(RdfSpecsHelper.RdfLangString);

        private static readonly Uri decimalDataType = new Uri(XmlSpecsHelper.XmlSchemaDataTypeDecimal);

        private static readonly Uri durationDataType = new Uri(XmlSpecsHelper.XmlSchemaDataTypeDuration);

        private static readonly Uri dateDataType = new Uri(XmlSpecsHelper.XmlSchemaDataTypeDate);

        private static readonly Uri dateTimeDataType = new Uri(XmlSpecsHelper.XmlSchemaDataTypeDateTime);

        private static readonly IReadOnlySet<Uri> textDataTypes = new HashSet<Uri> { stringDataType, langStringDataType };

        private static readonly string[] dateTimeFormats = [
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fK",
            "yyyy-MM-ddTHH:mm:ss.fZ",
            "yyyy-MM-ddTHH:mm:ss.ffK",
            "yyyy-MM-ddTHH:mm:ss.ffZ",
            "yyyy-MM-ddTHH:mm:ss.fffK",
            "yyyy-MM-ddTHH:mm:ss.fffZ"
        ];

        private static readonly string[] dateFormats = [
            "yyyy-MM-dd"
        ];

        public Validator(ValidationOptions options)
        {
            Options = options;
        }

        private ValidationOptions Options { get; }

        public async Task<ValidationResults> ValidateDataset(IGraph graph, Uri datasetId)
        {
            ValidationResults results = new ValidationResults();
            GraphContext context = new GraphContext(graph, graph.GetUriNode(datasetId));

            ValidateRdfTypes(results, context, [KnownPredicates.DcatDatasetClass, KnownPredicates.DcatDatasetSeriesClass]);

            bool isSerie = context.WithPredicate(KnownPredicates.RdfType).Triples.Any(t => t.Object is IUriNode u && UriEqualityComparer.Instance.Equals(u.Uri, KnownPredicates.DcatDatasetSeriesClass));
            bool isHvd = context.WithPredicate(KnownPredicates.DctType).Triples.Any(t => t.Object is IUriNode u && UriEqualityComparer.Instance.Equals(u.Uri, KnownPredicates.HvdType));

            HashSet<string> languages = new HashSet<string> { "sk" };
            foreach (ILiteralNode node in context.WithPredicate(KnownPredicates.DctTitle).Select(t => t.Object).OfType<ILiteralNode>())
            {
                if (!string.IsNullOrWhiteSpace(node.Language))
                {
                    languages.Add(node.Language.Trim());
                }
            }

            ValidateLanguageStrings(results, context.WithPredicate(KnownPredicates.DctTitle), languages, [CountValidationOptions.ExactlyOneOrError], Options.DefaultRequiredTextOptions);
            ValidateLanguageStrings(results, context.WithPredicate(KnownPredicates.DctDescription), languages, [CountValidationOptions.ExactlyOneOrError], Options.DefaultRequiredTextOptions);
            
            ValidateUris(results, context.WithPredicate(KnownPredicates.DctPublisher), u => u.OriginalString.StartsWith("https://data.gov.sk/id/legal-subject/"), "hodnota má byť v tvare <https://data.gov.sk/id/legal-subject/ico>", [CountValidationOptions.ExactlyOneOrError]);
            if (Options.AllowedPublishers is not null)
            {
                ValidateUris(results, context.WithPredicate(KnownPredicates.DctPublisher), Options.AllowedPublishers, []);
            }

            ValidateIssuedAndModified(results, context);
            
            await ValidateCodelist(results, context.WithPredicate(KnownPredicates.DcatTheme), KnownPredicates.ThemeCodelist, Options.DefaultCodelistItemValidator, [CountValidationOptions.AtLeastOneOrError], IgnoreEuroVocValues);
            await ValidateCodelist(results, context.WithPredicate(KnownPredicates.DctAccrualPeriodicity), KnownPredicates.FrequencyCodelist, Options.DefaultCodelistItemValidator, [CountValidationOptions.ExactlyOneOrError]);
            ValidateLanguageStrings(results, context.WithPredicate(KnownPredicates.DcatKeyword), languages, [CountValidationOptions.AtLeastOneOrError], Options.DefaultRequiredTextOptions);
            await ValidateCodelist(results, context.WithPredicate(KnownPredicates.DctType), KnownPredicates.TypeCodelist, Options.DefaultCodelistItemValidator, []);
            await ValidateCodelist(results, context.WithPredicate(KnownPredicates.DctSpatial), KnownPredicates.SpatialCodelist, Options.DefaultCodelistItemValidator, [CountValidationOptions.AtLeastOneOrError], _ => Task.FromResult(true), "NUTS");

            await ValidateTemporal(results, context.WithPredicate(KnownPredicates.DctTemporal), [CountValidationOptions.AtMostOneOrWarning]);
            await ValidateContactPoint(results, context.WithPredicate(KnownPredicates.DcatContactPoint), [CountValidationOptions.AtMostOneOrWarning]);
            ValidateDownloadableUriNodes(results, context.WithPredicate(KnownPredicates.DcatLandingPage), []);
            ValidateDownloadableUriNodes(results, context.WithPredicate(KnownPredicates.FoafPage), []);
            ValidateUris(results, context.WithPredicate(KnownPredicates.DctConformsTo), []);
            ValidateUris(results, context.WithPredicate(KnownPredicates.DctRelation), []);
            ValidateDecimal(results, context.WithPredicate(KnownPredicates.DcatSpatialResolutionInMeters), [], 0);
            ValidateDuration(results, context.WithPredicate(KnownPredicates.DcatTemporalResolution), [CountValidationOptions.AtMostOneOrError], TimeSpan.Zero);
            ValidateApplicableLegislation(results, context.WithPredicate(KnownPredicates.DcatApApplicableLegislation), isHvd);
            ValidateUris(results, context.WithPredicate(KnownPredicates.DcatApHvdCategory), isHvd ? [CountValidationOptions.AtLeastOneOrError] : [CountValidationOptions.NoneOrError]);

            List<IUriNode> inSeriesNodes = ValidateUriNodes(results, context.WithPredicate(KnownPredicates.DcatInSeries), []);
            foreach (IUriNode inSeriesNode in inSeriesNodes)
            {
                Uri parentDatasetId = inSeriesNode.Uri;
                if (!Options.CheckForDatasetSeriesCallback(parentDatasetId))
                {
                    results.Add(context, $"Dátová séria nebola nájdená '{parentDatasetId}'");
                }
            }

            await ValidateRelated(results, context.WithPredicate(KnownPredicates.DcatDistribution), isSerie ? [CountValidationOptions.NoneOrError] : [CountValidationOptions.AtLeastOneOrError], distributionContext => ValidateDistribution(results, distributionContext, isHvd));

            return results;
        }

        public async Task<ValidationResults> ValidateDistribution(IGraph graph, Uri datasetId, bool isDatasetHvd)
        {
            ValidationResults results = new ValidationResults();
            GraphContext context = new GraphContext(graph, graph.GetUriNode(datasetId));
            await ValidateDistribution(results, context, isDatasetHvd);
            return results;
        }

        public async Task ValidateDistribution(ValidationResults results, GraphContext context, bool isDatasetHvd)
        {
            bool hasDataSvc = false;
            HashSet<Uri> dataServiceEnpoints = new HashSet<Uri>(UriEqualityComparer.Instance);

            foreach (INode dataServiceNode in context.WithPredicate(KnownPredicates.DcatAccessService).Triples.Select(t => t.Object))
            {
                hasDataSvc = true;
                dataServiceEnpoints.UnionWith(context.Graph.GetTriplesWithSubjectPredicate(dataServiceNode, context.Graph.CreateUriNode(KnownPredicates.DcatEndpointUrl)).Select(t => t.Object).OfType<IUriNode>().Select(n => n.Uri));
            }

            await ValidateTermsOfUse(results, context.WithPredicate(KnownPredicates.LegTermsOfUse), [CountValidationOptions.ExactlyOneOrError]);
            List<Uri> downloadUris = ValidateDownloadableUriNodes(results, context.WithPredicate(KnownPredicates.DcatDownloadUrl), hasDataSvc ? [CountValidationOptions.AtMostOneOrError] : [CountValidationOptions.ExactlyOneOrError]);

            await ValidateCodelist(results, context.WithPredicate(KnownPredicates.DctFormat), KnownPredicates.FormatCodelist, Options.DefaultCodelistItemValidator, hasDataSvc ? [CountValidationOptions.AtMostOneOrError] : [CountValidationOptions.ExactlyOneOrError]);
            await ValidateCodelist(results, context.WithPredicate(KnownPredicates.DcatMediaType), KnownPredicates.MediaTypeCodelist, Options.DefaultCodelistItemValidator, hasDataSvc ? [CountValidationOptions.AtMostOneOrError] : [CountValidationOptions.ExactlyOneOrError]);
            await ValidateCodelist(results, context.WithPredicate(KnownPredicates.DcatPackageFormat), KnownPredicates.MediaTypeCodelist, Options.DefaultCodelistItemValidator, [CountValidationOptions.AtMostOneOrError]);
            await ValidateCodelist(results, context.WithPredicate(KnownPredicates.DcatCompressFormat), KnownPredicates.MediaTypeCodelist, Options.DefaultCodelistItemValidator, [CountValidationOptions.AtMostOneOrError]);

            ValidateApplicableLegislation(results, context.WithPredicate(KnownPredicates.DcatApApplicableLegislation), isDatasetHvd);

            HashSet<Uri> accessUrls = ValidateUris(results, context.WithPredicate(KnownPredicates.DcatAccessUrl), [CountValidationOptions.ExactlyOneOrError]).ToHashSet(UriEqualityComparer.Instance);

            if (hasDataSvc)
            {
                if (!accessUrls.SetEquals(dataServiceEnpoints))
                {
                    results.Add(context.WithPredicate(KnownPredicates.DcatAccessUrl), $"Distribúcia s dátovou službou by mala mať zhodný obsah dcat:accessURL ({FormatUriList(accessUrls)}) by mala byť zhodná s prístupovými bodmi dátovej služby ({FormatUriList(dataServiceEnpoints)})");
                }
            }
            else if (!accessUrls.SetEquals(downloadUris))
            {
                results.Add(context.WithPredicate(KnownPredicates.DcatAccessUrl), $"Distribúcia so súborom na stiahnutie by mala mať zhodný obsah dcat:accessURL ({FormatUriList(accessUrls)}) a dcat:downloadURL ({FormatUriList(downloadUris)})");
            }

            ValidateLanguageStrings(results, context.WithPredicate(KnownPredicates.DctTitle), [], [CountValidationOptions.AtMostOneOrError], Options.DefaultTextOptions);

            await ValidateRelated(results, context.WithPredicate(KnownPredicates.DcatAccessService), [CountValidationOptions.AtMostOneOrError], dataService => ValidateDataService(results, dataService, isDatasetHvd));
        }

        public async Task<ValidationResults> ValidateDataService(IGraph graph, Uri dataServiceId, bool isDatasetHvd)
        {
            ValidationResults results = new ValidationResults();
            GraphContext context = new GraphContext(graph, graph.GetUriNode(dataServiceId));
            await ValidateDataService(results, context, isDatasetHvd);
            return results;
        }

        public async Task ValidateDataService(ValidationResults results, GraphContext context, bool isDatasetHvd)
        {
            ValidateLanguageStrings(results, context.WithPredicate(KnownPredicates.DctTitle), [], [CountValidationOptions.ExactlyOneOrError], Options.DefaultRequiredTextOptions);
            ValidateDownloadableUriNodes(results, context.WithPredicate(KnownPredicates.DcatEndpointUrl), [CountValidationOptions.ExactlyOneOrError]);
            ValidateUris(results, context.WithPredicate(KnownPredicates.DcatApHvdCategory), isDatasetHvd ? [CountValidationOptions.AtLeastOneOrError] : [CountValidationOptions.NoneOrError]);
            ValidateApplicableLegislation(results, context.WithPredicate(KnownPredicates.DcatApApplicableLegislation), isDatasetHvd);
            await ValidateContactPoint(results, context.WithPredicate(KnownPredicates.DcatContactPoint), isDatasetHvd ? [CountValidationOptions.AtLeastOneOrError, CountValidationOptions.AtMostOneOrWarning] : [CountValidationOptions.AtMostOneOrWarning]);
            ValidateDownloadableUriNodes(results, context.WithPredicate(KnownPredicates.FoafPage), isDatasetHvd ? [CountValidationOptions.ExactlyOneOrError] : [CountValidationOptions.AtMostOneOrError]);
            ValidateUris(results, context.WithPredicate(KnownPredicates.DctConformsTo), []);
            ValidateDownloadableUriNodes(results, context.WithPredicate(KnownPredicates.DcatEndpointDescription), []);
        }

        private static async Task ValidateCodelist(ValidationResults results, GraphContext context, Uri codelist, ICodelistItemValidation validator, IEnumerable<CountValidationOptions> options, Func<Uri, Task<bool>>? ignoreValue = null, string? codelistName = null)
        {
            int count = 0;
            foreach (IUriNode node in GetAndValidateUriNodes(results, context))
            {
                if (await validator.IsValid(codelist, node.Uri))
                {
                    count++;
                }
                else if (ignoreValue is null || !await ignoreValue(node.Uri))
                {
                    results.Add(context, $"Hodnota '{node.Uri}' nie je z číselníka ({codelistName ?? codelist.OriginalString})");
                }
            }

            ValidateCount(results, context, count, options);
        }

        private static void ValidateNode(ValidationResults results, GraphContext context, INode node, NodeType nodeType)
        {
            if (node.NodeType != nodeType)
            {
                results.Add(context.WithObject(node), $"Nesprávny typ uzla, očakávaný typ '{TranslateNodeType(nodeType)}', skutočný typ '{TranslateNodeType(node.NodeType)}'");
            }
        }

        private static IEnumerable<T> GetAndValidateNodes<T>(ValidationResults results, GraphContext context, NodeType nodeType) where T : INode
        {
            foreach (INode n in context.Triples.Select(t => t.Object))
            {
                ValidateNode(results, context, n, nodeType);
                if (n is T typedNode)
                {
                    yield return typedNode;
                }
            }
        }

        private static IEnumerable<ILiteralNode> GetAndValidateLiteralNodes(ValidationResults results, GraphContext context, IReadOnlyCollection<Uri> expectedTypes, bool returnInvalidValues = true)
        {
            foreach (ILiteralNode n in GetAndValidateNodes<ILiteralNode>(results, context, NodeType.Literal))
            {
                bool isValid = false;

                if (expectedTypes.Count > 0)
                {
                    if (n.DataType is not null)
                    {
                        if (!expectedTypes.Any(t => UriEqualityComparer.Instance.Equals(n.DataType, t)))
                        {
                            results.Add(context.WithObject(n), $"Nie je definovaný správny dátový typ pre doslovný uzol (literal node), očakávané typy ({string.Join(", ", expectedTypes.Select(t => $"'{t.OriginalString}'"))}), skutočný typ '{n.DataType.OriginalString}'");
                        }
                        else
                        {
                            isValid = true;
                        }
                    }
                    else
                    {
                        results.Add(context.WithObject(n), $"Nie je definovaný žiadny dátový typ pre doslovný uzol (literal node), očakávané typy ({string.Join(", ", expectedTypes.Select(t => $"'{t.OriginalString}'"))})");
                    }
                }
                else
                {
                    isValid = true;
                }

                if (isValid || returnInvalidValues)
                {
                    yield return n;
                }
            }
        }

        private static List<IUriNode> GetAndValidateUriNodes(ValidationResults results, GraphContext context)
        {
            HashSet<Uri> uris = new HashSet<Uri>(UriEqualityComparer.Instance);
            List<IUriNode> nodes = GetAndValidateNodes<IUriNode>(results, context, NodeType.Uri).ToList();
            foreach (IUriNode n in nodes)
            {
                if (!uris.Add(n.Uri))
                {
                    results.Add(context.WithObject(n), $"Hodnota '{n.Uri}' je duplicitná");
                }
            }
            return nodes;
        }

        private static bool ValidateText(ValidationResults results, GraphContext context, string? value, IEnumerable<TextValidationOptions> options)
        {
            bool isValid = true;
            if (options is not null)
            {
                foreach (TextValidationOptions o in options)
                {
                    if (!ValidateText(results, context, value, o))
                    {
                        isValid = false;
                    }
                }
            }
            return isValid;
        }

        private static bool ValidateText(ValidationResults results, GraphContext context, string? value, TextValidationOptions options)
        {
            bool isValid = true;

            if (!options.EnableWhitespaceStartOrEnd && !string.IsNullOrEmpty(value) && value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1])))
            {
                results.Add(context, "Text nesmie začítať alebo končiť prázdnym znakom", options.Level);
                isValid = false;
            }

            if (options.TrimWhitespace)
            {
                value = value?.Trim();
            }
            
            int len = value?.Length ?? 0;

            if (len < options.MinimalLangth)
            {
                results.Add(context, $"Textová hodnota '{value}' je príliš krátka, očakávaná min. dĺžka {options.MinimalLangth} znakov, skutočná dĺžka {len} znakov", options.Level);
                isValid = false;
            }

            if (options.MaximalLength.HasValue && len > options.MaximalLength.Value)
            {
                results.Add(context, $"Textová hodnota '{value}' je príliš dlhá, očakávaná max. dĺžka {options.MaximalLength.Value} znakov, skutočná dĺžka {len} znakov", options.Level);
                isValid = false;
            }

            return isValid;
        }

        private void ValidateLanguageStrings(ValidationResults results, GraphContext context, IEnumerable<string> checkLanguages, IEnumerable<CountValidationOptions> countPerLanguage, IEnumerable<TextValidationOptions> textOptions)
        {
            countPerLanguage ??= [];
            Dictionary<string, HashSet<string>> usedLanguages = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (ILiteralNode n in GetAndValidateLiteralNodes(results, context, textDataTypes))
            {
                string language = n.Language?.Trim() ?? string.Empty;
                GraphContext nodeContext = context.WithObject(n);

                if (string.IsNullOrEmpty(language))
                {
                    results.Add(context.WithObject(n), "Nie je definovaný jazyk pre hodnotu", string.IsNullOrEmpty(Options.DefaultLangauge) ? ValidationErrorLevel.Error : ValidationErrorLevel.Information);
                    language = Options.DefaultLangauge ?? string.Empty;
                }

                if (!string.IsNullOrEmpty(language))
                {
                    if (!usedLanguages.TryGetValue(language, out HashSet<string>? langTexts))
                    {
                        langTexts = new HashSet<string>(StringComparer.Ordinal);
                        usedLanguages[language] = langTexts;
                    }

                    ValidateText(results, nodeContext, n.Value, textOptions);

                    if (!string.IsNullOrEmpty(n.Value) && !langTexts.Add(n.Value))
                    {
                        results.Add(nodeContext, $"Hodnota '{n.Value}' je duplicitná");
                    }
                }
            }

            HashSet<string> languages = new HashSet<string>(checkLanguages, StringComparer.OrdinalIgnoreCase);
            languages.UnionWith(usedLanguages.Keys);

            if (countPerLanguage is not null && countPerLanguage.Any())
            {
                foreach (string lang in languages)
                {
                    int count = 0;
                    if (usedLanguages.TryGetValue(lang, out HashSet<string>? langTexts))
                    {
                        count = langTexts.Count;
                    }

                    foreach (CountValidationOptions o in countPerLanguage)
                    {
                        if (count < o.Minimum)
                        {
                            results.Add(context, $"Nie je definovaný dostatočný počet hodnôt pre jazyk '{lang}', očakávaný min. počet {o.Minimum}, skutočný počet {count}", o.Level);
                        }
                        else if (o.Maximum.HasValue && count > o.Maximum.Value)
                        {
                            results.Add(context, $"Pre jazyk '{lang}' je definovaných príliš veľa hodnôt, očakávaný max. počet {o.Maximum.Value}, skutočný počet {count}", o.Level);
                        }
                    }
                }
            }
        }

        public static void ValidateRdfType(ValidationResults results, GraphContext context, Uri type) => ValidateRdfTypes(results, context, [type]);

        public static void ValidateRdfTypes(ValidationResults results, GraphContext context, IEnumerable<Uri> expectedTypes)
        {
            List<IUriNode> nodes = GetAndValidateUriNodes(results, context.WithPredicate(KnownPredicates.RdfType));
            HashSet<Uri> types = new HashSet<Uri>(nodes.Select(n => n.Uri), UriEqualityComparer.Instance);
            if (types.Count == 0)
            {
                results.Add(context, $"Objekt nemá správny RDF typ, očakávaný aspoň jeden typ zo zoznamu ({string.Join(", ", expectedTypes)}), objekt nemá  žiadne RDF typy");
            }
            else if (!types.Overlaps(expectedTypes))
            {
                results.Add(context, $"Objekt nemá správny RDF typ, očakávaný aspoň jeden typ zo zoznamu ({string.Join(", ", expectedTypes)}), objekt má RDF typy ({string.Join(", ", types)})");
            }
        }

        private static void ValidateCount(ValidationResults results, GraphContext context, int count, IEnumerable<CountValidationOptions> expectedCountOptions)
        {
            if (expectedCountOptions is not null)
            {
                foreach (CountValidationOptions options in expectedCountOptions)
                {
                    ValidateCount(results, context, count, options);
                }
            }
        }

        private static void ValidateCount(ValidationResults results, GraphContext context, int count, CountValidationOptions expectedCount)
        {
            if (count < expectedCount.Minimum)
            {
                results.Add(context, $"Nie je definovaný dostatočný počet platných hodnôt, očakávaný min. počet {expectedCount.Minimum}, skutočný počet {count}", expectedCount.Level);
            }
            else if (expectedCount.Maximum.HasValue && count > expectedCount.Maximum.Value)
            {
                results.Add(context, $"Je definovaných príliš veľa hodnôt, očakávaný max. počet {expectedCount.Maximum.Value}, skutočný počet {count}", expectedCount.Level);
            }
        }

        public static List<IUriNode> ValidateUriNodes(ValidationResults results, GraphContext context, IEnumerable<CountValidationOptions> expectedCountOptions)
        {
            List<IUriNode> nodes = GetAndValidateUriNodes(results, context);
            ValidateCount(results, context, nodes.Count, expectedCountOptions);
            return nodes;
        }

        public static List<Uri> ValidateUris(ValidationResults results, GraphContext context, IEnumerable<CountValidationOptions> expectedCountOptions)
        {
            List<IUriNode> nodes = GetAndValidateUriNodes(results, context);
            ValidateCount(results, context, nodes.Count, expectedCountOptions);
            List<Uri> uris = new List<Uri>(nodes.Count);
            foreach (IUriNode node in nodes)
            {
                uris.Add(node.Uri);
            }
            return uris;
        }

        public static List<Uri> ValidateUris(ValidationResults results, GraphContext context, IReadOnlyCollection<Uri> expectedUris, IEnumerable<CountValidationOptions> expectedCountOptions, string? listDescription = null)
        {
            List<IUriNode> nodes = GetAndValidateUriNodes(results, context);
            List<Uri> uris = new List<Uri>(nodes.Count);
            ValidateCount(results, context, nodes.Count, expectedCountOptions);

            Dictionary<Uri, IUriNode> nodesByUri = new Dictionary<Uri, IUriNode>(nodes.Count, UriEqualityComparer.Instance);
            foreach (IUriNode node in nodes)
            {
                nodesByUri[node.Uri] = node;
                uris.Add(node.Uri);
            }

            foreach (Uri uri in expectedUris)
            {
                nodesByUri.Remove(uri);
            }

            if (nodesByUri.Count > 0)
            {
                if (string.IsNullOrEmpty(listDescription))
                {
                    if (expectedUris.Count > 5)
                    {
                        listDescription = $"(napr. {string.Join(", ", expectedUris.Take(5).Select(u => $"'{u}'"))})";
                    }
                    else
                    {
                        listDescription = $"({string.Join(", ", expectedUris.Select(u => $"'{u}'"))})";
                    }
                }

                foreach ((Uri uri, IUriNode node) in nodesByUri)
                {
                    results.Add(context.WithObject(node), $"Hodnota '{uri}' nie je uvedená v zozname {listDescription}");
                }
            }

            return uris;
        }

        public static List<Uri> ValidateUris(ValidationResults results, GraphContext context, Func<Uri, bool> customValidator, string errorMessage, IEnumerable<CountValidationOptions> expectedCountOptions)
        {
            List<IUriNode> nodes = GetAndValidateUriNodes(results, context);
            List<Uri> uris = new List<Uri>(nodes.Count);
            ValidateCount(results, context, nodes.Count, expectedCountOptions);
            foreach (IUriNode node in nodes)
            {
                if (!customValidator(node.Uri))
                {
                    results.Add(context.WithObject(node), $"Hodnota '{node.Uri}' nie je platná ({errorMessage})");
                }
                else
                {
                    uris.Add(node.Uri);
                }
            }
            return uris;
        }

        private static string TranslateNodeType(NodeType nodeType)
        {
            return nodeType switch 
            { 
                NodeType.Blank => "Prázdny uzol (blank node)",
                NodeType.Uri => "URI/IRI",
                NodeType.Literal => "Doslovný uzol (literal node)",
                _ => Enum.GetName(nodeType) ?? "neznámy",
            };
        }

        private static Task<bool> IgnoreEuroVocValues(Uri uri) => Task.FromResult(uri.OriginalString.StartsWith(KnownPredicates.EuroVocPrefix.OriginalString));

        private static async Task ValidateRelated(ValidationResults results, GraphContext context, IEnumerable<CountValidationOptions> expectedCountOptions, Func<GraphContext, Task> innerValidator, bool requireUriNode = false)
        {
            int count = 0;
            foreach (INode node in context.Triples.Select(t => t.Object))
            {
                if (requireUriNode)
                {
                    ValidateNode(results, context, node, NodeType.Uri);
                }
                await innerValidator(context.WithNextSubject(node));
                count++;
            }
            ValidateCount(results, context, count, expectedCountOptions);
        }

        private async Task ValidateTemporal(ValidationResults results, GraphContext outContext, IEnumerable<CountValidationOptions> expectedCountOptions)
        {
            await ValidateRelated(results, outContext, expectedCountOptions, context =>
            {
                ValidateRdfTypes(results, context, [KnownPredicates.DctPeriodOfTime]);
                DateTimeOffset? start = GetAndValidateDate(results, context.WithPredicate(KnownPredicates.DcatStartDate), [CountValidationOptions.AtMostOneOrError], Options.MinimalDateTime, Options.MaximalDateTime);
                GetAndValidateDate(results, context.WithPredicate(KnownPredicates.DcatEndDate), [CountValidationOptions.AtMostOneOrError], start ?? Options.MinimalDateTime, Options.MaximalDateTime);
                return Task.CompletedTask;
            });
        }

        private async Task ValidateContactPoint(ValidationResults results, GraphContext outContext, IEnumerable<CountValidationOptions> expectedCountOptions)
        {
            await ValidateRelated(results, outContext, expectedCountOptions, context =>
            {
                ValidateRdfTypes(results, context, [KnownPredicates.VcardIndividual, KnownPredicates.VcardOrganization]);
                ValidateLanguageStrings(results, context.WithPredicate(KnownPredicates.VcardFn), Options.DefaultRequiredLangauges, [CountValidationOptions.AtMostOneOrError], Options.DefaultTextOptions);
                ValidateEmailNodes(results, context.WithPredicate(KnownPredicates.VcardHasEmail), [CountValidationOptions.AtMostOneOrError]);
                return Task.CompletedTask;
            });
        }

        private async Task ValidateTermsOfUse(ValidationResults results, GraphContext outContext, IEnumerable<CountValidationOptions> expectedCountOptions)
        {
            await ValidateRelated(results, outContext, expectedCountOptions, async context =>
            {
                ValidateRdfTypes(results, context, [KnownPredicates.LegTermsOfUseClass]);
                await ValidateCodelist(results, context.WithPredicate(KnownPredicates.LegAuthorsWorkType), KnownPredicates.LicenseCodelist, Options.DefaultCodelistItemValidator, [CountValidationOptions.ExactlyOneOrError]);
                await ValidateCodelist(results, context.WithPredicate(KnownPredicates.LegOriginalDatabaseType), KnownPredicates.LicenseCodelist, Options.DefaultCodelistItemValidator, [CountValidationOptions.ExactlyOneOrError]);
                await ValidateCodelist(results, context.WithPredicate(KnownPredicates.LegDatabaseProtectedBySpecialRightsType), KnownPredicates.LicenseCodelist, Options.DefaultCodelistItemValidator, [CountValidationOptions.ExactlyOneOrError]);
                await ValidateCodelist(results, context.WithPredicate(KnownPredicates.LegPersonalDataContainmentType), KnownPredicates.PersonalDataContainmentTypeCodelist, Options.DefaultCodelistItemValidator, [CountValidationOptions.ExactlyOneOrError]);
                ValidateLanguageStrings(results, context.WithPredicate(KnownPredicates.LegAuthorName), Options.DefaultRequiredLangauges, [CountValidationOptions.AtMostOneOrError], Options.DefaultTextOptions);
                ValidateLanguageStrings(results, context.WithPredicate(KnownPredicates.LegOriginalDatabaseAuthorName), Options.DefaultRequiredLangauges, [CountValidationOptions.AtMostOneOrError], Options.DefaultTextOptions);
            });
        }

        private static DateTimeOffset? GetAndValidateDate(ValidationResults results, GraphContext context, IEnumerable<CountValidationOptions> expectedCountOptions, DateTimeOffset? min = null, DateTimeOffset? max = null)
        {
            List<ILiteralNode> nodes = GetAndValidateLiteralNodes(results, context, [dateDataType]).ToList();
            ValidateCount(results, context, nodes.Count, expectedCountOptions);
            if (nodes.Count > 0)
            {
                ILiteralNode node = nodes[0];
                string value = node.Value ?? string.Empty;
                if (DateTimeOffset.TryParseExact(value, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTimeOffset time))
                {
                    if (min.HasValue && time < min.Value)
                    {
                        results.Add(context.WithObject(node), $"Hodnota '{time.LocalDateTime:G}' musí byť najskôr '{min.Value.LocalDateTime:G}'");
                    }

                    if (max.HasValue && time > max.Value)
                    {
                        results.Add(context.WithObject(node), $"Hodnota '{time.LocalDateTime:G}' musí byť najneskôr '{max.Value.LocalDateTime:G}'");
                    }

                    return time;
                }
            }
            return null;
        }

        private static DateTimeOffset? GetAndValidateDateTime(ValidationResults results, GraphContext context, IEnumerable<CountValidationOptions> expectedCountOptions, DateTimeOffset? min = null, DateTimeOffset? max = null)
        {
            List<ILiteralNode> nodes = GetAndValidateLiteralNodes(results, context, [dateTimeDataType]).ToList();
            ValidateCount(results, context, nodes.Count, expectedCountOptions);
            if (nodes.Count > 0)
            {
                ILiteralNode node = nodes[0];
                string value = node.Value ?? string.Empty;
                if (DateTimeOffset.TryParseExact(value, dateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTimeOffset time))
                {
                    if (min.HasValue && time < min.Value)
                    {
                        results.Add(context.WithObject(node), $"Hodnota '{time.LocalDateTime:G}' musí byť najskôr '{min.Value.LocalDateTime:G}'");
                    }

                    if (max.HasValue && time > max.Value)
                    {
                        results.Add(context.WithObject(node), $"Hodnota '{time.LocalDateTime:G}' musí byť najneskôr '{max.Value.LocalDateTime:G}'");
                    }

                    return time;
                }
            }
            return null;
        }

        private static decimal? ValidateDecimal(ValidationResults results, GraphContext context, IEnumerable<CountValidationOptions> expectedCountOptions, decimal? min = null, decimal? max = null)
        {
            List<ILiteralNode> nodes = GetAndValidateLiteralNodes(results, context, [decimalDataType]).ToList();
            ValidateCount(results, context, nodes.Count, expectedCountOptions);
            if (nodes.Count > 0)
            {
                ILiteralNode node = nodes[0];
                string text = node.Value ?? string.Empty;
                try
                {
                    decimal number = XmlConvert.ToDecimal(text);
                    if (min.HasValue && number < min.Value)
                    {
                        results.Add(context.WithObject(node), $"Hodnota '{text}' musí byť min. '{min.Value}'");
                    }

                    if (max.HasValue && number > max.Value)
                    {
                        results.Add(context.WithObject(node), $"Hodnota '{text}' musí byť max. '{max.Value}'");
                    }

                    return number;
                }
                catch
                {
                    results.Add(context.WithObject(node), $"Hodnota '{text}' nie je správne formátované číslo");
                }
            }
            return null;
        }

        private static TimeSpan? ValidateDuration(ValidationResults results, GraphContext context, IEnumerable<CountValidationOptions> expectedCountOptions, TimeSpan? min = null, TimeSpan? max = null)
        {
            List<ILiteralNode> nodes = GetAndValidateLiteralNodes(results, context, [durationDataType]).ToList();
            ValidateCount(results, context, nodes.Count, expectedCountOptions);
            if (nodes.Count > 0)
            {
                ILiteralNode node = nodes[0];
                string text = node.Value ?? string.Empty;
                try
                {
                    TimeSpan number = XmlConvert.ToTimeSpan(text);
                    if (min.HasValue && number < min.Value)
                    {
                        results.Add(context.WithObject(node), $"Hodnota '{text}' musí byť min. '{min.Value}'");
                    }

                    if (max.HasValue && number > max.Value)
                    {
                        results.Add(context.WithObject(node), $"Hodnota '{text}' musí byť max. '{max.Value}'");
                    }

                    return number;
                }
                catch
                {
                    results.Add(context.WithObject(node), $"Hodnota '{text}' nie je správne formátovaný hodnota xsd:duration");
                }
            }
            return null;
        }

        private static List<Uri> ValidateDownloadableUriNodes(ValidationResults results, GraphContext context, IEnumerable<CountValidationOptions> expectedCountOptions)
        {
            List<IUriNode> nodes = GetAndValidateUriNodes(results, context);
            List<Uri> uris = new List<Uri>(nodes.Count);
            ValidateCount(results, context, nodes.Count, expectedCountOptions);
            foreach (IUriNode node in nodes)
            {
                if (!(node.Uri.Scheme == "http" || node.Uri.Scheme == "https"))
                {
                    results.Add(context.WithObject(node), $"Hodnota '{node.Uri}' nie je odkaz na stránku http:// alebo https://");
                }
                else
                {
                    uris.Add(node.Uri);
                }
            }
            return uris;
        }

        private static bool IsValidEmailAddress(string address)
        {
            return EmailRegex().IsMatch(address);
        }

        private static void ValidateEmailNodes(ValidationResults results, GraphContext context, IEnumerable<CountValidationOptions> expectedCountOptions)
        {
            List<IUriNode> nodes = GetAndValidateUriNodes(results, context);
            ValidateCount(results, context, nodes.Count, expectedCountOptions);
            foreach (IUriNode node in nodes)
            {
                if (node.Uri.Scheme != "mailto")
                {
                    results.Add(context.WithObject(node), $"Hodnota '{node.Uri}' nie je URI v tvare mailto://");
                }
                else 
                {
                    if (node.Uri.OriginalString.Length >= 9)
                    {
                        string emailAddress = node.Uri.OriginalString[9..];
                        if (!IsValidEmailAddress(emailAddress))
                        {
                            results.Add(context.WithObject(node), $"Hodnota '{emailAddress}' nie platná e-mailová adresa");
                        }
                    } 
                    else
                    {
                        results.Add(context.WithObject(node), $"Hodnota '{node.Uri.OriginalString}' nie je v platnom tvare mailto://");
                    }
                }
            }
        }

        private static void ValidateApplicableLegislation(ValidationResults results, GraphContext context, bool isHvd)
        {
            foreach (IUriNode node in GetAndValidateUriNodes(results, context))
            {
                if (UriEqualityComparer.Instance.Equals(KnownPredicates.HvdDatasetLegislation, node.Uri))
                {
                    if (!isHvd)
                    {
                        results.Add(context.WithObject(node), $"Dataset nie je označný ako HVD a preto nemôže mať legislatívu <{KnownPredicates.HvdDatasetLegislation}>");
                    }
                    return;
                }
            }

            if (isHvd)
            {
                results.Add(context, $"Dataset je označný ako HVD a preto musí mať legislatívu <{KnownPredicates.HvdDatasetLegislation}>");
            }
        }

        private void ValidateIssuedAndModified(ValidationResults results, GraphContext context)
        {
            DateTimeOffset? issued = GetAndValidateDateTime(results, context.WithPredicate(KnownPredicates.DctIssued), [CountValidationOptions.AtMostOneOrError], Options.MinimalDateTime, DateTimeOffset.Now);
            DateTimeOffset? modified = GetAndValidateDateTime(results, context.WithPredicate(KnownPredicates.DctModified), [CountValidationOptions.AtMostOneOrError], Options.MinimalDateTime, DateTimeOffset.Now);

            if (issued.HasValue && modified.HasValue && modified.Value < issued.Value)
            {
                results.Add(context, $"Dátum modifikácie '{modified.Value.LocalDateTime:g}' nemôže byť skôr ako dátum vydania '{issued.Value.LocalDateTime:g}'");
            }
        }

        private static string FormatUriList(IEnumerable<Uri> uris) => string.Join(", ", uris.Select(u => $"'{u.OriginalString}'"));

        [GeneratedRegex(@"(?: [a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])")]
        private static partial Regex EmailRegex();
    }
}