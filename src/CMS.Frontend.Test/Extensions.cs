using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Frontend.Test;

namespace CMS.Frontend.Test
{
    public static class Extensions
    {
        internal static async Task Login(this IPage page, CompositeWebApplicationFactory factory, string role, string? publisherId = null)
        {
            using HttpClient client = factory.CreateDefaultClient();
            string token = factory.CreateUserAndToken(role, publisherId);

            await page.GotoAsync("http://localhost:6001/");

            await page.RunAndWaitForRequests(async () =>
            {
                await page.EvaluateAsync($"document.write('<form action=\"/saml/consume\" method=\"post\" id=\"token-form\"><input type=\"hidden\" name=\"token\" value=\"{token}\"></form>');document.getElementById('token-form').submit();");
                await page.WaitForURLAsync("http://localhost:6001/saml/consume");

                await page.WaitForURLAsync("http://localhost:6001/");
            }, new List<string> { "user-info" });
        }

        public static async Task OpenSuggestions(this IPage page)
        {
            await page.RunAndWaitForRequests(async () =>
            {
                await page.OpenMenu("Podnety");
            }, new List<string> { "/cms/suggestions/search" });
        }

        public static async Task OpenApplications(this IPage page)
        {
            await page.RunAndWaitForRequests(async () =>
            {
                await page.OpenMenu("Aplikácie");
            }, new List<string> { "/cms/applications/search" });
        }
    }
}
