using Abstractions;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using TestBase;

namespace CodelistProvider.Test
{
    public class InternalCodelistProviderTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public InternalCodelistProviderTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task CodelistsShouldBeEmptyByDefault()
        {
            Storage storage = new Storage(fixture.GetStoragePath(false));
            TestDocumentStorageClient storageClient = new TestDocumentStorageClient(storage, AnonymousAccessPolicy.Default);
            InternalCodelistProvider provider = new InternalCodelistProvider(storageClient, new DefaultLanguagesSource());

            List<Codelist> lists = await provider.GetCodelists();
            Assert.Empty(lists);
        }

        [Fact]
        public async Task TwoCodelistsShouldBeReturned()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            TestDocumentStorageClient storageClient = new TestDocumentStorageClient(storage, AnonymousAccessPolicy.Default);
            InternalCodelistProvider provider = new InternalCodelistProvider(storageClient, new DefaultLanguagesSource());

            List<Codelist> lists = await provider.GetCodelists();
            Assert.Equal(2, lists.Count);
            Assert.All(lists, l =>
            {
                Assert.False(string.IsNullOrEmpty(l.Id));
                Assert.NotEmpty(l.Labels);
                Assert.All(l.Labels, b => Assert.False(string.IsNullOrEmpty(b.Value)));
                Assert.NotEmpty(l.Items);
                Assert.All(l.Items, i =>
                {
                    Assert.False(string.IsNullOrEmpty(i.Value.Id));
                    Assert.NotEmpty(i.Value.Labels);
                    Assert.True(i.Value.Labels.ContainsKey("sk"));
                });
            });
        }

        [Fact]
        public async Task SingleCodelistShouldBeReturned()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            TestDocumentStorageClient storageClient = new TestDocumentStorageClient(storage, AnonymousAccessPolicy.Default);
            InternalCodelistProvider provider = new InternalCodelistProvider(storageClient, new DefaultLanguagesSource());

            Codelist? list = await provider.GetCodelist("frequency");
            Assert.NotNull(list);
            Assert.NotEmpty(list.Id);
            Assert.NotEmpty(list.Labels);
            Assert.All(list.Labels, b => Assert.False(string.IsNullOrEmpty(b.Value)));
            Assert.NotEmpty(list.Items);
            Assert.All(list.Items, i =>
            {
                Assert.False(string.IsNullOrEmpty(i.Value.Id));
                Assert.NotEmpty(i.Value.Labels);
                Assert.True(i.Value.Labels.ContainsKey("sk"));
            });
        }

        [Fact]
        public async Task UnknownCodelistShouldNotBeReturned()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            TestDocumentStorageClient storageClient = new TestDocumentStorageClient(storage, AnonymousAccessPolicy.Default);
            InternalCodelistProvider provider = new InternalCodelistProvider(storageClient, new DefaultLanguagesSource());

            Codelist? list = await provider.GetCodelist("unknown");
            Assert.Null(list);
        }
    }
}