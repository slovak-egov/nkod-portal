using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TestBase;
using System.Data;
using static Lucene.Net.Documents.Field;
using System.Security.Policy;
using VDS.RDF;

namespace WebApi.Test
{
    public class DistributionModificationTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        private const string PublisherId = "http://example.com/publisher";

        private readonly IFileStorageAccessPolicy accessPolicy = new PublisherFileAccessPolicy(PublisherId);

        public DistributionModificationTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        private DistributionInput CreateInput(Guid datasetId, bool withOptionalProperties = false)
        {
            DistributionInput input = new DistributionInput
            {
                DatasetId = datasetId.ToString(),
                AuthorsWorkType = "https://data.gov.sk/def/ontology/law/authorsWorkType/1",
                OriginalDatabaseType = "https://data.gov.sk/def/ontology/law/originalDatabaseType/1",
                DatabaseProtectedBySpecialRightsType = "https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1",
                PersonalDataContainmentType = "https://data.gov.sk/def/ontology/law/personalDataContainmentType/1",
                AuthorName = "AuthorName",
                OriginalDatabaseAuthorName = "OriginalDatabaseAuthorName",
                DownloadUrl = "http://data.gov.sk/download",
                Format = "http://publications.europa.eu/resource/dataset/file-type/1",
                MediaType = "http://www.iana.org/assignments/media-types/text/csv",
            };

            if (withOptionalProperties)
            {
                input.ConformsTo = "http://data.gov.sk/specification";
                input.CompressFormat = "http://www.iana.org/assignments/media-types/application/zip";
                input.PackageFormat = "http://www.iana.org/assignments/media-types/application/zip";
                input.Title = new Dictionary<string, string>
                {
                    { "sk", "TitleSk" },
                    { "en", "TitleEn" },
                };
            }

            return input;
        }

        private DistributionInput CreateDataServiceInput(Guid datasetId, bool withOptionalProperties = false)
        {
            DistributionInput input = new DistributionInput
            {
                DatasetId = datasetId.ToString(),
                AuthorsWorkType = "https://data.gov.sk/def/ontology/law/authorsWorkType/1",
                OriginalDatabaseType = "https://data.gov.sk/def/ontology/law/originalDatabaseType/1",
                DatabaseProtectedBySpecialRightsType = "https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1",
                PersonalDataContainmentType = "https://data.gov.sk/def/ontology/law/personalDataContainmentType/1",
                AuthorName = "AuthorName",
                OriginalDatabaseAuthorName = "OriginalDatabaseAuthorName",
                Format = "http://publications.europa.eu/resource/dataset/file-type/1",
                MediaType = "http://www.iana.org/assignments/media-types/text/csv",
                EndpointUrl = "http://data.gov.sk/download",
                IsDataService = true,
                Title = new Dictionary<string, string>
                {
                    { "sk", "TitleSk" },
                    { "en", "TitleEn" },
                },
                Description = new Dictionary<string, string>
                {
                    { "sk", "DescriptionSk" },
                    { "en", "DescritpionEn" },
                },
                ApplicableLegislations = new List<string>
                {
                    "https://data.gov.sk/id/eli/sk/zz/2019/95",
                    "https://data.gov.sk/id/eli/sk/zz/2007/39/20220101",
                },
                Documentation = "http://data.gov.sk/specification",
            };

            return input;
        }

