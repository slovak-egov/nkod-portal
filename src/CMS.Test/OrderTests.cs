using CMS.Datasets;
using CMS.Likes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Piranha;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Test
{
    public class OrderTests
    {
        [Fact]
        public async Task DatasetsShouldBeOrderedByLikes()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            using IApi api = f.CreateApi();
            DatasetPost d1 = await api.CreateDataset();
            DatasetPost d2 = await api.CreateDataset(likeUsers: new[] { Guid.NewGuid(), Guid.NewGuid() });
            DatasetPost d3 = await api.CreateDataset(likeUsers: new[] { Guid.NewGuid() });

            using HttpResponseMessage response1 = await client.GetAsync($"/cms/datasets/order?property=likes");
            Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);
            List<string>? uris = await response1.Content.ReadFromJsonAsync<List<string>>();
            Assert.NotNull(uris);
            Assert.Equal(new[] { d1.Title, d3.Title, d2.Title }, uris);

            using HttpResponseMessage response2 = await client.GetAsync($"/cms/datasets/order?property=likes&reverse=true");
            Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);
            uris = await response2.Content.ReadFromJsonAsync<List<string>>();
            Assert.NotNull(uris);
            Assert.Equal(new[] { d2.Title, d3.Title, d1.Title }, uris);
        }

        [Fact]
        public async Task DatasetsShouldBeOrderedByComments()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            using IApi api = f.CreateApi();
            DatasetPost d1 = await api.CreateDataset();
            DatasetPost d2 = await api.CreateDataset();
            DatasetPost d3 = await api.CreateDataset();

            await api.CreateComment(contentId: d2.Id);
            await api.CreateComment(contentId: d2.Id);
            await api.CreateComment(contentId: d3.Id);


            using HttpResponseMessage response1 = await client.GetAsync($"/cms/datasets/order?property=comments");
            Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);
            List<string>? uris = await response1.Content.ReadFromJsonAsync<List<string>>();
            Assert.NotNull(uris);
            Assert.Equal(new[] { d3.Title, d2.Title }, uris);

            using HttpResponseMessage response2 = await client.GetAsync($"/cms/datasets/order?property=comments&reverse=true");
            Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);
            uris = await response2.Content.ReadFromJsonAsync<List<string>>();
            Assert.NotNull(uris);
            Assert.Equal(new[] { d2.Title, d3.Title }, uris);
        }

        [Fact]
        public async Task DatasetsShouldBeOrderedBySuggestions()
        {
            using ApiApplicationFactory f = new ApiApplicationFactory();
            using HttpClient client = f.CreateClient();

            using IApi api = f.CreateApi();
            DatasetPost d1 = await api.CreateDataset();
            DatasetPost d2 = await api.CreateDataset();
            DatasetPost d3 = await api.CreateDataset();

            await api.CreateSuggestion(datasetUri: d2.Title);
            await api.CreateSuggestion(datasetUri: d2.Title);
            await api.CreateSuggestion(datasetUri: d3.Title);


            using HttpResponseMessage response1 = await client.GetAsync($"/cms/datasets/order?property=suggestions");
            Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);
            List<string>? uris = await response1.Content.ReadFromJsonAsync<List<string>>();
            Assert.NotNull(uris);
            Assert.Equal(new[] { d3.Title, d2.Title }, uris);

            using HttpResponseMessage response2 = await client.GetAsync($"/cms/datasets/order?property=suggestions&reverse=true");
            Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);
            uris = await response2.Content.ReadFromJsonAsync<List<string>>();
            Assert.NotNull(uris);
            Assert.Equal(new[] { d2.Title, d3.Title }, uris);
        }
    }
}
