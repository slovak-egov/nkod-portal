using Abstractions;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System.Text;
using TestBase;

namespace CodelistProvider.Test
{
    public class CodelistShortIdTests : IClassFixture<StorageFixture>
    {
        private const string SchemeId = "http://example.com/codelist";

        private readonly StorageFixture fixture;

        public CodelistShortIdTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        private static string CreateCodelist(params string[] concepts)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("@prefix skos: <http://www.w3.org/2004/02/skos/core#> .");
            sb.AppendLine("@prefix custom: <https://data.gov.sk/custom/> .");
            sb.AppendLine($"<{SchemeId}> a skos:ConceptScheme ; skos:prefLabel \"Test\"@sk .");
            foreach (string concept in concepts)
            {
                sb.AppendLine(concept);
            }
            return sb.ToString();
        }

        private static string Concept(string id, string? shortId = null)
        {
            return $"<{id}> a skos:Concept ; skos:prefLabel \"{id}\"@sk {(shortId is not null ? $"; custom:shortId \"{shortId}\" " : string.Empty)}.";
        }

        private static async Task<Dictionary<string, string?>> Upload(InternalCodelistProvider provider, string content)
        {
            Assert.True(await provider.UpdateCodelist(new MemoryStream(Encoding.UTF8.GetBytes(content))));
            Codelist? codelist = await provider.GetCodelist(SchemeId);
            Assert.NotNull(codelist);
            return codelist.Items.Values.ToDictionary(i => i.Id, i => i.ShortId);
        }

        [Fact]
        public void ShortIdsShouldBeCreatedFromLocalName()
        {
            SkosConceptScheme scheme = SkosConceptScheme.Parse(CreateCodelist(
                Concept("http://example.com/codelist/CSV"),
                Concept("http://example.com/other/CSV"),
                Concept("http://example.com/codelist/by/4.0/"),
                Concept("http://example.com/codelist/Žilinský-kraj")))!;

            scheme.AssignShortIds(null);

            Assert.Equal("CSV", scheme.GetConcept("http://example.com/codelist/CSV")!.ShortId);
            Assert.Equal("CSV-2", scheme.GetConcept("http://example.com/other/CSV")!.ShortId);
            Assert.Equal("4-0", scheme.GetConcept("http://example.com/codelist/by/4.0/")!.ShortId);
            Assert.Equal("Zilinsky-kraj", scheme.GetConcept("http://example.com/codelist/Žilinský-kraj")!.ShortId);
        }

        [Fact]
        public void ExplicitShortIdsShouldBeKept()
        {
            SkosConceptScheme scheme = SkosConceptScheme.Parse(CreateCodelist(
                Concept("http://example.com/codelist/A", "B"),
                Concept("http://example.com/codelist/B")))!;

            scheme.AssignShortIds(null);

            Assert.Equal("B", scheme.GetConcept("http://example.com/codelist/A")!.ShortId);
            Assert.Equal("B-2", scheme.GetConcept("http://example.com/codelist/B")!.ShortId);
        }

        [Fact]
        public async Task ShortIdsShouldBeReturnedForStoredCodelists()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            TestDocumentStorageClient storageClient = new TestDocumentStorageClient(storage, AnonymousAccessPolicy.Default);
            InternalCodelistProvider provider = new InternalCodelistProvider(storageClient, new DefaultLanguagesSource(), null);

            Codelist? list = await provider.GetCodelist(DcatDataset.AccrualPeriodicityCodelist);
            Assert.NotNull(list);
            Assert.All(list.Items.Values, i => Assert.True(SkosConceptScheme.IsValidShortId(i.ShortId)));
            Assert.Equal(list.Items.Count, list.Items.Values.Select(i => i.ShortId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal("ANNUAL", list.Items["http://publications.europa.eu/resource/authority/frequency/ANNUAL"].ShortId);
        }

        [Fact]
        public async Task ItemShouldBeFoundByShortId()
        {
            using Storage storage = new Storage(fixture.GetStoragePath());
            TestDocumentStorageClient storageClient = new TestDocumentStorageClient(storage, AnonymousAccessPolicy.Default);
            InternalCodelistProvider provider = new InternalCodelistProvider(storageClient, new DefaultLanguagesSource(), null);

            CodelistItem? item = await provider.GetCodelistItem(DcatDataset.AccrualPeriodicityCodelist, "annual");
            Assert.NotNull(item);
            Assert.Equal("http://publications.europa.eu/resource/authority/frequency/ANNUAL", item.Id);

            item = await provider.GetCodelistItem(DcatDataset.AccrualPeriodicityCodelist, "http://publications.europa.eu/resource/authority/frequency/ANNUAL");
            Assert.NotNull(item);
            Assert.Equal("ANNUAL", item.ShortId);

            Assert.Null(await provider.GetCodelistItem(DcatDataset.AccrualPeriodicityCodelist, "not-existing"));
        }

        [Fact]
        public async Task ShortIdsShouldBePreservedAcrossUpdates()
        {
            using Storage storage = new Storage(fixture.GetStoragePath(false));
            TestDocumentStorageClient storageClient = new TestDocumentStorageClient(storage, new AllAccessFilePolicy());
            InternalCodelistProvider provider = new InternalCodelistProvider(storageClient, new DefaultLanguagesSource(), null);

            Dictionary<string, string?> ids = await Upload(provider, CreateCodelist(
                Concept("http://example.com/codelist/A"),
                Concept("http://example.com/codelist/B")));
            Assert.Equal("A", ids["http://example.com/codelist/A"]);
            Assert.Equal("B", ids["http://example.com/codelist/B"]);

            // B is removed, a new concept with the same local name must not take its id, and an attempt to change id of A is ignored
            ids = await Upload(provider, CreateCodelist(
                Concept("http://example.com/codelist/A", "X"),
                Concept("http://example.com/other/B")));
            Assert.Equal("A", ids["http://example.com/codelist/A"]);
            Assert.Equal("B-2", ids["http://example.com/other/B"]);
            Assert.Equal(2, ids.Count);

            // B returns with its original id
            ids = await Upload(provider, CreateCodelist(
                Concept("http://example.com/codelist/A"),
                Concept("http://example.com/codelist/B"),
                Concept("http://example.com/other/B")));
            Assert.Equal("A", ids["http://example.com/codelist/A"]);
            Assert.Equal("B", ids["http://example.com/codelist/B"]);
            Assert.Equal("B-2", ids["http://example.com/other/B"]);

            // Ids are preserved also after a reload from storage
            InternalCodelistProvider reloaded = new InternalCodelistProvider(storageClient, new DefaultLanguagesSource(), null);
            Codelist? codelist = await reloaded.GetCodelist(SchemeId);
            Assert.NotNull(codelist);
            Assert.Equal("B", codelist.Items["http://example.com/codelist/B"].ShortId);
            Assert.Equal("B-2", codelist.Items["http://example.com/other/B"].ShortId);
        }
    }
}
