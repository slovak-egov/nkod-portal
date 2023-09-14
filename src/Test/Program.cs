using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using Test;

string sourceDir = args[0];
string targetDir = args[1];

TestAccess accessPolicy = new TestAccess();
foreach (string path in Directory.EnumerateDirectories(targetDir))
{
    Directory.Delete(path, true);
}
Storage storage = new Storage(targetDir);

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "Agent")))
{
    string content = File.ReadAllText(path);
    FoafAgent catalog = FoafAgent.Parse(content)!;

    Guid id = Guid.NewGuid();

    storage.InsertFile(
      content, new FileMetadata(id, catalog?.GetName("sk") ?? id.ToString(), FileType.PublisherRegistration, null, catalog.Uri.ToString(), true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy
  );
}

Dictionary<Uri, (Guid, DcatDataset)> datasets = new Dictionary<Uri, (Guid, DcatDataset)>();

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "Dataset")))
{
    string content = File.ReadAllText(path);
    DcatDataset catalog = DcatDataset.Parse(content)!;

    Guid id = Guid.NewGuid();

    foreach (Uri uri in catalog.Distributions)
    {
        datasets[uri] = (id, catalog);
    }

    Dictionary<string, string[]> additionalValues = new Dictionary<string, string[]>();
    if (catalog.Type is not null)
    {
        additionalValues["https://data.gov.sk/def/ontology/egov/DatasetType"] = new[] { catalog.Type.ToString() };
    }
    additionalValues["themes_sk"] = catalog.GetKeywords("sk").ToArray();

    storage.InsertFile(
        content, new FileMetadata(id, catalog?.GetTitle("sk") ?? id.ToString(), FileType.DatasetRegistration, null, catalog.Publisher!.ToString(), true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, additionalValues), true, accessPolicy
    );
}

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "Distribution")))
{
    string content = File.ReadAllText(path);
    DcatDistribution catalog = DcatDistribution.Parse(content)!;

    Guid id = Guid.NewGuid();

    (Guid parentId, DcatDataset dataset) = datasets[catalog.Uri];

    storage.InsertFile(
        content, new FileMetadata(id, catalog.GetTitle("sk") ?? id.ToString(), FileType.DistributionRegistration, parentId, dataset.Publisher.ToString(), true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy
    );
}

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "Catalog")))
{
    string content = File.ReadAllText(path);
    DcatCatalog catalog = DcatCatalog.Parse(content)!;

    Guid id = Guid.NewGuid();

    storage.InsertFile(
        content, new FileMetadata(id, catalog.GetTitle("sk") ?? id.ToString(), FileType.LocalCatalogRegistration, null, catalog.Publisher?.ToString(), true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy
    );
}

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "ConceptScheme")))
{
    string content = File.ReadAllText(path);
    
    storage.InsertFile(
        content, new FileMetadata(Guid.NewGuid(), Path.GetFileName(path), FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy
    );
}
