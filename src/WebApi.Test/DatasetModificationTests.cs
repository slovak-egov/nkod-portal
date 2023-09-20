using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using NkodSk.Abstractions;
using TestBase;
using System.Web;

namespace WebApi.Test
{
    public class DatasetModificationTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherAccessPolicy(PublisherId);

        public DatasetModificationTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        private DatasetInput CreateInput(bool withOptionalProperties = false)
        {
            DatasetInput input = new DatasetInput
            {
                Name = new Dictionary<string, string>
                {
                    { "sk", "TestName" }
                },
                Description = new Dictionary<string, string>
                {
                    { "sk", "TestDescription" }
                },
                IsPublic = true,
                AccrualPeriodicity = "http://publications.europa.eu/resource/dataset/frequency/1",
                Keywords = new Dictionary<string, IEnumerable<string>>
                {
                    { "sk", new[]{ "TestKeyword1", "TestKeyword2" } }
                },
                Themes = new List<string> { "http://publications.europa.eu/resource/dataset/data-theme/1" }
            };

            if (withOptionalProperties)
            {
                input.Name["en"] = "TestNameEn";
                input.Description["en"] = "TestNameEn";
                input.Keywords["en"] = new[] { "TestKeyword1En" };
                                
                input.Type = new List<string> { "https://data.gov.sk/set/codelist/dataset-type/1" };
                input.Spatial = new List<string> { "http://publications.europa.eu/resource/dataset/country/1" };
                input.StartDate = "2.8.2023";
                input.EndDate = "14.8.2023";
                input.ContactName = new Dictionary<string, string>
                {
                    { "sk", "TestContentName" },
                    { "en", "TestContentNameEn" },
                };
                input.ContactEmail = "contact@example.com";
            }

            return input;
        }

        private void ValidateValues(Storage storage, string? id, DatasetInput input, string publisher, bool shouldBePublic)
        {
            Assert.NotNull(id);
            FileState? state = storage.GetFileState(Guid.Parse(id), accessPolicy);
            Assert.NotNull(state);
            Assert.NotNull(state.Content);
            Assert.Equal(shouldBePublic && input.IsPublic, state.Metadata.IsPublic);
            Assert.Equal(publisher, state.Metadata.Publisher);
            Assert.Equal(FileType.DatasetRegistration, state.Metadata.Type);
            Assert.Null(state.Metadata.ParentFile);
            Assert.Equal("TestName", state.Metadata.Name["sk"]);
            Assert.True((DateTimeOffset.Now - state.Metadata.Created).Duration().TotalMinutes < 1);
            Assert.True((DateTimeOffset.Now - state.Metadata.LastModified).Duration().TotalMinutes < 1);

            DcatDataset? dataset = DcatDataset.Parse(state.Content);
            Assert.NotNull(dataset);

            Extensions.AssertTextsEqual(input.Name, dataset.Title);
            Extensions.AssertTextsEqual(input.Description, dataset.Description);
            Assert.Equal(input.AccrualPeriodicity, dataset.AccrualPeriodicity?.ToString());
            Extensions.AssertTextsEqual(input.Keywords, dataset.Keywords);
            Assert.Equal(input.IsPublic, dataset.ShouldBePublic);
            Assert.Equivalent((input.Themes ?? new List<string>()).Union(input.EuroVocThemes ?? new List<string>()) , dataset.Themes.Select(v => v.ToString()));
            Assert.Equal(input.Type ?? new List<string>(), dataset.Type.Select(u => u.ToString()));
            Assert.Equivalent(input.Spatial ?? new List<string>(), dataset.Spatial.Select(v => v.ToString()));
            Extensions.AssertDateEqual(input.StartDate, dataset.Temporal?.StartDate);
            Extensions.AssertDateEqual(input.EndDate, dataset.Temporal?.EndDate);
            Extensions.AssertTextsEqual(input.ContactName, dataset.ContactPoint?.Name);
            Assert.Equal(input.ContactEmail, dataset.ContactPoint?.Email);
            Assert.Equal(input.Documentation, dataset.Documentation?.ToString());
            Assert.Equal(input.Specification, dataset.Specification?.ToString());
            Assert.Equal(input.SpatialResolutionInMeters is not null ? decimal.Parse(input.SpatialResolutionInMeters, System.Globalization.CultureInfo.CurrentCulture) : null, dataset.SpatialResolutionInMeters);
            Assert.Equal(input.TemporalResolution, dataset.TemporalResolution);
            Assert.Equal(input.IsPartOf, dataset.IsPartOf?.ToString());
        }

