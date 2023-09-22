using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NkodSk.Abstractions;
using TestBase;
using System.Web;
using Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WebApi.Test
{
    public class CodelistAdminTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        public CodelistAdminTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        private void CreateOldConcept()
        {
            fixture.CreateCodelistFile(DcatDataset.TypeCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "http://example.com/test/1", new LanguageDependedTexts{ { "sk", "Test1Sk" }, { "en", "Test1En" } } },
                { "http://example.com/test/2", new LanguageDependedTexts{ { "sk", "Test2Sk" }, { "en", "Test2En" } } },
            });
        }

        private byte[] CreateNewConcept()
        {
            SkosConceptScheme scheme = SkosConceptScheme.Create(new Uri(DcatDataset.TypeCodelist));
            SkosConcept concept = scheme.CreateConcept(new Uri("http://example.com/test/3"));
            concept.SetLabel(new LanguageDependedTexts { { "sk", "Test3Sk" }, { "en", "Test3En" } });
            concept = scheme.CreateConcept(new Uri("http://example.com/test/4"));
            concept.SetLabel(new LanguageDependedTexts { { "sk", "Test4Sk" }, { "en", "Test4En" } });

            string content = scheme.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            return bytes;
        }

        private async Task ValidateCodelistOld(HttpClient client)
        {
            await ValidateCodelist(client, new Dictionary<string, string>
            {
                { "http://example.com/test/1", "Test1Sk" },
                { "http://example.com/test/2", "Test2Sk" },
            });
        }

        private async Task ValidateCodelistNew(HttpClient client)
        {
            await ValidateCodelist(client, new Dictionary<string, string>
            {
                { "http://example.com/test/3", "Test3Sk" },
                { "http://example.com/test/4", "Test4Sk" },
            });
        }

        private async Task ValidateCodelist(HttpClient client, Dictionary<string, string> values)
        {
            using HttpResponseMessage response = await client.GetAsync($"/codelists?keys[]={HttpUtility.HtmlEncode(DcatDataset.TypeCodelist)}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<CodelistView>? codelistResponse = await response.Content.ReadFromJsonAsync<List<CodelistView>>();
            Assert.NotNull(codelistResponse);
            Assert.Single(codelistResponse);
            CodelistView codelist = codelistResponse[0];
            Assert.Equal(2, values.Count);
            int index = 0;
            foreach ((string key, string name) in values)
            {
                CodelistItemView actual = codelist.Values[index];
                Assert.Equal(key, actual.Id);
                Assert.Equal(name, actual.Label);
                index++;
            }
        }

        [Fact]
        public async Task CodelistPutIsNotAllowedForAnonymousUser()
        {
            string path = fixture.GetStoragePath();

            CreateOldConcept();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            byte[] bytes = CreateNewConcept();
            using MultipartFormDataContent requestContent = new MultipartFormDataContent
            {
                { new ByteArrayContent(bytes, 0, bytes.Length), "file", "codelist.ttl" }
            };

            using HttpResponseMessage response = await client.PutAsync("/codelists", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            await ValidateCodelistOld(client);
        }

        [Fact]
        public async Task CodelistPutIsNotAllowedForPulisher()
        {
            string path = fixture.GetStoragePath();

            CreateOldConcept();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            byte[] bytes = CreateNewConcept();
            using MultipartFormDataContent requestContent = new MultipartFormDataContent
            {
                { new ByteArrayContent(bytes, 0, bytes.Length), "file", "codelist.ttl" }
            };

            using HttpResponseMessage response = await client.PutAsync("/codelists", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            await ValidateCodelistOld(client);
        }

        [Fact]
        public async Task CodelistPutIsAllowedForSuperadmin()
        {
            string path = fixture.GetStoragePath();

            CreateOldConcept();

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("Superadmin"));

            byte[] bytes = CreateNewConcept();
            using MultipartFormDataContent requestContent = new MultipartFormDataContent
            {
                { new ByteArrayContent(bytes, 0, bytes.Length), "file", "codelist.ttl" }
            };

            using HttpResponseMessage response = await client.PutAsync("/codelists", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await ValidateCodelistNew(client);
        }
    }
}
