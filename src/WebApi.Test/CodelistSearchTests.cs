using Newtonsoft.Json;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebApi.Test
{
    public class CodelistSearchTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;


        public CodelistSearchTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task TestEmptySearch()
        {
            string path = fixture.GetStoragePath();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync("/codelists");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();

            List<CodelistView>? result = JsonConvert.DeserializeObject<List<CodelistView>>(content);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task TestOneCodelist()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateCodelistFile(DcatDataset.SpatialCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "https://data.gov.sk/id/nuts1/SK0", new LanguageDependedTexts { { "sk", "Slovensko" }, { "en", "Slovakia" } }  },
                { "https://data.gov.sk/id/nuts1/SK01", new LanguageDependedTexts { { "sk", "Nitra" }, { "en", "Nitra" } } }
            });

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync("/codelists?keys[]=" + HttpUtility.UrlEncode(DcatDataset.SpatialCodelist));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();

            List<CodelistView>? result = JsonConvert.DeserializeObject<List<CodelistView>>(content);
            Assert.NotNull(result);
            Assert.Single(result);

            CodelistView codelist = result[0];
            Assert.Equal(DcatDataset.SpatialCodelist, codelist.Id);
            Assert.Equal(DcatDataset.SpatialCodelist, codelist.Label);
            Assert.NotNull(codelist.Values);
            Assert.Equal(2, codelist.Values.Count);
            Assert.Equal("https://data.gov.sk/id/nuts1/SK01", codelist.Values[0].Id);
            Assert.Equal("https://data.gov.sk/id/nuts1/SK0", codelist.Values[1].Id);
        }

        [Fact]
        public async Task TestOneCodelistWithPriority()
        {
            string path = fixture.GetStoragePath();

            fixture.CreateCodelistFile(DcatDataset.SpatialCodelist, new Dictionary<string, (LanguageDependedTexts, decimal)>
            {
                { "https://data.gov.sk/id/nuts1/SK0", (new LanguageDependedTexts { { "sk", "Slovensko" }, { "en", "Slovakia" } }, 1) },
                { "https://data.gov.sk/id/nuts1/SK01", (new LanguageDependedTexts { { "sk", "Nitra" }, { "en", "Nitra" } }, 0) }
            });

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync("/codelists?keys[]=" + HttpUtility.UrlEncode(DcatDataset.SpatialCodelist));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();

            List<CodelistView>? result = JsonConvert.DeserializeObject<List<CodelistView>>(content);
            Assert.NotNull(result);
            Assert.Single(result);

            CodelistView codelist = result[0];
            Assert.Equal(DcatDataset.SpatialCodelist, codelist.Id);
            Assert.Equal(DcatDataset.SpatialCodelist, codelist.Label);
            Assert.NotNull(codelist.Values);
            Assert.Equal(2, codelist.Values.Count);
            Assert.Equal("https://data.gov.sk/id/nuts1/SK0", codelist.Values[0].Id);
            Assert.Equal("https://data.gov.sk/id/nuts1/SK01", codelist.Values[1].Id);
        }
    }
}