        [Fact]
        public async Task TestCreateUnauthorized()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using JsonContent requestContent = JsonContent.Create(CreateInput());
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestCreateMinimal()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, PublisherId, false);
        }

        [Fact]
        public async Task TestCreateMinimalNonPublic()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.IsPublic = false;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, PublisherId, false);
        }

        [Fact]
        public async Task TestCreateExtended()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput(true);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, PublisherId, false);
        }

        [Fact]
        public async Task TestModifyUnauthorized()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            DatasetInput input = CreateInput();
            input.Id = datasetId.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestModify()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            foreach (Guid distributionId in distributions)
            {
                storage.DeleteFile(distributionId, accessPolicy);
            }

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput(true);
            input.Id = datasetId.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, PublisherId, false);
        }

        [Fact]
        public async Task TestModifyNonPublic()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            foreach (Guid distributionId in distributions)
            {
                storage.DeleteFile(distributionId, accessPolicy);
            }

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput(true);
            input.IsPublic = false;
            input.Id = datasetId.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, PublisherId, false);
        }

        [Fact]
        public async Task TestModifyOtherPublisher()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId + "1");
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.Id = datasetId.ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TestDeleteUnauthorized()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.DeleteAsync($"/datasets?id={HttpUtility.UrlEncode(datasetId.ToString())}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            Assert.NotNull(storage.GetFileMetadata(datasetId, accessPolicy));
            foreach (Guid distributionId in distributions)
            {
                Assert.NotNull(storage.GetFileMetadata(distributionId, accessPolicy));
            }
        }

        [Fact]
        public async Task TestDelete()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            using HttpResponseMessage response = await client.DeleteAsync($"/datasets?id={HttpUtility.UrlEncode(datasetId.ToString())}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Null(storage.GetFileMetadata(datasetId, accessPolicy));
            foreach (Guid distributionId in distributions)
            {
                Assert.Null(storage.GetFileMetadata(distributionId, accessPolicy));
            }
        }

        [Fact]
        public async Task TestDeleteOtherPublisher()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId + "1");
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            using HttpResponseMessage response = await client.DeleteAsync($"/datasets?id={HttpUtility.UrlEncode(datasetId.ToString())}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.NotNull(storage.GetFileMetadata(datasetId, accessPolicy));
            foreach (Guid distributionId in distributions)
            {
                Assert.NotNull(storage.GetFileMetadata(distributionId, accessPolicy));
            }
        }

        [Fact]
        public async Task NameShouldBeRequiredOnCreate()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.Name!["sk"] = string.Empty;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DescriptionShouldBeRequiredOnCreate()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.Description!["sk"] = string.Empty;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ThemeShouldBeRequiredOnCreate()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.Themes!.Clear();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AccrualPeriodicityShouldBeRequiredOnCreate()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.AccrualPeriodicity = string.Empty;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task KeywordsShouldBeRequiredOnCreate()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.Keywords!["sk"] = new[] { "" };
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task NameShouldBeRequiredOnModify()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.Id = datasetId.ToString();
            input.Name!["sk"] = string.Empty;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DescriptionShouldBeRequiredOnModify()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.Id = datasetId.ToString();
            input.Description!["sk"] = string.Empty;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ThemeShouldBeRequiredOnModify()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.Id = datasetId.ToString();
            input.Themes!.Clear();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AccrualPeriodicityShouldBeRequiredOnModify()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.Id = datasetId.ToString();
            input.AccrualPeriodicity = string.Empty;
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task KeywordsShouldBeRequiredOnModify()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("User", PublisherId));
            DatasetInput input = CreateInput();
            input.Id = datasetId.ToString();
            input.Keywords!["sk"] = new[] { "" };
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/datasets", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