        private void ValidateValues(Storage storage, string? id, DistributionInput input, Guid datasetId, string publisher)
        {
            Assert.NotNull(id);
            FileState? state = storage.GetFileState(Guid.Parse(id), accessPolicy);
            Assert.NotNull(state);
            Assert.NotNull(state.Content);
            Assert.True(state.Metadata.IsPublic);
            Assert.Equal(publisher, state.Metadata.Publisher);
            Assert.Equal(FileType.DistributionRegistration, state.Metadata.Type);
            Assert.Equal(datasetId, state.Metadata.ParentFile);
            Assert.Equal(input.Format, state.Metadata.Name["sk"]);
            Assert.True((DateTimeOffset.Now - state.Metadata.Created).Duration().TotalMinutes < 1);
            Assert.True((DateTimeOffset.Now - state.Metadata.LastModified).Duration().TotalMinutes < 1);

            DcatDistribution? distribution = DcatDistribution.Parse(state.Content);
            Assert.NotNull(distribution);

            Assert.Equal(input.AuthorsWorkType, distribution.TermsOfUse?.AuthorsWorkType?.ToString());
            Assert.Equal(input.OriginalDatabaseType, distribution.TermsOfUse?.OriginalDatabaseType?.ToString());
            Assert.Equal(input.DatabaseProtectedBySpecialRightsType, distribution.TermsOfUse?.DatabaseProtectedBySpecialRightsType?.ToString());
            Assert.Equal(input.PersonalDataContainmentType, distribution.TermsOfUse?.PersonalDataContainmentType?.ToString());
            Assert.Equal(input.AuthorName, distribution.TermsOfUse?.AuthorName);
            Assert.Equal(input.OriginalDatabaseAuthorName, distribution.TermsOfUse?.OriginalDatabaseAuthorName);
            Assert.Equal(input.DownloadUrl, distribution.DownloadUrl?.ToString());
            Assert.Equal(input.Format, distribution.Format?.ToString());
            Assert.Equal(input.MediaType, distribution.MediaType?.ToString());
            Assert.Equal(input.ConformsTo, distribution.ConformsTo?.ToString());
            Assert.Equal(input.CompressFormat, distribution.CompressFormat?.ToString());
            Assert.Equal(input.PackageFormat, distribution.PackageFormat?.ToString());
            Extensions.AssertTextsEqual(input.Title, distribution.Title);

            DcatDataset dataset = DcatDataset.Parse(storage.GetFileState(datasetId, accessPolicy)!.Content!)!;

            List<string>? applicationLegilations = new List<string>(input.ApplicableLegislations ?? Enumerable.Empty<string>());
            if (input.ApplicableLegislations != null)
            {
                if (dataset.IsHvd && !applicationLegilations.Contains(DcatDataset.HvdLegislation))
                {
                    applicationLegilations.Add(DcatDataset.HvdLegislation);
                }
            }

            Assert.Equivalent(applicationLegilations, distribution.ApplicableLegislations.Select(v => v.ToString()));

            if (input.IsDataService)
            {
                Assert.Equal(input.EndpointUrl, distribution.DataService?.EndpointUrl?.ToString());
                Assert.Equivalent(applicationLegilations, distribution.DataService?.ApplicableLegislations.Select(v => v.ToString()));
                Assert.Equal(input.Documentation, distribution.DataService?.Documentation?.ToString());
                Assert.Equal(input.ConformsTo, distribution.DataService?.ConformsTo?.ToString());
                Assert.Equal(input.EndpointDescription, distribution.DataService?.EndpointDescription?.ToString());
                Assert.Equal(input.ConformsTo, distribution.DataService?.ConformsTo?.ToString());
                Assert.Equal(input.HvdCategory, distribution.DataService?.HvdCategory?.ToString());
            }
            else
            {
                Assert.Null(distribution.AccessService);
                Assert.Null(distribution.DataService);
            }

            Extensions.AssertTextsEqual(input.ContactName, distribution.DataService?.ContactPoint?.Name);
            Assert.Equal(input.ContactEmail, distribution.DataService?.ContactPoint?.Email);

            ValidateDatasetModifyChange(storage, datasetId);
        }

        private void ValidateDatasetModifyChange(Storage storage, Guid datasetId)
        {
            FileState? datasetState = storage.GetFileState(datasetId, accessPolicy);
            Assert.NotNull(datasetState);
            Assert.NotNull(datasetState.Content);
            Assert.True((DateTimeOffset.Now - datasetState.Metadata.LastModified).Duration().TotalMinutes < 1);

            DcatDataset? dataset = DcatDataset.Parse(datasetState.Content);
            Assert.NotNull(dataset);

            Assert.True((dataset.Modified!.Value - DateTimeOffset.Now).Duration().TotalMinutes < 1);
        }

