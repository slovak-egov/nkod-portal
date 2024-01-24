using NkodSk.Abstractions;
using NkodSk.RdfFulltextIndex;
using NkodSk.RdfFulltextIndex.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace RdfFulltextIndex.Test
{
    public class BaseTest : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public BaseTest(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void DocumentShouldBeFoundByTitle()
        {
            FileStorageQuery query = new FileStorageQuery { QueryText = "poriadky" };
            query.RequiredFacets = new List<string>
            {
                "publisher"
            };
            FulltextResponse list = fixture.Index.Search(query);
            Assert.Single(list.Documents);
        }

        [Fact]
        public void DocumentShouldBeFoundByPartOfTitle()
        {
            FileStorageQuery query = new FileStorageQuery { QueryText = "poriadk" };
            query.RequiredFacets = new List<string>
            {
                "publisher"
            };
            FulltextResponse list = fixture.Index.Search(query);
            Assert.Single(list.Documents);
        }

        [Fact]
        public void DocumentShouldNotBeFoundByTitle()
        {
            FileStorageQuery query = new FileStorageQuery { QueryText = "xxxx" };
            FulltextResponse list = fixture.Index.Search(query);
            Assert.Empty(list.Documents);
        }

        [Fact]
        public void DocumentBeforeTime()
        {
            FileStorageQuery query = new FileStorageQuery { QueryText = "poriadky" };
            query.DateTo = new DateOnly(2000, 1, 1);
            FulltextResponse list = fixture.Index.Search(query);
            Assert.Empty(list.Documents);
        }

        [Fact]
        public void DocumentAfterTime()
        {
            FileStorageQuery query = new FileStorageQuery { QueryText = "poriadky" };
            query.DateFrom = new DateOnly(2022, 1, 1);
            FulltextResponse list = fixture.Index.Search(query);
            Assert.Empty(list.Documents);
        }

        [Fact]
        public void DocumentInTime()
        {
            FileStorageQuery query = new FileStorageQuery { QueryText = "poriadky" };
            query.DateFrom = new DateOnly(2015, 1, 1);
            query.DateTo = new DateOnly(2016, 1, 1);
            FulltextResponse list = fixture.Index.Search(new FileStorageQuery { QueryText = "poriadky" });
            Assert.Single(list.Documents);
        }
    }
}
