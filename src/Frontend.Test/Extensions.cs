using AngleSharp.Dom;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace Frontend.Test
{
    public static class Extensions
    {
        internal static async Task Login(this IPage page, WebApiApplicationFactory factory, string? publisherId, string role, string? companyName = null, bool createPublisherIfNeeded = true, bool waitForHomePage = true)
        {
            using HttpClient client = factory.CreateDefaultClient();
            string token = factory.CreateUserAndToken(role, publisherId, companyName, createPublisherIfNeeded);

            await page.GotoAsync("http://localhost:6001/");

            await page.RunAndWaitForRequests(async () =>
            {
                await page.EvaluateAsync($"document.write('<form action=\"/saml/consume\" method=\"post\" id=\"token-form\"><input type=\"hidden\" name=\"token\" value=\"{token}\"></form>');document.getElementById('token-form').submit();");
                await page.WaitForURLAsync("http://localhost:6001/saml/consume");

                if (waitForHomePage)
                {
                    await page.WaitForURLAsync("http://localhost:6001/");
                }
            }, new List<string> { "user-info" });
        }

        public static async Task AssertNoTable(this IPage test)
        {
            Assert.IsNull(await test.QuerySelectorAsync("table"));
        }

        public static async Task AssertTableRowsCount(this IPage test, int count)
        {
            IElementHandle? table = await test.QuerySelectorAsync("table");
            Assert.IsNotNull(table);
            IReadOnlyList<IElementHandle> rows = await table.QuerySelectorAllAsync("tbody tr");
            Assert.AreEqual(count, rows.Count);
        }

        public static async Task OpenMenu(this IPage test, params string[] items)
        {
            IElementHandle? parent = await test.QuerySelectorAsync("header");

            async Task<IElementHandle?> Find(string linkName)
            {
                if (parent == null)
                {
                    return null;
                }

                foreach (IElementHandle li in await parent.QuerySelectorAllAsync("li"))
                {
                    foreach (IElementHandle link in await li.QuerySelectorAllAsync("a"))
                    {
                        if (await link.TextContentAsync() == linkName)
                        {
                            return li;
                        }
                    }                    
                }
                return null;
            }

            foreach (string item in items)
            {
                parent = await Find(item);
                Assert.IsNotNull(parent);
                IElementHandle? link = await parent.QuerySelectorAsync("a");
                Assert.IsNotNull(link);
                await link.ClickAsync();
            }
        }

        public static async Task OpenDatasetsAdmin(this IPage test)
        {
            await test.RunAndWaitForDatasetList(async () =>
            {
                await test.OpenMenu("Správa", "Datasety");
            });

            Assert.AreEqual("Národný katalóg otvorených dát - Zoznam datasetov", await test.TitleAsync());
            Assert.AreEqual("Zoznam datasetov", await (await test.QuerySelectorAsync("h1"))!.TextContentAsync());
        }

        public static async Task OpenLocalCatalogsAdmin(this IPage test)
        {
            await test.RunAndWaitForLocalCatalogList(async () =>
            {
                await test.OpenMenu("Správa", "Lokálne katalógy");
            });

            Assert.AreEqual("Národný katalóg otvorených dát - Zoznam lokálnych katalógov", await test.TitleAsync());
            Assert.AreEqual("Zoznam lokálnych katalógov", await (await test.QuerySelectorAsync("h1"))!.TextContentAsync());
        }

        public static async Task OpenUsersAdmin(this IPage test)
        {
            await test.RunAndWaitForUserList(async () =>
            {
                await test.OpenMenu("Správa", "Používatelia");
            });

            Assert.AreEqual("Národný katalóg otvorených dát - Zoznam používateľov", await test.TitleAsync());
            Assert.AreEqual("Zoznam používateľov", await (await test.QuerySelectorAsync("h1"))!.TextContentAsync());
        }

        public static async Task OpenPublishersAdmin(this IPage test)
        {
            await test.RunAndWaitForPublisherList(async () =>
            {
                await test.OpenMenu("Správa", "Poskytovatelia dát");
            });

            Assert.AreEqual("Národný katalóg otvorených dát - Poskytovatelia dát", await test.TitleAsync());
            Assert.AreEqual("Poskytovatelia dát", await (await test.QuerySelectorAsync("h1"))!.TextContentAsync());
        }

        public static async Task OpenDistributionsAdmin(this IPage test, int datasetRowIndex)
        {
            await test.OpenDatasetsAdmin();

            await test.ClickOnTableButton(datasetRowIndex, "Zmeniť distribúcie");

            Assert.AreEqual("Národný katalóg otvorených dát - Zoznam distribúcií", await test.TitleAsync());
            Assert.AreEqual("Zoznam distribúcií", await (await test.QuerySelectorAsync("h1"))!.TextContentAsync());
        }

        public static async Task ClickOnTableButton(this IPage test, int rowIndex, string buttonName)
        {
            ILocator row = test.Locator($"table tbody tr:nth-child({rowIndex + 1})");
            await row.GetByText(buttonName).ClickAsync();
        }

        public static async Task WaitForLoadingDone(this IPage test)
        {
            while (true)
            {
                if (await test.GetByTestId("loading").CountAsync() == 0)
                {
                    break;
                }
                await Task.Delay(25);
            }
        }

        public static async Task<IElementHandle?> GetByFieldsetLegend(this IPage test, string text)
        {
            foreach (IElementHandle element in await test.QuerySelectorAllAsync("fieldset"))
            {
                IElementHandle? legend = await element.QuerySelectorAsync("legend");
                string? legendText = legend is not null ? await legend.TextContentAsync() : null;
                if (legendText == text)
                {
                    return element;
                }
            }
            return null;
        }

        public static async Task<IReadOnlyList<string>> GetAlerts(this IPage test)
        {
            IReadOnlyList<IElementHandle> alerts = await test.QuerySelectorAllAsync(".custom-alert");
            List<string> texts = new List<string>();
            foreach (IElementHandle alert in alerts)
            {
                texts.Add(await alert.TextContentAsync() ?? string.Empty);
            }
            return texts;
        }

        public static async Task<string?> GetMultiRadioSelectedLabel(this IPage test, string legendText)
        {
            IElementHandle? fieldset = await test.GetByFieldsetLegend(legendText);
            if (fieldset is not null)
            {
                IElementHandle? input = await fieldset.QuerySelectorAsync("input:checked");
                if (input is not null)
                {
                    return await input.GetAttributeAsync("label");
                }
            }
            return null;
        }

        public static async Task<IElementHandle?> GetInputByLabel(IElementHandle parent, string label)
        {
            IReadOnlyList<IElementHandle> labels = await parent.QuerySelectorAllAsync("label");
            IReadOnlyList<IElementHandle> inputs = await parent.QuerySelectorAllAsync("input");

            int index = 0;
            foreach (IElementHandle l in labels)
            {
                if (await l.TextContentAsync() == label)
                {
                    return inputs[index];
                }
                index++;
            }
         
            foreach (IElementHandle element in await parent.QuerySelectorAllAsync("label"))
            {
                string? labelText = await element.TextContentAsync();
                if (labelText == label)
                {
                    return await element.QuerySelectorAsync("input");
                }
            }
            return null;
        }

        public static async Task<IElementHandle?> GetFormElementGroup(this IPage test, string label)
        {
            foreach (IElementHandle element in await test.QuerySelectorAllAsync(".govuk-form-group"))
            {
                IElementHandle? labelElement = await element.QuerySelectorAsync("label");
                Assert.IsNotNull(labelElement);
                string? labelText = await labelElement.TextContentAsync();
                if (labelText == label)
                {
                    return element;
                }
            }
            return null;
        }

        public static async Task<IElementHandle> GetInputInFormElementGroup(this IPage test, string label)
        {
            IElementHandle? group = await test.GetFormElementGroup(label);
            Assert.IsNotNull(group);
            return (await group.QuerySelectorAsync("input"))!;
        }

        public static async Task<IElementHandle> GetTextareaInFormElementGroup(this IPage test, string label)
        {
            IElementHandle? group = await test.GetFormElementGroup(label);
            Assert.IsNotNull(group);
            return (await group.QuerySelectorAsync("textarea"))!;
        }

        public static async Task<List<string>> GetTags(IElementHandle parent)
        {
            List<string> items = new List<string>();
            foreach (IElementHandle element in await parent.QuerySelectorAllAsync(".nkod-entity-detail-tag"))
            {
                items.Add((await element.TextContentAsync())!);
            }
            return items;
        }

        public static async Task<List<string>> GetMultiSelectItems(this IPage test, string label)
        {
            IElementHandle? group = await test.GetFormElementGroup(label);
            Assert.IsNotNull(group);
            List<string> items = new List<string>();
            foreach (IElementHandle element in await group.QuerySelectorAllAsync(".nkod-entity-detail-tag"))
            {
                items.Add((await element.GetAttributeAsync("data-value"))!);
            }
            return items;
        }

        public static async Task<IElementHandle?> GetSelectInFormElementGroup(this IPage test, string label)
        {
            IElementHandle? group = await test.GetFormElementGroup(label);
            Assert.IsNotNull(group);
            return await group.QuerySelectorAsync("select");
        }

        public static async Task<string?> GetSelectItemFormElementGroup(this IPage test, string label)
        {
            IElementHandle? group = await test.GetFormElementGroup(label);
            Assert.IsNotNull(group);
            IElementHandle? selectedOption = await group.QuerySelectorAsync("select option:checked");
            return selectedOption is not null ? await selectedOption.GetAttributeAsync("value") : null;
        }

        public static async Task<IElementHandle?> GetRadioInFormElementGroup(this IPage test, string groupLabel, string radioLabel)
        {
            IElementHandle? group = await test.GetFormElementGroup(groupLabel);
            Assert.IsNotNull(group);
            return await GetInputByLabel(group, radioLabel);
        }

        public static async Task FillMultiSelect(IElementHandle group, IEnumerable<string> values)
        {
            foreach (IElementHandle t in await group.QuerySelectorAllAsync(".nkod-entity-detail-tag"))
            {
                await t.ClickAsync();
            }

            foreach (string value in values)
            {
                IElementHandle? select = await group.QuerySelectorAsync("select");
                Assert.IsNotNull(select);
                await select.SelectOptionAsync(value);
                IElementHandle? button = await group.QuerySelectorAsync("button");
                Assert.IsNotNull(button);
                await button.ClickAsync();
            }
        }

        public static async Task FillMultiInput(IElementHandle group, IEnumerable<string> values)
        {
            foreach (IElementHandle t in await group.QuerySelectorAllAsync(".nkod-entity-detail-tag"))
            {
                await t.ClickAsync();
            }

            foreach (string value in values)
            {
                IElementHandle? input = await group.QuerySelectorAsync("input");
                Assert.IsNotNull(input);
                await input.FillAsync(value);
                IElementHandle? button = await group.QuerySelectorAsync("button");
                Assert.IsNotNull(button);
                await button.ClickAsync();
            }
        }

        public static async Task<List<string>> GetSelectOptions(IElementHandle select)
        {
            List<string> values = new List<string>();
            foreach (IElementHandle option in await select.QuerySelectorAllAsync("option"))
            {
                values.Add((await option.GetAttributeAsync("value"))!);
            }
            return values;
        }

        private static async Task AssertLanguageValues<T>(this IPage test, string label, Dictionary<string, T> values, Func<IElementHandle, T, Task> testAction)
        {
            IElementHandle? group = await test.QuerySelectorAsync($".govuk-form-group[data-label=\"{label}\"]");
            Assert.IsNotNull(group);

            foreach (IElementHandle t in await group.QuerySelectorAllAsync(".language-input"))
            {
                string? lang = (await t.GetAttributeAsync("data-lang"));
                Assert.IsNotNull(lang);

                if (!values.ContainsKey(lang))
                {
                    throw new Exception($"Unexpected language input element {label}:{lang}");
                }
            }

            foreach ((string lang, T value) in values)
            {
                IElementHandle? handle = await group.QuerySelectorAsync($".language-input[data-lang=\"{lang}\"]");
                Assert.IsNotNull(handle);
                await testAction(handle, value);
            }
        }


        private static async Task AssertLangaugeValuesInput(this IPage test, string label, Dictionary<string, string> values)
        {
            await test.AssertLanguageValues(label, values, async (h, v) =>
            {
                IElementHandle? handle = await h.QuerySelectorAsync("input");
                Assert.IsNotNull(handle);
                Assert.AreEqual(v, await handle.GetAttributeAsync("value"));
            });
        }

        private static async Task AssertLangaugeValuesTextarea(this IPage test, string label, Dictionary<string, string> values)
        {
            await test.AssertLanguageValues(label, values, async (h, v) =>
            {
                IElementHandle? handle = await h.QuerySelectorAsync("textarea");
                Assert.IsNotNull(handle);
                Assert.AreEqual(v, await handle.TextContentAsync());
            });
        }

        private static async Task AssertLangaugeValuesKeywords(this IPage test, string label, Dictionary<string, List<string>> values)
        {
            if (!values.ContainsKey("sk"))
            {
                values["sk"] = new List<string>();
            }
            await test.AssertLanguageValues(label, values, async (h, v) =>
            {
                List<string> items = new List<string>();
                foreach (IElementHandle element in await h.QuerySelectorAllAsync(".nkod-entity-detail-tag"))
                {
                    items.Add((await element.GetAttributeAsync("data-value"))!);
                }
                CollectionAssert.AreEqual(v, items);
            });
        }

        public static async Task AssertDatasetForm(this IPage test, DcatDataset rdf)
        {
            await test.WaitForLoadingDone();

            Assert.AreEqual(rdf.ShouldBePublic ? "publikovaný" : "nepublikovaný", await test.GetMultiRadioSelectedLabel("Stav datasetu"));

            await test.AssertLangaugeValuesInput("Názov datasetu", rdf.Title);
            await test.AssertLangaugeValuesTextarea("Popis", rdf.Description);
            CollectionAssert.AreEquivalent(rdf.NonEuroVocThemes.Select(e => e.ToString()).ToList(), await test.GetMultiSelectItems("Téma"));
            Assert.AreEqual(rdf.AccrualPeriodicity?.ToString(), await test.GetSelectItemFormElementGroup("Periodicita aktualizácie"));
            await test.AssertLangaugeValuesKeywords("Kľúčové slová", rdf.Keywords);
            CollectionAssert.AreEqual(rdf.Spatial.Select(e => e.ToString()).ToList(), await test.GetMultiSelectItems("Súvisiace geografické územie"));

            Assert.AreEqual(rdf.Temporal?.StartDate?.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty, await (await test.GetInputInFormElementGroup("Časové pokrytie, dátum od")).GetAttributeAsync("value"));
            Assert.AreEqual(rdf.Temporal?.EndDate?.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty, await (await test.GetInputInFormElementGroup("Časové pokrytie, dátum do")).GetAttributeAsync("value"));

            await test.AssertLangaugeValuesInput("Kontaktný bod, meno", rdf.ContactPoint?.Name ?? new Dictionary<string, string>());
            Assert.AreEqual(rdf.ContactPoint?.Email ?? string.Empty, await (await test.GetInputInFormElementGroup("Kontaktný bod, e-mailová adresa")).GetAttributeAsync("value"));

            Assert.AreEqual(rdf.LandingPage?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Domovská webová stránka")).GetAttributeAsync("value"));
            Assert.AreEqual(rdf.Specification?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Odkaz na špecifikáciu")).GetAttributeAsync("value"));
            Assert.AreEqual(rdf.Documentation?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Odkaz na dokumentáciu")).GetAttributeAsync("value"));
            Assert.AreEqual(rdf.Relation?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Súvisiaci zdroj")).GetAttributeAsync("value"));

            CollectionAssert.AreEqual(rdf.EuroVocThemes.Select(e => e.ToString()).ToList(), await test.GetMultiSelectItems("Klasifikácia podľa EuroVoc"));

            Assert.AreEqual(rdf.SpatialResolutionInMeters?.ToString("G29") ?? string.Empty, await (await test.GetInputInFormElementGroup("Priestorové rozlíšenie v metroch")).GetAttributeAsync("value"));
            Assert.AreEqual(rdf.TemporalResolution?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Časové rozlíšenie")).GetAttributeAsync("value"));

            CollectionAssert.AreEqual(rdf.ApplicableLegislations.Select(e => e.ToString()).ToList(), await test.GetMultiSelectItems("Právny predpis"));
            Assert.AreEqual(rdf.HvdCategory?.ToString() ?? string.Empty, await test.GetSelectItemFormElementGroup("Kategória HVD"));

            Assert.AreEqual(rdf.IsSerie, await test.GetByLabel("Dataset je séria").IsCheckedAsync());
           
            if (rdf.IsPartOfInternalId is not null)
            {
                IElementHandle? partOfGroup = await test.GetFormElementGroup("Nadradený dataset");
                Assert.IsTrue(await test.GetByLabel("Dataset patrí do série").IsCheckedAsync());
                Assert.IsNotNull(partOfGroup);
                Assert.AreEqual(rdf.IsPartOfInternalId.ToString(), await (await partOfGroup.QuerySelectorAsync("select option:checked"))!.GetAttributeAsync("value"));
            }
            else
            {
                IReadOnlyList<IElementHandle> labels = await test.GetTestElements("is-part-of-serie");
                if (labels.Count > 0)
                {
                    Assert.IsFalse(await test.GetByLabel("Dataset patrí do série").IsCheckedAsync());
                }
            }
        }

        private static async Task FillLangaugeValues<T>(this IPage test, string label, Dictionary<string, T> values, Func<IElementHandle, T, Task> fillAction)
        {
            IElementHandle? group = await test.QuerySelectorAsync($".govuk-form-group[data-label=\"{label}\"]");
            Assert.IsNotNull(group);

            foreach (IElementHandle t in await group.QuerySelectorAllAsync(".language-input"))
            {
                string? lang = (await t.GetAttributeAsync("data-lang"));
                Assert.IsNotNull(lang);
                if (!values.ContainsKey(lang))
                {
                    IElementHandle? button = await t.QuerySelectorAsync("button");
                    if (button is not null)
                    {
                        await button.ClickAsync();
                    }
                }
            }

            foreach ((string lang, T value) in values)
            {
                IElementHandle? handle = await group.QuerySelectorAsync($".language-input[data-lang=\"{lang}\"]");
                if (handle is null)
                {
                    IElementHandle? addLanguage = await group.QuerySelectorAsync(".add-language");
                    Assert.IsNotNull(addLanguage);

                    //IElementHandle? select = await addLanguage.QuerySelectorAsync("select");
                    //Assert.IsNotNull(select);
                    //await select.SelectOptionAsync(lang);

                    IElementHandle? button = await addLanguage.QuerySelectorAsync("button");
                    Assert.IsNotNull(button);
                    await button.ClickAsync();

                    handle = await group.QuerySelectorAsync($".language-input[data-lang=\"{lang}\"]");
                }

                Assert.IsNotNull(handle);
                await fillAction(handle, value);
            }
        }


        private static async Task FillLangaugeValuesInput(this IPage test, string label, Dictionary<string, string> values)
        {
            await test.FillLangaugeValues(label, values, async (h, v) =>
            {
                IElementHandle? handle = await h.QuerySelectorAsync("input");
                Assert.IsNotNull(handle);
                await handle.FillAsync(v);
            });
        }

        private static async Task FillLangaugeValuesTextarea(this IPage test, string label, Dictionary<string, string> values)
        {
            await test.FillLangaugeValues(label, values, async (h, v) =>
            {
                IElementHandle? handle = await h.QuerySelectorAsync("textarea");
                Assert.IsNotNull(handle);
                await handle.FillAsync(v);
            });
        }

        private static async Task FillLangaugeValuesKeywords(this IPage test, string label, Dictionary<string, List<string>> values)
        {
            await test.FillLangaugeValues(label, values, FillMultiInput);
        }

        public static async Task FillDatasetFields(this IPage test, DcatDataset rdf)
        {
            await test.CheckDatasetPublicityRadio(rdf.ShouldBePublic);

            await test.FillLangaugeValuesInput("Názov datasetu", rdf.Title);
            await test.FillLangaugeValuesTextarea("Popis", rdf.Description);

            await (await test.GetSelectInFormElementGroup("Periodicita aktualizácie"))!.SelectOptionAsync(rdf.AccrualPeriodicity?.ToString() ?? string.Empty);
            await FillMultiSelect((await test.GetFormElementGroup("Téma"))!, rdf.NonEuroVocThemes.Select(t => t.ToString()));
            await test.FillLangaugeValuesKeywords("Kľúčové slová", rdf.Keywords);
            await FillMultiSelect((await test.GetFormElementGroup("Súvisiace geografické územie"))!, rdf.Spatial.Select(t => t.ToString()));

            await (await test.GetInputInFormElementGroup("Časové pokrytie, dátum od")).FillAsync(rdf.Temporal?.StartDate?.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty);
            await (await test.GetInputInFormElementGroup("Časové pokrytie, dátum do")).FillAsync(rdf.Temporal?.EndDate?.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty);

            await test.FillLangaugeValuesInput("Kontaktný bod, meno", rdf.ContactPoint?.Name ?? new Dictionary<string, string>());
            await (await test.GetInputInFormElementGroup("Kontaktný bod, e-mailová adresa")).FillAsync(rdf.ContactPoint?.Email ?? string.Empty);

            await (await test.GetInputInFormElementGroup("Domovská webová stránka")).FillAsync(rdf.LandingPage?.ToString() ?? string.Empty);
            await (await test.GetInputInFormElementGroup("Odkaz na špecifikáciu")).FillAsync(rdf.Specification?.ToString() ?? string.Empty);
            await (await test.GetInputInFormElementGroup("Odkaz na dokumentáciu")).FillAsync(rdf.Documentation?.ToString() ?? string.Empty);
            await (await test.GetInputInFormElementGroup("Súvisiaci zdroj")).FillAsync(rdf.Relation?.ToString() ?? string.Empty);

            await FillMultiInput((await test.GetFormElementGroup("Klasifikácia podľa EuroVoc"))!, rdf.EuroVocThemes.Select(t => t.ToString()));

            await (await test.GetInputInFormElementGroup("Priestorové rozlíšenie v metroch")).FillAsync(rdf.SpatialResolutionInMeters?.ToString("G29") ?? string.Empty);
            await (await test.GetInputInFormElementGroup("Časové rozlíšenie")).FillAsync(rdf.TemporalResolution?.ToString() ?? string.Empty);

            await FillMultiInput((await test.GetFormElementGroup("Právny predpis"))!, rdf.ApplicableLegislations.Select(t => t.ToString()));
            await (await test.GetSelectInFormElementGroup("Kategória HVD"))!.SelectOptionAsync(rdf.HvdCategory?.ToString() ?? string.Empty);

            await test.GetByLabel("Dataset je séria").SetCheckedAsync(rdf.IsSerie);

            if (rdf.IsPartOfInternalId is not null)
            {
                await test.GetByLabel("Dataset patrí do série").SetCheckedAsync(true);
                await (await test.GetSelectInFormElementGroup("Nadradený dataset"))!.SelectOptionAsync(rdf.IsPartOfInternalId);
            }
            else
            {
                foreach (IElementHandle element in await test.GetTestElements("is-part-of-serie"))
                {
                    await element.SetCheckedAsync(false);
                }
            }
        }

        public static async Task AssertLocalCatalogForm(this IPage test, DcatCatalog rdf)
        {
            await test.WaitForLoadingDone();

            Assert.AreEqual(rdf.ShouldBePublic ? "publikovaný" : "nepublikovaný", await test.GetMultiRadioSelectedLabel("Stav lokálneho katalógu"));

            await test.AssertLangaugeValuesInput("Názov katalógu", rdf.Title);
            await test.AssertLangaugeValuesTextarea("Popis", rdf.Description);

            Assert.AreEqual(rdf.HomePage?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Domáca stránka katalógu")).GetAttributeAsync("value"));
            await test.FillLangaugeValuesInput("Kontaktný bod, meno", rdf.ContactPoint?.Name ?? new Dictionary<string, string>());
            Assert.AreEqual(rdf.ContactPoint?.Email ?? string.Empty, await (await test.GetInputInFormElementGroup("Kontaktný bod, e-mailová adresa")).GetAttributeAsync("value"));

            string? typeValue = TranslateLocalCatalogType(rdf.Type?.ToString());
            Assert.AreEqual(typeValue, await test.GetMultiRadioSelectedLabel("Typ katalógu"));
            Assert.AreEqual(rdf.EndpointUrl?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Prístupový bod katalógu")).GetAttributeAsync("value"));
        }

        public static async Task FillLocalCatalogFields(this IPage test, DcatCatalog rdf)
        {
            await test.CheckLocalCatalogPublicityRadio(rdf.ShouldBePublic);

            await test.FillLangaugeValuesInput("Názov katalógu", rdf.Title);
            await test.FillLangaugeValuesTextarea("Popis", rdf.Description);
            await (await test.GetInputInFormElementGroup("Domáca stránka katalógu")).FillAsync(rdf.HomePage?.ToString() ?? string.Empty);

            await test.FillLangaugeValuesInput("Kontaktný bod, meno", rdf.ContactPoint?.Name ?? new Dictionary<string, string>());
            await (await test.GetInputInFormElementGroup("Kontaktný bod, e-mailová adresa")).FillAsync(rdf.ContactPoint?.Email ?? string.Empty);

            await test.CheckLocalCatalogType(rdf.Type?.ToString());
            await (await test.GetInputInFormElementGroup("Prístupový bod katalógu")).FillAsync(rdf.EndpointUrl?.ToString() ?? string.Empty);
        }

        public static async Task CheckDatasetPublicityRadio(this IPage test, bool value)
        {
            IElementHandle? fieldset = await test.GetByFieldsetLegend("Stav datasetu");
            Assert.IsNotNull(fieldset);

            IElementHandle? input = await GetInputByLabel(fieldset, value ? "publikovaný" : "nepublikovaný");
            Assert.IsNotNull(input);

            await input.CheckAsync();
        }

        public static async Task CheckLocalCatalogPublicityRadio(this IPage test, bool value)
        {
            IElementHandle? fieldset = await test.GetByFieldsetLegend("Stav lokálneho katalógu");
            Assert.IsNotNull(fieldset);

            IElementHandle? input = await GetInputByLabel(fieldset, value ? "publikovaný" : "nepublikovaný");
            Assert.IsNotNull(input);

            await input.CheckAsync();
        }

        private static string? TranslateLocalCatalogType(string? value)
        {
            return value switch
            {
                DcatCatalog.LocalCatalogTypeCodelist + "/1" => "DCAT Dokumenty",
                DcatCatalog.LocalCatalogTypeCodelist + "/2" => "SPARQL",
                _ => null
            };
        }

        public static async Task CheckLocalCatalogType(this IPage test, string? value)
        {
            IElementHandle? fieldset = await test.GetByFieldsetLegend("Typ katalógu");
            Assert.IsNotNull(fieldset);

            string? typeValue = TranslateLocalCatalogType(value);

            if (typeValue is not null)
            {
                IElementHandle? input = await GetInputByLabel(fieldset, typeValue);
                Assert.IsNotNull(input);

                await input.CheckAsync();
            }
        }

        public static FileState? GetLastEntity(Storage storage, FileType type)
        {
            FileStorageQuery query = new FileStorageQuery
            {
                OnlyTypes = new List<FileType> { type },
                OrderDefinitions = new List<FileStorageOrderDefinition> { new FileStorageOrderDefinition { Property = FileStorageOrderProperty.Created, ReverseOrder = true } },
                MaxResults = 1
            };
            FileStorageResponse response = storage.GetFileStates(query, new AllAccessFilePolicy());
            return response.Files.Count > 0 ? response.Files[0] : null;
        }

        public static DcatDataset? GetLastDataset(Storage storage)
        {
            FileState? state = GetLastEntity(storage, FileType.DatasetRegistration);
            return state?.Content is not null ? DcatDataset.Parse(state.Content) : null;
        }

        public static void RemoveNullValues(Dictionary<string, string> values)
        {
            foreach (string key in values.Keys.ToArray())
            {
                if (string.IsNullOrEmpty(values[key]))
                {
                    values.Remove(key);
                }
            }
        }

        public static void RemoveNullOrEmptyValues(Dictionary<string, List<string>> values)
        {
            foreach (string key in values.Keys.ToArray())
            {
                List<string>? value = values[key];
                if (value is null || value.Count == 0)
                {
                    values.Remove(key);
                }
            }
        }

        public static void AssertAreEqualLanguage(Dictionary<string, string>? expected, Dictionary<string, string>? actual)
        {
            expected ??= new Dictionary<string, string>();
            actual ??= new Dictionary<string, string>();
            RemoveNullValues(expected);
            RemoveNullValues(actual);
            CollectionAssert.AreEquivalent(expected, actual);
        }

        public static void AssertAreEqualLanguage(Dictionary<string, List<string>>? expected, Dictionary<string, List<string>>? actual)
        {
            expected ??= new Dictionary<string, List<string>>();
            actual ??= new Dictionary<string, List<string>>();
            RemoveNullOrEmptyValues(expected);
            RemoveNullOrEmptyValues(actual);
            CollectionAssert.AreEquivalent(expected.Keys, actual.Keys);
            foreach (string key in expected.Keys)
            {
                CollectionAssert.AreEquivalent(expected[key], actual[key]);
            }
        }

        public static void AssertAreEqual(DcatDataset expected, bool isPublic, FileState state)
        {
            DcatDataset actual = DcatDataset.Parse(state.Content!)!;

            AssertAreEqualLanguage(expected.Title, actual.Title);
            AssertAreEqualLanguage(expected.Description, actual.Description);
            Assert.AreEqual(expected.AccrualPeriodicity, actual.AccrualPeriodicity);
            CollectionAssert.AreEquivalent(expected.Themes.ToList(), actual.Themes.ToList());
            AssertAreEqualLanguage(expected.Keywords, actual.Keywords);
            CollectionAssert.AreEquivalent(expected.Spatial.ToList(), actual.Spatial.ToList());
            Assert.AreEqual(expected.Temporal?.StartDate, actual.Temporal?.StartDate);
            Assert.AreEqual(expected.Temporal?.EndDate, actual.Temporal?.EndDate);
            AssertAreEqualLanguage(expected.ContactPoint?.Name, actual.ContactPoint?.Name);
            Assert.AreEqual(expected.ContactPoint?.Email, actual.ContactPoint?.Email);
            Assert.AreEqual(expected.LandingPage, actual.LandingPage);
            Assert.AreEqual(expected.Specification, actual.Specification);
            Assert.AreEqual(expected.Documentation, actual.Documentation);
            Assert.AreEqual(expected.Relation, actual.Relation);

            Assert.AreEqual(expected.SpatialResolutionInMeters, actual.SpatialResolutionInMeters);
            Assert.AreEqual(expected.TemporalResolution, actual.TemporalResolution);

            AssertAreEqualLanguage(expected.EuroVocThemeLabels, actual.EuroVocThemeLabels);

            CollectionAssert.AreEquivalent(expected.ApplicableLegislations.ToList(), actual.ApplicableLegislations.ToList());
            Assert.AreEqual(expected.HvdCategory, actual.HvdCategory);

            Assert.AreEqual(expected.IsSerie, actual.IsSerie);
            Assert.AreEqual(expected.IsPartOf, actual.IsPartOf);
            Assert.AreEqual(expected.IsPartOfInternalId, actual.IsPartOfInternalId);

            Assert.AreEqual(expected.ShouldBePublic, actual.ShouldBePublic);
            Assert.AreEqual(isPublic, state.Metadata.IsPublic);
            Assert.AreEqual(expected.Publisher!.ToString(), state.Metadata.Publisher);
        }

        public static void AssertAreEqual(DcatDistribution expected, FileState state)
        {
            DcatDistribution actual = DcatDistribution.Parse(state.Content!)!;

            Assert.AreEqual(expected.TermsOfUse?.AuthorsWorkType, actual.TermsOfUse?.AuthorsWorkType);
            Assert.AreEqual(expected.TermsOfUse?.OriginalDatabaseType, actual.TermsOfUse?.OriginalDatabaseType);
            Assert.AreEqual(expected.TermsOfUse?.DatabaseProtectedBySpecialRightsType, actual.TermsOfUse?.DatabaseProtectedBySpecialRightsType);
            Assert.AreEqual(expected.TermsOfUse?.PersonalDataContainmentType, actual.TermsOfUse?.PersonalDataContainmentType);

            Assert.AreEqual(expected.AccessUrl, actual.AccessUrl);
            Assert.AreEqual(expected.Format, actual.Format);
            Assert.AreEqual(expected.MediaType, actual.MediaType);
            Assert.AreEqual(expected.ConformsTo, actual.ConformsTo);
            Assert.AreEqual(expected.CompressFormat, actual.CompressFormat);
            Assert.AreEqual(expected.PackageFormat, actual.PackageFormat);

            CollectionAssert.AreEquivalent(expected.ApplicableLegislations.ToList(), actual.ApplicableLegislations.ToList());

            AssertAreEqualLanguage(expected.Title, actual.Title);
            
            Assert.AreEqual(expected.DownloadUrl, actual.DownloadUrl);

            DcatDataService? dataService = expected.DataService;
            if (dataService is not null)
            {
                DcatDataService? actualDataService = actual.DataService;
                Assert.IsNotNull(actualDataService);

                Assert.AreEqual(dataService.EndpointUrl, actualDataService.EndpointUrl);
                Assert.AreEqual(dataService.Documentation, actualDataService.Documentation);
                Assert.AreEqual(dataService.ConformsTo, actualDataService.ConformsTo);
                Assert.AreEqual(dataService.HvdCategory, actualDataService.HvdCategory);
                Assert.AreEqual(dataService.EndpointDescription, actualDataService.EndpointDescription);
                AssertAreEqualLanguage(dataService.ContactPoint?.Name, actualDataService.ContactPoint?.Name);
                Assert.AreEqual(dataService.ContactPoint?.Email, actualDataService.ContactPoint?.Email);
                CollectionAssert.AreEquivalent(dataService.ApplicableLegislations.ToList(), actualDataService.ApplicableLegislations.ToList());
            }
            else
            {
                Assert.IsNull(actual.DataService);
            }
        }

        public static void AssertAreEqual(DcatCatalog expected, FileState state)
        {
            DcatCatalog actual = DcatCatalog.Parse(state.Content!)!;

            AssertAreEqualLanguage(expected.Title, actual.Title);
            AssertAreEqualLanguage(expected.Description, actual.Description);
            Assert.AreEqual(expected.HomePage, actual.HomePage);
            AssertAreEqualLanguage(expected.ContactPoint?.Name, actual.ContactPoint?.Name);
            Assert.AreEqual(expected.ContactPoint?.Email, actual.ContactPoint?.Email);
            Assert.AreEqual(expected.Type?.ToString(), actual.Type?.ToString());
            Assert.AreEqual(expected.EndpointUrl, actual.EndpointUrl);

            Assert.AreEqual(expected.ShouldBePublic, actual.ShouldBePublic);
            Assert.AreEqual(expected.ShouldBePublic, state.Metadata.IsPublic);
            Assert.AreEqual(expected.Publisher!.ToString(), state.Metadata.Publisher);
        }

        public static async Task RunAndWaitForRequests(this IPage page, Func<Task> action, List<string> requiredRequests)
        {
            await page.RunAndWaitForResponseAsync(action, r =>
            {
                Uri uri = new Uri(r.Url);
                for (int i = 0; i < requiredRequests.Count; i++)
                {
                    if (uri.AbsolutePath.EndsWith(requiredRequests[i]))
                    {
                        requiredRequests.RemoveAt(i);
                        break;
                    }
                }
                return requiredRequests.Count == 0;
            });
        }

        public static async Task RunAndWaitForDatasetCreate(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "datasets/search",
                "codelists"
            });
            await page.WaitForURLAsync($"http://localhost:6001/sprava/datasety/pridat");
        }

        public static async Task RunAndWaitForDatasetEdit(this IPage page, Guid id, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "datasets/search",
                "datasets/search",
                "codelists"
            });
            await page.WaitForURLAsync($"http://localhost:6001/sprava/datasety/upravit/{id}");
        }

        public static async Task RunAndWaitForDatasetList(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "datasets/search",
            });

            await page.WaitForURLAsync($"http://localhost:6001/sprava/datasety");
            await page.WaitForLoadingDone();
        }

        public static async Task RunAndWaitForLocalCatalogCreate(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "codelists",
            });
            await page.WaitForURLAsync($"http://localhost:6001/sprava/lokalne-katalogy/pridat");
        }

        public static async Task RunAndWaitForLocalCatalogEdit(this IPage page, Guid id, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "local-catalogs/search",
                "codelists",
            });
            await page.WaitForURLAsync($"http://localhost:6001/sprava/lokalne-katalogy/upravit/{id}");
        }

        public static async Task RunAndWaitForLocalCatalogList(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "local-catalogs/search",
            });

            await page.WaitForURLAsync($"http://localhost:6001/sprava/lokalne-katalogy");
        }

        public static async Task RunAndWaitForUserList(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "users/search",
            });

            await page.WaitForURLAsync($"http://localhost:6001/sprava/pouzivatelia");
        }

        public static async Task RunAndWaitForDistributionCreate(this IPage page, Guid datasetId, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "codelists",
                "distributions/search",
            });            
            await page.WaitForURLAsync($"http://localhost:6001/sprava/distribucie/{datasetId}/pridat");
            await page.WaitForLoadingDone();
            await page.WaitForLoadingDone();
        }

        public static async Task RunAndWaitForDistributionEdit(this IPage page, Guid id, Guid datasetId, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "distributions/search",
                "codelists"
            });
            await page.WaitForURLAsync($"http://localhost:6001/sprava/distribucie/{datasetId}/upravit/{id}");
        }

        public static async Task RunAndWaitForDistributionList(this IPage page, Guid datasetId, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "distributions/search",
            });

            await page.WaitForURLAsync($"http://localhost:6001/sprava/distribucie/{datasetId}");
        }

        public static async Task RunAndWaitForPublisherList(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "publishers/search",
            });

            await page.WaitForURLAsync("http://localhost:6001/sprava/poskytovatelia");
        }

        public static async Task TakeScreenshot(this IPage page)
        {
            await Task.Delay(600);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = "screenshot.png", FullPage = true });
        }

        public static async Task AssertDistributionForm(this IPage test, DcatDistribution rdf)
        {
            await test.WaitForLoadingDone();

            Assert.AreEqual(rdf.TermsOfUse?.AuthorsWorkType?.ToString(), await test.GetSelectItemFormElementGroup("Typ autorského diela"));
            Assert.AreEqual(rdf.TermsOfUse?.OriginalDatabaseType?.ToString(), await test.GetSelectItemFormElementGroup("Typ originálnej databázy"));
            Assert.AreEqual(rdf.TermsOfUse?.DatabaseProtectedBySpecialRightsType?.ToString(), await test.GetSelectItemFormElementGroup("Typ špeciálnej právnej ochrany databázy"));
            Assert.AreEqual(rdf.TermsOfUse?.PersonalDataContainmentType?.ToString(), await test.GetSelectItemFormElementGroup("Typ výskytu osobných údajov"));

            Assert.AreEqual(rdf.TermsOfUse?.AuthorName?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Meno autora diela")).GetAttributeAsync("value"));
            Assert.AreEqual(rdf.TermsOfUse?.OriginalDatabaseAuthorName?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Meno autora originálnej databázy")).GetAttributeAsync("value"));

            Assert.AreEqual(rdf.Format?.ToString(), await test.GetSelectItemFormElementGroup("Formát súboru na stiahnutie"));
            Assert.AreEqual(rdf.MediaType?.ToString(), await test.GetSelectItemFormElementGroup("Typ média súboru na stiahnutie"));
            Assert.AreEqual(rdf.CompressFormat?.ToString() ?? string.Empty, await test.GetSelectItemFormElementGroup("Typ média kompresného formátu"));
            Assert.AreEqual(rdf.PackageFormat?.ToString() ?? string.Empty, await test.GetSelectItemFormElementGroup("Typ média balíčkovacieho formátu"));
            Assert.AreEqual(rdf.ConformsTo?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Odkaz na strojovo-čitateľnú schému súboru na stiahnutie")).GetAttributeAsync("value"));
            await test.AssertLangaugeValuesInput("Názov distribúcie", rdf.Title);

            DcatDataService? dataService = rdf.DataService;
            if (dataService is not null)
            {
                Assert.IsNull(await test.GetFormElementGroup("URL súboru na stiahnutie"));

                Assert.IsTrue(await test.GetByLabel("Súbor je prístupný cez dátovú službu").IsCheckedAsync());

                Assert.AreEqual(dataService.EndpointUrl?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Prístupový bod")).GetAttributeAsync("value"));
                Assert.AreEqual(dataService.Documentation?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Dokumentácia")).GetAttributeAsync("value"));
            }
            else
            {
                Assert.IsNull(await test.GetFormElementGroup("Prístupový bod"));
                Assert.IsNull(await test.GetFormElementGroup("Odkaz na dokumentáciu"));
                Assert.IsNull(await test.GetFormElementGroup("Odkaz na špecifikáciu"));
                Assert.IsNull(await test.GetFormElementGroup("Popis prístupového bodu"));

                Assert.AreEqual(rdf.DownloadUrl?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("URL súboru na stiahnutie")).GetAttributeAsync("value"));

                Assert.IsFalse(await test.GetByLabel("Súbor je prístupný cez dátovú službu").IsCheckedAsync());
            }
            CollectionAssert.AreEquivalent(rdf.ApplicableLegislations.Select(e => e.ToString()).ToList(), await test.GetMultiSelectItems("Právny predpis"));
        }

        public static async Task FillDistributionFields(this IPage test, DcatDistribution rdf)
        {
            await (await test.GetSelectInFormElementGroup("Typ autorského diela"))!.SelectOptionAsync(rdf.TermsOfUse?.AuthorsWorkType?.ToString() ?? string.Empty);
            await (await test.GetSelectInFormElementGroup("Typ originálnej databázy"))!.SelectOptionAsync(rdf.TermsOfUse?.OriginalDatabaseType?.ToString() ?? string.Empty);
            await (await test.GetSelectInFormElementGroup("Typ špeciálnej právnej ochrany databázy"))!.SelectOptionAsync(rdf.TermsOfUse?.DatabaseProtectedBySpecialRightsType?.ToString() ?? string.Empty);
            await (await test.GetSelectInFormElementGroup("Typ výskytu osobných údajov"))!.SelectOptionAsync(rdf.TermsOfUse?.PersonalDataContainmentType?.ToString() ?? string.Empty);

            await (await test.GetInputInFormElementGroup("Meno autora diela"))!.FillAsync(rdf.TermsOfUse?.AuthorName?.ToString() ?? string.Empty);
            await (await test.GetInputInFormElementGroup("Meno autora originálnej databázy"))!.FillAsync(rdf.TermsOfUse?.OriginalDatabaseAuthorName?.ToString() ?? string.Empty);

            await (await test.GetSelectInFormElementGroup("Formát súboru na stiahnutie"))!.SelectOptionAsync(rdf.Format?.ToString() ?? string.Empty);
            await (await test.GetSelectInFormElementGroup("Typ média súboru na stiahnutie"))!.SelectOptionAsync(rdf.MediaType?.ToString() ?? string.Empty);
            await (await test.GetSelectInFormElementGroup("Typ média kompresného formátu"))!.SelectOptionAsync(rdf.CompressFormat?.ToString() ?? string.Empty);
            await (await test.GetSelectInFormElementGroup("Typ média balíčkovacieho formátu"))!.SelectOptionAsync(rdf.PackageFormat?.ToString() ?? string.Empty);
            await (await test.GetInputInFormElementGroup("Odkaz na strojovo-čitateľnú schému súboru na stiahnutie"))!.FillAsync(rdf.ConformsTo?.ToString() ?? string.Empty);
            await test.FillLangaugeValuesInput("Názov distribúcie", rdf.Title);

            IElementHandle? fieldset = await test.GetByFieldsetLegend("Súbor distribúcie");
            Assert.IsNotNull(fieldset);

            DcatDataService? dataService = rdf.DataService;
            if (dataService is not null)
            {
                IElementHandle? input = await GetInputByLabel(fieldset, "Súbor je prístupný cez dátovú službu");
                Assert.IsNotNull(input);
                await input.CheckAsync();

                await (await test.GetInputInFormElementGroup("Prístupový bod"))!.FillAsync(dataService.EndpointUrl?.ToString() ?? string.Empty);
                await (await test.GetInputInFormElementGroup("Popis prístupového bodu"))!.FillAsync(dataService.EndpointDescription?.ToString() ?? string.Empty);
                await (await test.GetInputInFormElementGroup("Dokumentácia"))!.FillAsync(dataService.Documentation?.ToString() ?? string.Empty);
                await (await test.GetSelectInFormElementGroup("Kategória HVD"))!.SelectOptionAsync(dataService.HvdCategory?.ToString() ?? string.Empty);

                await test.FillLangaugeValuesInput("Kontaktný bod, meno", dataService.ContactPoint?.Name ?? new Dictionary<string, string>());
                await (await test.GetInputInFormElementGroup("Kontaktný bod, e-mailová adresa")).FillAsync(dataService.ContactPoint?.Email ?? string.Empty);
            }
            else
            {
                IElementHandle? input = await GetInputByLabel(fieldset, "Súbor je prístupný na adrese");
                Assert.IsNotNull(input);
                await input.CheckAsync();

                await (await test.GetInputInFormElementGroup("URL súboru na stiahnutie"))!.FillAsync(rdf.DownloadUrl?.ToString() ?? string.Empty);
            }

            await FillMultiInput((await test.GetFormElementGroup("Právny predpis"))!, rdf.ApplicableLegislations.Select(t => t.ToString()));
        }

        public static async Task UplaodDistributionFile(this IPage test)
        {
            IElementHandle? fieldset = await test.GetByFieldsetLegend("Súbor distribúcie");
            Assert.IsNotNull(fieldset);

            IElementHandle? input = await GetInputByLabel(fieldset, "Nahratie súboru do NKOD");
            Assert.IsNotNull(input);
            await input.CheckAsync();

            await test.UploadFile(new FilePayload { Name = "test.csv", Buffer = Encoding.UTF8.GetBytes("test") });
        }

        public static async Task UploadFile(this IPage test, FilePayload payload)
        {
            await test.SetInputFilesAsync("input[type=file]", payload);
        }

        public static async Task OpenHomePage(this IPage test)
        {
            await test.GotoAsync("http://localhost:6001/");
            await test.WaitForURLAsync($"http://localhost:6001/");
        }

        public static async Task OpenLocalCatalogSearch(this IPage test)
        {
            await test.OpenHomePage();

            await test.RunAndWaitForLocalCatalogSearch(async () =>
            {
                await test.OpenMenu("Lokálne katalógy");
            });

            Assert.AreEqual("Národný katalóg otvorených dát - Lokálne katalógy", await test.TitleAsync());
            Assert.AreEqual("Lokálne katalógy", await (await test.QuerySelectorAsync("h1"))!.TextContentAsync());
        }

        public static async Task RunAndWaitForLocalCatalogSearch(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "local-catalogs/search",
            });

            await page.WaitForURLAsync($"http://localhost:6001/lokalne-katalogy");
        }

        public static async Task<string> GetSearchResultsCount(this IPage page)
        {
            return (await (page.GetByTestId("sr-count")).TextContentAsync())!;
        }

        public static async Task<IReadOnlyList<IElementHandle>> GetTestElements(this IPage page, string testId)
        {
            return await page.QuerySelectorAllAsync($"[data-testid='{testId}']");
        }

        public static async Task<IElementHandle?> GetTestElement(this IPage page, string testId, bool throwWhenNotSingle = true)
        {
            IReadOnlyList<IElementHandle> elements = await page.GetTestElements(testId);
            if (throwWhenNotSingle && elements.Count > 1)
            {
                throw new Exception($"Unexpected count of testid = {testId}");
            }
            return elements.Count > 0 ? elements[0] : null;
        }

        public static async Task<string?> GetLabel(this IPage page, IElementHandle element)
        {
            string? id = await element.GetAttributeAsync("id");
            Assert.IsNotNull(id);
            IElementHandle? label = await page.QuerySelectorAsync($"label[for='{id}']");
            Assert.IsNotNull(label);
            return await label.TextContentAsync();
        }

        public static async Task CheckTestContent(this IPage page, string testId, string content, int minimalCount = 1)
        {
            IReadOnlyList<IElementHandle> elements = await page.GetTestElements(testId);

            Assert.IsTrue(elements.Count >= minimalCount);

            foreach (IElementHandle element in elements)
            {
                Assert.AreEqual(content, await element.TextContentAsync());
            }
        }

        public static async Task CheckTestContent(this IPage page, string testId, List<string> values)
        {
            IReadOnlyList<IElementHandle> elements = await page.GetTestElements(testId);

            Assert.IsTrue(elements.Count == 1);

            int i = 0;
            foreach (IElementHandle element in await elements.Single().QuerySelectorAllAsync("div"))
            {
                Assert.AreEqual(values[i], await element.TextContentAsync());
                i++;
            }
        }

        public static async Task CheckTestContentLink(this IPage page, string testId, string href)
        {
            IReadOnlyList<IElementHandle> elements = await page.GetTestElements(testId);

            Assert.IsTrue(elements.Count == 1);
            IElementHandle element = elements.Single();
            Assert.IsNotNull(element);
            IElementHandle? link = await element.QuerySelectorAsync("a");
            Assert.IsNotNull(link);
            Assert.AreEqual(href, await link.GetAttributeAsync("href"));
            Assert.AreEqual("Zobraziť", await link.TextContentAsync());
        }

        public static async Task OpenLocalCatalogDetail(this IPage page, Guid id)
        {
            string url = $"http://localhost:6001/lokalne-katalogy/{id}";
            await page.RunAndWaitForRequests(async () =>
            {
                await page.GotoAsync(url);
            }, new List<string> { "local-catalogs/search" });

            await page.WaitForURLAsync(url);
        }

        public static async Task RunAndWaitForPublisherSearch(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "publishers/search",
            });

            await page.WaitForURLAsync($"http://localhost:6001/poskytovatelia");
        }

        public static async Task OpenPublisherSearch(this IPage test)
        {
            await test.OpenHomePage();

            await test.RunAndWaitForPublisherSearch(async () =>
            {
                await test.OpenMenu("Poskytovatelia dát");
            });

            Assert.AreEqual("Národný katalóg otvorených dát - Poskytovatelia dát", await test.TitleAsync());
            Assert.AreEqual("Poskytovatelia dát", await (await test.QuerySelectorAsync("h1"))!.TextContentAsync());
        }

        public static async Task OpenDatasetSearch(this IPage test)
        {
            await test.OpenHomePage();

            await test.RunAndWaitForDatasetSearch(async () =>
            {
                await test.OpenMenu("Vyhľadávanie");
            });

            Assert.AreEqual("Národný katalóg otvorených dát - Vyhľadávanie", await test.TitleAsync());
            Assert.AreEqual("Vyhľadávanie", await (await test.QuerySelectorAsync("h1"))!.TextContentAsync());
        }

        public static async Task OpenDatasetDetail(this IPage page, Guid id)
        {
            string url = $"http://localhost:6001/datasety/{id}";
            await page.RunAndWaitForRequests(async () =>
            {
                await page.GotoAsync(url);
            }, new List<string> { "datasets/search", "datasets/search", "datasets/search" });

            await page.WaitForURLAsync(url);
        }

        public static async Task RunAndWaitForDatasetSearch(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "datasets/search",
            });

            await page.WaitForURLAsync($"http://localhost:6001/datasety");
        }

        public static async Task RunAndWaitForHomePage(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "publishers/search",
            });
            await page.WaitForURLAsync($"http://localhost:6001/");
        }

        private static string? GetRoleName(string? internalRoleName)
        {
            return internalRoleName switch
            {
                "Publisher" => "Zverejňovateľ dát",
                "PublisherAdmin" => "Administrátor poskytovateľa dát",
                null => "Žiadna rola",
                _ => null
            };
        }

        public static async Task AssertNewUserForm(this IPage test, NewUserInput input)
        {
            await test.WaitForLoadingDone();

            Assert.AreEqual(input.FirstName, await (await test.GetInputInFormElementGroup("Meno")).GetAttributeAsync("value"));
            Assert.AreEqual(input.LastName, await (await test.GetInputInFormElementGroup("Priezvisko")).GetAttributeAsync("value"));
            Assert.AreEqual(input.Email, await (await test.GetInputInFormElementGroup("E-mailová adresa")).GetAttributeAsync("value"));

            Assert.AreEqual(GetRoleName(input.Role), await test.GetMultiRadioSelectedLabel("Rola"));
        }

        private static async Task FillUserRole(this IPage test, string? roleName)
        {
            IElementHandle? fieldset = await test.GetByFieldsetLegend("Rola");
            Assert.IsNotNull(fieldset);

            IElementHandle? input = await GetInputByLabel(fieldset, GetRoleName(roleName)!);
            Assert.IsNotNull(input);

            await input.CheckAsync();
        }

        public static async Task FillUserFields(this IPage test, NewUserInput input)
        {
            await (await test.GetInputInFormElementGroup("Meno"))!.FillAsync(input.FirstName);
            await (await test.GetInputInFormElementGroup("Priezvisko"))!.FillAsync(input.LastName);
            await (await test.GetInputInFormElementGroup("E-mailová adresa"))!.FillAsync(input.Email ?? string.Empty);
            await test.FillUserRole(input.Role);
        }

        public static void AssertAreEqual(NewUserInput input, PersistentUserInfo userInfo)
        {
            Assert.AreEqual(input.FirstName, userInfo.FirstName);
            Assert.AreEqual(input.LastName, userInfo.LastName);
            Assert.AreEqual(input.Email, userInfo.Email);
            Assert.AreEqual(input.Role, userInfo.Role);
        }

        public static async Task RunAndWaitForUserCreate(this IPage page, Func<Task> action)
        {
            await action();
            await page.WaitForURLAsync($"http://localhost:6001/sprava/pouzivatelia/pridat");
        }

        public static async Task RunAndWaitForUserEdit(this IPage page, string id, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "users/search",
            });
            await page.WaitForURLAsync($"http://localhost:6001/sprava/pouzivatelia/upravit/{id}");
        }

        public static async Task AssertEditUserForm(this IPage test, NewUserInput input)
        {
            await test.WaitForLoadingDone();

            Assert.AreEqual(input.FirstName, await (await test.GetInputInFormElementGroup("Meno")).GetAttributeAsync("value"));
            Assert.AreEqual(input.LastName, await (await test.GetInputInFormElementGroup("Priezvisko")).GetAttributeAsync("value"));
            Assert.AreEqual(input.Email, await (await test.GetInputInFormElementGroup("E-mailová adresa")).GetAttributeAsync("value"));

            Assert.AreEqual(GetRoleName(input.Role), await test.GetMultiRadioSelectedLabel("Rola"));
        }

        public static async Task FillUserFields(this IPage test, EditUserInput input)
        {
            await (await test.GetInputInFormElementGroup("Meno"))!.FillAsync(input.FirstName);
            await (await test.GetInputInFormElementGroup("Priezvisko"))!.FillAsync(input.LastName);
            await (await test.GetInputInFormElementGroup("E-mailová adresa"))!.FillAsync(input.Email ?? string.Empty);
            await test.FillUserRole(input.Role);
        }

        public static void AssertAreEqual(EditUserInput input, PersistentUserInfo userInfo)
        {
            Assert.AreEqual(input.FirstName, userInfo.FirstName);
            Assert.AreEqual(input.LastName, userInfo.LastName);
            Assert.AreEqual(input.Email, userInfo.Email);
            Assert.AreEqual(input.Role, userInfo.Role);
        }

        public static async Task WaitForRefershToken(this IPage page)
        {
            List<string> requiredRequests = new List<string>
            {
                "user-info",
                "refresh"
            };

            await page.WaitForResponseAsync(r =>
            {
                Uri uri = new Uri(r.Url);
                for (int i = 0; i < requiredRequests.Count; i++)
                {
                    if (uri.AbsolutePath.EndsWith(requiredRequests[i]))
                    {
                        requiredRequests.RemoveAt(i);
                        break;
                    }
                }
                return requiredRequests.Count == 0;
            });
        }

        public static async Task AssertAdminPublisherForm(this IPage test, FoafAgent rdf, bool isEnabled)
        {
            await test.WaitForLoadingDone();

            Assert.AreEqual(isEnabled ? "publikovaný" : "nepublikovaný", await test.GetMultiRadioSelectedLabel("Stav"));
            await test.AssertLangaugeValuesInput("Názov poskytovateľa dát", rdf.Name);
            Assert.AreEqual(rdf.Uri.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("URI")).GetAttributeAsync("value"));
            await test.AssertPublisherForm(rdf);
        }

        public static async Task AssertPublisherForm(this IPage test, FoafAgent rdf)
        {
            await test.WaitForLoadingDone();
            
            Assert.AreEqual(rdf.HomePage?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Adresa webového sídla")).GetAttributeAsync("value"));
            Assert.AreEqual(rdf.EmailAddress?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("E-mailová adresa kontaktnej osoby")).GetAttributeAsync("value"));
            Assert.AreEqual(rdf.Phone?.ToString() ?? string.Empty, await (await test.GetInputInFormElementGroup("Telefónne číslo kontaktnej osoby")).GetAttributeAsync("value"));

            Assert.AreEqual(rdf.LegalForm?.ToString(), await test.GetSelectItemFormElementGroup("Právna forma"));
        }

        public static async Task FillAdminPublisherForm(this IPage test, FoafAgent rdf, bool isEnabled)
        {
            IElementHandle? fieldset = await test.GetByFieldsetLegend("Stav");
            Assert.IsNotNull(fieldset);

            IElementHandle? input = await GetInputByLabel(fieldset, isEnabled ? "publikovaný" : "nepublikovaný");
            Assert.IsNotNull(input);

            await input.CheckAsync();

            await test.FillLangaugeValuesInput("Názov", rdf.Name);
            await (await test.GetInputInFormElementGroup("URI")).FillAsync(rdf.Uri.ToString());

            await test.FillPublisherForm(rdf);
        }

        public static async Task FillPublisherForm(this IPage test, FoafAgent rdf)
        {
            await (await test.GetInputInFormElementGroup("Adresa webového sídla")).FillAsync(rdf.HomePage?.ToString() ?? string.Empty);
            await (await test.GetInputInFormElementGroup("E-mailová adresa kontaktnej osoby")).FillAsync(rdf.EmailAddress ?? string.Empty);
            await (await test.GetInputInFormElementGroup("Telefónne číslo kontaktnej osoby")).FillAsync(rdf.Phone ?? string.Empty);
            await (await test.GetSelectInFormElementGroup("Právna forma"))!.SelectOptionAsync(rdf.LegalForm?.ToString() ?? string.Empty);
        }

        public static void AssertAreEqual(FoafAgent expected, FileState state, bool isEnabled)
        {
            FoafAgent actual = FoafAgent.Parse(state.Content!)!;

            AssertAreEqualLanguage(expected.Name, actual.Name);
            Assert.AreEqual(expected.Uri, actual.Uri);
            Assert.AreEqual(expected.HomePage, actual.HomePage);
            Assert.AreEqual(expected.EmailAddress, actual.EmailAddress);
            Assert.AreEqual(expected.Phone, actual.Phone);
            Assert.AreEqual(expected.LegalForm, actual.LegalForm);
            Assert.AreEqual(isEnabled, state.Metadata.IsPublic);
        }


        public static async Task RunAndWaitForPublisherCreate(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "codelists"
            });
            await page.WaitForURLAsync($"http://localhost:6001/sprava/poskytovatelia/pridat");
        }

        public static async Task RunAndWaitForPublisherEdit(this IPage page, Guid id, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "publishers/search",
                "codelists"
            });
            await page.WaitForURLAsync($"http://localhost:6001/sprava/poskytovatelia/upravit/{id}");
        }

        public static async Task RunAndWaitForChangeLicenses(this IPage page, Func<Task> action)
        {
            await page.RunAndWaitForRequests(action, new List<string>
            {
                "codelists",
            });
            await page.WaitForURLAsync($"http://localhost:6001/sprava/zmena-licencii");
            await page.WaitForLoadingDone();
        }
    }
}