        [Fact]
        public async Task TestCreateUnauthorized()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using JsonContent requestContent = JsonContent.Create(CreateInput(datasetId));
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestCreateMinimal()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);
        }

        [Fact]
        public async Task TestCreateMinimalWithWrongPersonalData()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            input.PersonalDataContainmentType = "https://data.gov.sk/def/ontology/law/personalDataContainmentType/3";
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.True(string.IsNullOrEmpty(result.Id));
            Assert.False(result.Success);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors["personaldatacontainmenttype"]);
        }

        [Fact]
        public async Task TestCreateMinimalInSerie()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();

            fixture.CreatePublisher("Test", PublisherId);
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);

            FileState parentState = storage.GetFileState(datasetId, accessPolicy)!;
            DcatDataset parentDataset = DcatDataset.Parse(parentState.Content!)!;
            parentDataset.IsSerie = true;
            storage.InsertFile(parentDataset.ToString(), parentState.Metadata, true, accessPolicy);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TestCreateExtended()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId, true);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);
        }

        private void ModifyDatasetAsHvd(Storage storage, Guid id, Guid publisherId)
        {
            FoafAgent publisher = FoafAgent.Parse(storage.GetFileState(publisherId, accessPolicy)!.Content!)!;

            FileState state = storage.GetFileState(id, accessPolicy)!;
            DcatDataset dataset = DcatDataset.Parse(state.Content!)!;
            dataset.Type = new List<Uri> { new Uri(DcatDataset.HvdType) };
            dataset.ApplicableLegislations = new List<Uri> { new Uri("http://www.example.com/eli/test") };
            dataset.HvdCategory = new Uri("http://publications.europa.eu/resource/dataset/high-value-dataset-category/1");
            storage.InsertFile(dataset.ToString(), dataset.UpdateMetadata(true, publisher, state.Metadata), true, accessPolicy);
        }

        [Fact]
        public async Task TestCreateMinimalInHvdDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);

            ModifyDatasetAsHvd(storage, datasetId, publisherId);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);
        }

        [Fact]
        public async Task TestModifyUnauthorized()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            DistributionInput input = CreateInput(datasetId);
            input.Id = distributions[0].ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestModify()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            input.Id = distributions[0].ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);
        }

        [Fact]
        public async Task TestModifyInHvdDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);

            ModifyDatasetAsHvd(storage, datasetId, publisherId);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            input.Id = distributions[0].ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);
        }

        [Fact]
        public async Task TestModifyWithWrongPersonalData()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            input.Id = distributions[0].ToString();
            input.PersonalDataContainmentType = "https://data.gov.sk/def/ontology/law/personalDataContainmentType/3";
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.True(string.IsNullOrEmpty(result.Id));
            Assert.False(result.Success);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors["personaldatacontainmenttype"]);
        }

        [Fact]
        public async Task TestModifyOtherPublisher()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId + "1");
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            input.Id = distributions[0].ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TestDeleteUnauthorized()
        {
            string path = fixture.GetStoragePath();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            using HttpResponseMessage response = await client.DeleteAsync($"/distributions?id={HttpUtility.UrlEncode(distributions[0].ToString())}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            Assert.NotNull(storage.GetFileMetadata(distributions[0], accessPolicy));
        }

        [Fact]
        public async Task TestDelete()
        {
            string path = fixture.GetStoragePath();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            using HttpResponseMessage response = await client.DeleteAsync($"/distributions?id={HttpUtility.UrlEncode(distributions[0].ToString())}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Null(storage.GetFileMetadata(distributions[0], accessPolicy));
            ValidateDatasetModifyChange(storage, datasetId);
        }

        [Fact]
        public async Task TestDeleteOtherPublisher()
        {
            string path = fixture.GetStoragePath();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId + "1");
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            using HttpResponseMessage response = await client.DeleteAsync($"/distributions?id={HttpUtility.UrlEncode(distributions[0].ToString())}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.NotNull(storage.GetFileMetadata(distributions[0], accessPolicy));
        }

        [Fact]
        public async Task LinkFileToDistributionOnCreate()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "test.txt", FileType.DistributionFile, null, PublisherId, true, "test.txt", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            storage.InsertFile("Content", metadata, false, accessPolicy);

            DistributionInput input = CreateInput(datasetId);
            input.FileId = metadata.Id.ToString();

            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);

            FileMetadata? modifiedMetadata = storage.GetFileMetadata(metadata.Id, accessPolicy);
            Assert.NotNull(modifiedMetadata);
            Assert.Equal(Guid.Parse(result.Id), modifiedMetadata.ParentFile);
        }

        [Fact]
        public async Task LinkFileToDistributionOnModify()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "test.txt", FileType.DistributionFile, null, PublisherId, true, "test.txt", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            storage.InsertFile("Content", metadata, false, accessPolicy);

            DistributionInput input = CreateInput(datasetId);
            input.Id = distributions[0].ToString();
            input.FileId = metadata.Id.ToString();

            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);

            FileMetadata? modifiedMetadata = storage.GetFileMetadata(metadata.Id, accessPolicy);
            Assert.NotNull(modifiedMetadata);
            Assert.Equal(Guid.Parse(result.Id), modifiedMetadata.ParentFile);
        }

        [Fact]
        public async Task TestCreateShouldPublishDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            foreach (Guid distributionId in distributions)
            {
                storage.DeleteFile(distributionId, accessPolicy);
            }

            FileState state = storage.GetFileState(datasetId, accessPolicy)!;
            DcatDataset dataset = DcatDataset.Parse(state.Content!)!;
            dataset.ShouldBePublic = true;
            storage.InsertFile(dataset.ToString(), state.Metadata with { IsPublic = false }, true, accessPolicy);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);

            state = storage.GetFileState(datasetId, accessPolicy)!;
            Assert.True(state.Metadata.IsPublic);
        }

        [Fact]
        public async Task TestCreateShouldNotPublishDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);
            foreach (Guid distributionId in distributions)
            {
                storage.DeleteFile(distributionId, accessPolicy);
            }

            FileState state = storage.GetFileState(datasetId, accessPolicy)!;
            DcatDataset dataset = DcatDataset.Parse(state.Content!)!;
            dataset.ShouldBePublic = false;
            storage.InsertFile(dataset.ToString(), state.Metadata with { IsPublic = false }, true, accessPolicy);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);

            state = storage.GetFileState(datasetId, accessPolicy)!;
            Assert.False(state.Metadata.IsPublic);
        }

        [Fact]
        public async Task TestCreateShouldAddFormatToDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();

            fixture.CreatePublisher("Ministerstvo hospodárstva SR", PublisherId);
            Guid datasetId = fixture.CreateDataset("Test", PublisherId);

            using Storage storage = new Storage(path);

            FileMetadata metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Empty(metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/1" }, metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());
        }

        [Fact]
        public async Task TestCreateShouldNotAddFormatToDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();

            (Guid datasetId, _, _) = fixture.CreateFullDataset(PublisherId);

            using Storage storage = new Storage(path);

            FileMetadata metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/1" }, metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/1" }, metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());
        }

        [Fact]
        public async Task TestCreateShouldAddSecondFormatToDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();

            (Guid datasetId, _, _) = fixture.CreateFullDataset(PublisherId);

            using Storage storage = new Storage(path);

            FileMetadata metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/1" }, metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            input.Format = "http://publications.europa.eu/resource/dataset/file-type/2";
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/1", "http://publications.europa.eu/resource/dataset/file-type/2" }, metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());
        }

        [Fact]
        public async Task TestEditShouldRetainFormatInDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();

            (Guid datasetId, _, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);

            using Storage storage = new Storage(path);

            FileMetadata metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/1" }, metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            input.Id = distributions[0].ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/1" }, metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());
        }

        [Fact]
        public async Task TestEditShouldChangeFormatInDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, _, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);

            using Storage storage = new Storage(path);

            FileMetadata metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/1" }, metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            input.Id = distributions[0].ToString();
            input.Format = "http://publications.europa.eu/resource/dataset/file-type/2";
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/2" }, metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());
        }

        [Fact]
        public async Task TestDeleteShouldRemoveFormatInDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, _, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);

            using Storage storage = new Storage(path);

            FileMetadata metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(new[] { "http://publications.europa.eu/resource/dataset/file-type/1" }, metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            using HttpResponseMessage response = await client.DeleteAsync($"/distributions?id={HttpUtility.UrlEncode(distributions[0].ToString())}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            metadata = storage.GetFileMetadata(datasetId, accessPolicy)!;
            Assert.Equal(Array.Empty<string>(), metadata.AdditionalValues?.GetValueOrDefault(DcatDistribution.FormatCodelist, Array.Empty<string>()) ?? Array.Empty<string>());
        }

        [Fact]
        public async Task TestCreateShouldAddDistrubutionToDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();

            (Guid datasetId, _, _) = fixture.CreateFullDataset(PublisherId);

            using Storage storage = new Storage(path);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            FileState? distributionState = storage.GetFileState(Guid.Parse(result.Id), accessPolicy);
            FileState? datasetState = storage.GetFileState(datasetId, accessPolicy);

            Assert.NotNull(distributionState);
            Assert.NotNull(datasetState);

            DcatDataset dataset = DcatDataset.Parse(datasetState.Content!)!;
            DcatDistribution distribution = DcatDistribution.Parse(distributionState.Content!)!;

            HashSet<Triple> datasetTriples = new HashSet<Triple>(dataset.Graph.Triples);
            HashSet<Triple> distributionTriples = new HashSet<Triple>(distribution.Graph.Triples);
            Assert.Contains(new Triple(dataset.Node, dataset.Graph.CreateUriNode("dcat:distribution"), distribution.Node), datasetTriples);
            List<Triple> notFound = distributionTriples.Where(t => !datasetTriples.Contains(t)).ToList();
            Assert.Empty(notFound);
        }

        [Fact]
        public async Task TestModifyShouldModifyDistrubutionToDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();

            (Guid datasetId, _, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);

            using Storage storage = new Storage(path);

            FileState distributionStateBefore = storage.GetFileState(distributions[0], accessPolicy)!;
            DcatDistribution distributionBefore = DcatDistribution.Parse(distributionStateBefore.Content!)!;

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            input.Id = distributions[0].ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            FileState? distributionState = storage.GetFileState(Guid.Parse(result.Id), accessPolicy);
            FileState? datasetState = storage.GetFileState(datasetId, accessPolicy);

            Assert.NotNull(distributionState);
            Assert.NotNull(datasetState);

            DcatDataset dataset = DcatDataset.Parse(datasetState.Content!)!;
            DcatDistribution distribution = DcatDistribution.Parse(distributionState.Content!)!;

            HashSet<Triple> datasetTriples = new HashSet<Triple>(dataset.Graph.Triples);
            HashSet<Triple> distributionTriples = new HashSet<Triple>(distribution.Graph.Triples);
            Assert.Contains(new Triple(dataset.Node, dataset.Graph.CreateUriNode("dcat:distribution"), distribution.Node), datasetTriples);
            List<Triple> notFound = distributionTriples.Where(t => !datasetTriples.Contains(t)).ToList();
            Assert.Empty(notFound);

            HashSet<Triple> distributionBeforeTriples = new HashSet<Triple>(distributionBefore.Graph.Triples);
            distributionBeforeTriples.ExceptWith(distribution.Graph.Triples);
            Assert.True(distributionBeforeTriples.All(t => !datasetTriples.Contains(t)));
        }

        [Fact]
        public async Task TestDeleteShouldRemoveDistributionFromDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, _, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);

            using Storage storage = new Storage(path);

            FileState distributionState = storage.GetFileState(distributions[0], accessPolicy)!;
            DcatDistribution distribution = DcatDistribution.Parse(distributionState.Content!)!;

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            using HttpResponseMessage response = await client.DeleteAsync($"/distributions?id={HttpUtility.UrlEncode(distributions[0].ToString())}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FileState? datasetState = storage.GetFileState(datasetId, accessPolicy);
            Assert.NotNull(datasetState);
            DcatDataset dataset = DcatDataset.Parse(datasetState.Content!)!;

            HashSet<Triple> datasetTriples = new HashSet<Triple>(dataset.Graph.Triples);
            Assert.DoesNotContain(new Triple(dataset.Node, dataset.Graph.CreateUriNode("dcat:distribution"), distribution.Node), datasetTriples);
            Assert.True(distribution.Graph.Triples.All(t => !datasetTriples.Contains(t)));
        }

        [Fact]
        public async Task DistributionShouldNotBeAddedToHarvestedDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);

            DcatDataset dataset = DcatDataset.Parse(storage.GetFileState(datasetId, accessPolicy)!.Content!)!;
            dataset.IsHarvested = true;
            storage.InsertFile(dataset.ToString(), dataset.UpdateMetadata(true, null, storage.GetFileState(datasetId, accessPolicy)!.Metadata), true, accessPolicy);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task HarvestedDistributionShouldNotBeEdited()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);

            DcatDistribution distribution = DcatDistribution.Parse(storage.GetFileState(distributions[0], accessPolicy)!.Content!)!;
            distribution.IsHarvested = true;
            storage.InsertFile(distribution.ToString(), distribution.UpdateMetadata(datasetId, PublisherId, storage.GetFileState(distributions[0], accessPolicy)!.Metadata), true, accessPolicy);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateInput(datasetId);
            input.Id = distributions[0].ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task HarvestedDistributionShouldNotBeDeleted()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);

            DcatDistribution distribution = DcatDistribution.Parse(storage.GetFileState(distributions[0], accessPolicy)!.Content!)!;
            distribution.IsHarvested = true;
            storage.InsertFile(distribution.ToString(), distribution.UpdateMetadata(datasetId, PublisherId, storage.GetFileState(distributions[0], accessPolicy)!.Metadata), true, accessPolicy);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            using HttpResponseMessage response = await client.DeleteAsync($"/distributions?id={HttpUtility.UrlEncode(distributions[0].ToString())}");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DataServiceShouldBeAdded()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateDataServiceInput(datasetId);
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);
        }

        [Fact]
        public async Task DataServiceShouldBeModified()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);

            using Storage storage = new Storage(path);
            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateDataServiceInput(datasetId);
            input.Id = distributions[0].ToString();
            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);
        }

        [Fact]
        public async Task DataServiceShouldBeAddedInHvdDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);

            ModifyDatasetAsHvd(storage, datasetId, publisherId);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateDataServiceInput(datasetId);
            input.ContactName = new Dictionary<string, string> { { "sk", "Service ContactName" } };
            input.ContactEmail = "contact@example.com";

            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PostAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);
        }

        [Fact]
        public async Task DataServiceShouldBeModifiedInHvdDataset()
        {
            string path = fixture.GetStoragePath();
            fixture.CreateDatasetCodelists();
            fixture.CreateDistributionCodelists();
            (Guid datasetId, Guid publisherId, Guid[] distributions) = fixture.CreateFullDataset(PublisherId);
            using Storage storage = new Storage(path);

            ModifyDatasetAsHvd(storage, datasetId, publisherId);

            using WebApiApplicationFactory applicationFactory = new WebApiApplicationFactory(storage);
            using HttpClient client = applicationFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, applicationFactory.CreateToken("PublisherAdmin", PublisherId));
            DistributionInput input = CreateDataServiceInput(datasetId);
            input.ContactName = new Dictionary<string, string> { { "sk", "Service ContactName" } };
            input.ContactEmail = "contact@example.com";
            input.Id = distributions[0].ToString();

            using JsonContent requestContent = JsonContent.Create(input);
            using HttpResponseMessage response = await client.PutAsync("/distributions", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            SaveResult? result = JsonConvert.DeserializeObject<SaveResult>(content);
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Id));
            Assert.True(result.Success);
            Assert.True(result.Errors is null || result.Errors.Count == 0);
            ValidateValues(storage, result.Id, input, datasetId, PublisherId);
        }
    }
}
