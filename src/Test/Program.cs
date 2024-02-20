using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using VDS.RDF.Parsing;
using VDS.RDF;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Xml;
using TestBase;
using System.Xml.Linq;
using Abstractions;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System;
using VDS.RDF.Query.Expressions.Functions.Sparql.Boolean;
using System.Data;

string sourceDir = args[0];
string targetDir = args[1];

AllAccessFilePolicy accessPolicy = new AllAccessFilePolicy();
foreach (string path in Directory.EnumerateDirectories(targetDir))
{
    Directory.Delete(path, true);
}
Storage storage = new Storage(targetDir);

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "ConceptScheme"), "*.ttl"))
{
    string content = File.ReadAllText(path);

    storage.InsertFile(
        content, new FileMetadata(Guid.NewGuid(), Path.GetFileName(path), FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy
    );
}

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "ConceptScheme"), "*.txt"))
{
    string[] lines = File.ReadAllLines(path);

    SkosConceptScheme conceptScheme = SkosConceptScheme.Create(new Uri(lines[0]));
    conceptScheme.SetLabel(new LanguageDependedTexts { { "sk", lines[1] } });

    for (int i = 2; i < lines.Length; i += 2)
    {
        SkosConcept concept = conceptScheme.CreateConcept(new Uri(lines[i].Trim()));
        concept.SetLabel(new LanguageDependedTexts { { "sk", lines[i + 1].Trim() } });
    }

    storage.InsertFile(
       conceptScheme.ToString(), new FileMetadata(Guid.NewGuid(), Path.GetFileName(path), FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy
    );
}

void SaveGraph(IGraph g, string name, int? max)
{
    IUriNode type = g.CreateUriNode("skos:Concept");
    int index = 0;
    foreach (Triple t in g.GetTriplesWithPredicateObject(g.CreateUriNode("rdf:type"), type))
    {
        IUriNode subject = (IUriNode)t.Subject;

        List<Triple> labels = g.GetTriplesWithSubjectPredicate(subject, g.GetUriNode("skos:prefLabel")).ToList();
        bool hasSk = labels.Select(t => t.Object).OfType<ILiteralNode>().Any(n => n.Language == "sk");
        if (hasSk && (!max.HasValue || index < max.Value))
        {
            index++;
        }
        else
        {
            g.Retract(t);
        }
    }

    IUriNode schemeType = g.GetUriNode("skos:ConceptScheme");
    IUriNode rdfTypeNode = g.GetUriNode(new Uri(RdfSpecsHelper.RdfType));
    IUriNode skosConceptId = g.GetTriplesWithPredicateObject(rdfTypeNode, schemeType).Select(t => t.Subject).OfType<IUriNode>().First();
    SkosConceptScheme original = new SkosConceptScheme(g, skosConceptId);

    SkosConceptScheme conceptScheme = SkosConceptScheme.Create(skosConceptId.Uri);
    conceptScheme.SetLabel(new LanguageDependedTexts { { "sk", name } });

    foreach (SkosConcept originalConcept in original.Concepts)
    {
        SkosConcept concept = conceptScheme.CreateConcept(originalConcept.Uri);
        string? label = originalConcept.GetLabel("sk");
        if (label != null)
        {
            concept.SetLabel(new LanguageDependedTexts { { "sk", label } });
        }
    }

    string content = conceptScheme.ToString();

    storage.InsertFile(
       content, new FileMetadata(Guid.NewGuid(), name, FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy);
    Console.WriteLine(content.Length);
}

{
    //PrepareXml(Path.Combine(sourceDir, "ConceptScheme", "eurovoc.rdf"));

    IGraph g = new Graph();
    SkosConceptScheme.AddDefaultNamespaces(g);

    SkosConceptScheme conceptScheme = SkosConceptScheme.Create(new Uri(DcatDataset.EuroVocThemeCodelist));
    conceptScheme.SetLabel(new Dictionary<string, string> { { "sk", "EuroVoc" } });

    string content = conceptScheme.ToString();

    storage.InsertFile(
       content, new FileMetadata(Guid.NewGuid(), "eurovoc", FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy);
    Console.WriteLine(content.Length);
}

{
    IGraph g = new Graph();
    SkosConceptScheme.AddDefaultNamespaces(g);

    FileLoader.Load(g, Path.Combine(sourceDir, "ConceptScheme", "nuts2004.ttl"));

    SkosConceptScheme conceptScheme = SkosConceptScheme.Create(new Uri(DcatDataset.SpatialCodelist));
    conceptScheme.SetLabel(new LanguageDependedTexts { { "sk", "Geografické územie" } });

    IUriNode rdfTypeNode = g.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));

    void LoadNodes(string type)
    {
        IUriNode ontologyType = g.CreateUriNode(new Uri(type));
        IUriNode labelNode = g.CreateUriNode(new Uri("http://www.w3.org/2004/02/skos/core#prefLabel"));
        foreach (IUriNode subject in g.GetTriplesWithPredicateObject(rdfTypeNode, ontologyType).Select(t => t.Subject).OfType<IUriNode>())
        {
            SkosConcept concept = conceptScheme.CreateConcept(subject.Uri);
            concept.SetLabel(new Dictionary<string, string> { { "sk", g.GetTriplesWithSubjectPredicate(subject, labelNode).Select(f => f.Object).OfType<ILiteralNode>().First(l => l.Language == "sk").Value } });
        }
    }

    LoadNodes("https://data.gov.sk/def/ontology/location/NUTS1");
    LoadNodes("https://data.gov.sk/def/ontology/location/NUTS3");
    LoadNodes("https://data.gov.sk/def/ontology/location/LAU1");
    LoadNodes("https://data.gov.sk/def/ontology/location/LAU2");

    SkosConcept concept = conceptScheme.CreateConcept(new Uri("http://publications.europa.eu/resource/authority/continent/EUROPE"));
    concept.SetLabel(new LanguageDependedTexts { { "sk", "Európa" } });


    string content = conceptScheme.ToString();

    storage.InsertFile(
       content, new FileMetadata(Guid.NewGuid(), "places", FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy);
    Console.WriteLine(content.Length);
}

{
    SkosConceptScheme conceptScheme = SkosConceptScheme.Create(new Uri("http://www.iana.org/assignments/media-types"));
    conceptScheme.SetLabel(new LanguageDependedTexts { { "sk", "Typy súborov" } });

    foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "ConceptScheme"), "iana*.csv"))
    {
        using (FileStream fs = File.OpenRead(path))
        using (TextReader tr = new StreamReader(fs))
        using (CsvReader csvReader = new CsvReader(tr, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ",", HasHeaderRecord = true }))
        {
            while (csvReader.Read())
            {
                SkosConcept concept = conceptScheme.CreateConcept(new Uri("http://www.iana.org/assignments/media-types/" + csvReader.GetField(1)!));
                concept.SetLabel(new LanguageDependedTexts { { "sk", csvReader.GetField(1)! } });
            }
        }
    }

    SaveGraph(conceptScheme.Graph, "iana", null);
}





TestDocumentStorageClient documentStorageClient = new TestDocumentStorageClient(storage, new AllAccessFilePolicy());
InternalCodelistProvider codelistProvider = new InternalCodelistProvider(documentStorageClient, new DefaultLanguagesSource(), null);









HashSet<string> publishers = new HashSet<string>();

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "Agent")))
{
    string content = File.ReadAllText(path);
    FoafAgent catalog = FoafAgent.Parse(content)!;

    if (catalog.LegalForm is null)
    {
        catalog.LegalForm = new Uri("https://data.gov.sk/def/legal-form-type/995");
    }

    Guid id = Guid.NewGuid();

    publishers.Add(catalog.Uri.ToString());

    storage.InsertFile(
      content, new FileMetadata(id, catalog.GetName("sk") ?? id.ToString(), FileType.PublisherRegistration, null, catalog.Uri.ToString(), true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy
  );
}

void RemoveEmptyTexts(Dictionary<string, string> texts, Action<Dictionary<string, string>> setAction)
{
    if (texts.Values.Any(string.IsNullOrWhiteSpace))
    {
        Dictionary<string, string> newValues = new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> pair in texts)
        {
            if (!string.IsNullOrWhiteSpace(pair.Value))
            {
                newValues[pair.Key] = pair.Value;
            }
        }
        setAction(newValues);
    }
}

void RemoveEmptyTextsCollection(Dictionary<string, List<string>> texts, Action<Dictionary<string, List<string>>> setAction)
{
    if (texts.Values.Any(v => v.Count == 0 || v.Any(string.IsNullOrWhiteSpace)))
    {
        Dictionary<string, List<string>> newValues = new Dictionary<string, List<string>>();
        foreach (KeyValuePair<string, List<string>> pair in texts)
        {
            List<string> newList = new List<string>(pair.Value.Where(v => !string.IsNullOrWhiteSpace(v)));
            if (newValues.Count > 0)
            {
                newValues[pair.Key] = newList;
            }
        }
        setAction(newValues);
    }
}

async Task<bool> CheckCodelistValue(string codelistId, Uri? uri, bool required)
{
    if (uri is not null)
    {
        CodelistItem? codelistItem = await codelistProvider.GetCodelistItem(codelistId, uri.ToString());
        return codelistItem is not null;
    }
    return !required;
}


Dictionary<Uri, Guid> distributionUriToDataset = new Dictionary<Uri, Guid>();
Dictionary<Guid, FileMetadata> datasetMetadatas = new Dictionary<Guid, FileMetadata>();
Dictionary<Uri, FileMetadata> datasetMetadataByUri = new Dictionary<Uri, FileMetadata>();
Dictionary<Guid, Uri> datasetPartOf = new Dictionary<Guid, Uri>();

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "Dataset")))
{
    string content = File.ReadAllText(path);
    DcatDataset catalog = DcatDataset.Parse(content)!;

    RemoveEmptyTexts(catalog.Title, catalog.SetTitle);
    RemoveEmptyTexts(catalog.Description, catalog.SetDescription);
    RemoveEmptyTextsCollection(catalog.Keywords, catalog.SetKeywords);

    bool isValid = true;

    string title = catalog.Title.GetValueOrDefault("sk", string.Empty);
    if (title.Length == 0)
    {
        Console.WriteLine("Dataset without sk title: " + catalog.Uri);
        isValid = false;
    }
    if (title.Length > 500)
    {
        Console.WriteLine("Dataset with large sk title: " + catalog.Uri + $" {title}");
        isValid = false;
    }

    string description = catalog.Description.GetValueOrDefault("sk", string.Empty);
    //if (description.Length == 0)
    //{
    //    Console.WriteLine("Dataset without sk description: " + catalog.Uri);
    //    continue;
    //}
    if (description.Length > 4000)
    {
        Console.WriteLine("Dataset with large sk description: " + catalog.Uri);
        isValid = false;
    }

    if (catalog.Publisher == null)
    {
        Console.WriteLine("Dataset without publisher: " + catalog.Uri);
        isValid = false;
    }

    if (!publishers.Contains(catalog.Publisher.ToString()))
    {
        Console.WriteLine("Dataset with unknown publisher: " + catalog.Uri + $" {catalog.Publisher}");
        isValid = false;
    }

    //List<string> keywords = catalog.Keywords.GetValueOrDefault("sk", new List<string>());
    //if (keywords.Count == 0)
    //{
    //    Console.WriteLine("Dataset without sk keywords: " + catalog.Uri);
    //    continue;
    //}

    foreach (Uri uri in catalog.Type)
    {
        if (!await CheckCodelistValue(DcatDataset.TypeCodelist, uri, false))
        {
            Console.WriteLine("Dataset with unknown type: " + catalog.Uri + $" ({uri})");
            isValid = false;
        }
    }

    foreach (Uri uri in catalog.NonEuroVocThemes)
    {
        if (!await CheckCodelistValue(DcatDataset.ThemeCodelist, uri, true))
        {
            Console.WriteLine("Dataset with unknown theme: " + catalog.Uri + $" ({uri})");
            isValid = false;
        }
    }

    if (!await CheckCodelistValue(DcatDataset.AccrualPeriodicityCodelist, catalog.AccrualPeriodicity, false))
    {
        Console.WriteLine("Dataset with unknown AccrualPeriodicity: " + catalog.Uri + $" ({catalog.AccrualPeriodicity})");
        isValid = false;
    }

    if (catalog.Spatial.Any(s => s.Host == "data.gov.sk" && s.LocalPath.StartsWith("/def/")))
    {
        List<Uri> newSpatial = new List<Uri>();
        foreach (Uri uri in catalog.Spatial)
        {
            if (uri.Host == "data.gov.sk" && uri.LocalPath.StartsWith("/def/"))
            {
                UriBuilder builder = new UriBuilder(uri);
                builder.Path = "/id/" + builder.Path.Substring(5);
                newSpatial.Add(builder.Uri);
            }
            else
            {
                newSpatial.Add(uri);
            }
        }
        catalog.Spatial = newSpatial;
    }

    foreach (Uri uri in catalog.Spatial)
    {
        if (!await CheckCodelistValue(DcatDataset.SpatialCodelist, uri, false))
        {
            Console.WriteLine("Dataset with unknown spatial: " + catalog.Uri + $" ({uri})");
            isValid = false;
        }
    }

    if (catalog.Temporal is not null)
    {
        if (!catalog.Temporal.StartDate.HasValue && !catalog.Temporal.EndDate.HasValue)
        {
            catalog.SetTemporal(null, null);
        }
    }

    if (catalog.ContactPoint is not null)
    {
        RemoveEmptyTexts(catalog.ContactPoint.Name, catalog.ContactPoint.SetNames);
        if (catalog.ContactPoint.Email is not null)
        {
            if (string.IsNullOrWhiteSpace(catalog.ContactPoint.Email))
            {
                catalog.ContactPoint.Email = null;
            }
            else if (!Regex.IsMatch(catalog.ContactPoint.Email, @"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])"))
            {
                Console.WriteLine("Dataset with invalid email: " + catalog.Uri);
                continue;
            }
        }
    }

    if (!isValid)
    {
        continue;
    }

    content = catalog.ToString();

    FileMetadata metadata = catalog.UpdateMetadata(catalog.Distributions.Any());

    metadata = metadata with { Created = catalog.Issued ?? new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), LastModified = catalog.Modified ?? new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) };

    if (catalog.Issued.HasValue)
    {
        metadata = metadata with { Created = catalog.Issued.Value };
    }

    if (catalog.Modified.HasValue)
    {
        metadata = metadata with { LastModified = catalog.Modified.Value };
    }

    Match m = Regex.Match(catalog.Uri.ToString(), "^https://data.gov.sk/set/(.+)$");
    if (m.Success && m.Groups.Count >= 2 && Guid.TryParse(m.Groups[1].Value, out Guid id))
    {
        metadata = metadata with { Id = id };
    }

    if (catalog.IsPartOf is not null)
    {
        datasetPartOf[metadata.Id] = catalog.IsPartOf;
    }

    foreach (Uri uri in catalog.Distributions)
    {
        distributionUriToDataset[uri] = metadata.Id;
    }
    datasetMetadatas[metadata.Id] = metadata;
    datasetMetadataByUri[catalog.Uri] = metadata;

    storage.InsertFile(
        content, metadata, true, accessPolicy
    );
}

foreach ((Guid childId, Uri parentUri) in datasetPartOf)
{
    if (datasetMetadataByUri.TryGetValue(parentUri, out FileMetadata? parentMetadata) && datasetMetadatas.TryGetValue(childId, out FileMetadata? childMetadata))
    {
        FileState state = storage.GetFileState(childMetadata.Id, accessPolicy)!;
        DcatDataset child = DcatDataset.Parse(state.Content!)!;
        child.IsPartOfInternalId = parentMetadata.Id.ToString();
        childMetadata = child.UpdateMetadata(true, state.Metadata);
        storage.InsertFile(child.ToString(), childMetadata, true, accessPolicy);
        datasetMetadatas[childId] = childMetadata;

        FileState parentState = storage.GetFileState(parentMetadata.Id, accessPolicy)!;
        DcatDataset parent = DcatDataset.Parse(parentState.Content!)!;
        if (!parent.IsSerie)
        {
            parent.IsSerie = true;
            parentMetadata = parent.UpdateMetadata(true, parentState.Metadata);
            storage.InsertFile(parent.ToString(), parentMetadata, true, accessPolicy);
            datasetMetadatas[parentMetadata.Id] = parentMetadata;
        }
    }
    else
    {
        Console.WriteLine("Parent not found: " + parentUri);
    }
}

string filesDir = @"F:\Backup\DataGov2\files";

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "Distribution")))
{
    string content = File.ReadAllText(path);
    DcatDistribution catalog = DcatDistribution.Parse(content)!;

    if (distributionUriToDataset.TryGetValue(catalog.Uri, out Guid datasetId))
    {
        bool isValid = true;

        RemoveEmptyTexts(catalog.Title, catalog.SetTitle);

        if (catalog.TermsOfUse is not null)
        {
            if (!await CheckCodelistValue(DcatDistribution.LicenseCodelist, catalog.TermsOfUse.AuthorsWorkType, true))
            {
                Console.WriteLine("Distribution with unknown AuthorsWorkType: " + catalog.Uri + $" ({catalog.TermsOfUse.AuthorsWorkType})");
                isValid = false;
            }

            if (!await CheckCodelistValue(DcatDistribution.LicenseCodelist, catalog.TermsOfUse.OriginalDatabaseType, true))
            {
                Console.WriteLine("Distribution with unknown OriginalDatabaseType: " + catalog.Uri + $" ({catalog.TermsOfUse.OriginalDatabaseType})");
                isValid = false;
            }

            if (!await CheckCodelistValue(DcatDistribution.LicenseCodelist, catalog.TermsOfUse.DatabaseProtectedBySpecialRightsType, true))
            {
                Console.WriteLine("Distribution with unknown DatabaseProtectedBySpecialRightsType: " + catalog.Uri + $" ({catalog.TermsOfUse.DatabaseProtectedBySpecialRightsType})");
                isValid = false;
            }

            if (!await CheckCodelistValue(DcatDistribution.PersonalDataContainmentTypeCodelist, catalog.TermsOfUse.PersonalDataContainmentType, true))
            {
                Console.WriteLine("Distribution with unknown PersonalDataContainmentType: " + catalog.Uri + $" ({catalog.TermsOfUse.PersonalDataContainmentType})");
                isValid = false;
            }
        }

        void FilterMediaType(Uri? format, Action<Uri> setFormat)
        {
            if (format is not null)
            {
                string address = format.ToString();
                int index = address.IndexOf(";");
                if (index >= 0)
                {
                    address = address.Substring(0, index);
                }
                setFormat(new Uri(address));
            }
        }

        if (catalog.Format is not null && !await CheckCodelistValue(DcatDistribution.FormatCodelist, catalog.Format, false))
        {
            string address = catalog.Format.ToString();
            int index = address.LastIndexOf("/");
            if (index >= 0)
            {
                address = address.Substring(0, index + 1) + address.Substring(index + 1).ToUpperInvariant();
                catalog.Format = new Uri(address);
            }
        }

        if (!await CheckCodelistValue(DcatDistribution.FormatCodelist, catalog.Format, false))
        {
            Console.WriteLine("Distribution with unknown Format: " + catalog.Uri + $" ({catalog.Format})");
            isValid = false;
        }



        FilterMediaType(catalog.MediaType, v => catalog.MediaType = v);
        FilterMediaType(catalog.CompressFormat, v => catalog.CompressFormat = v);
        FilterMediaType(catalog.PackageFormat, v => catalog.PackageFormat = v);


        if (!await CheckCodelistValue(DcatDistribution.MediaTypeCodelist, catalog.MediaType, false))
        {
            Console.WriteLine("Distribution with unknown MediaType: " + catalog.Uri + $" ({catalog.MediaType})");
            isValid = false;
        }

        if (!await CheckCodelistValue(DcatDistribution.MediaTypeCodelist, catalog.CompressFormat, false))
        {
            Console.WriteLine("Distribution with unknown CompressFormat: " + catalog.Uri + $" ({catalog.CompressFormat})");
            isValid = false;
        }

        if (!await CheckCodelistValue(DcatDistribution.MediaTypeCodelist, catalog.PackageFormat, false))
        {
            Console.WriteLine("Distribution with unknown PackageFormat: " + catalog.Uri + $" ({catalog.PackageFormat})");
            isValid = false;
        }

        if (!isValid)
        {
            continue;
        }

        FileMetadata datasetMetadata = datasetMetadatas[datasetId];

        FileMetadata metadata = catalog.UpdateMetadata(datasetMetadata);
        datasetMetadata = catalog.UpdateDatasetMetadata(datasetMetadata);

        storage.InsertFile(
            content, metadata, true, accessPolicy
        );


        if (catalog.DownloadUrl is not null && catalog.DownloadUrl.Host == "data.gov.sk")
        {
            int index = catalog.DownloadUrl.LocalPath.LastIndexOf("/");
            string fileName = index >= 0 ? catalog.DownloadUrl.LocalPath.Substring(index + 1) : catalog.DownloadUrl.LocalPath;
            string downloadPath = Path.Combine(filesDir, Path.GetFileName(path));
            if (File.Exists(downloadPath))
            {
                FileMetadata downloadMetadata = new FileMetadata(Guid.NewGuid(), fileName, FileType.DistributionFile, metadata.Id, metadata.Publisher, true, fileName, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
                storage.InsertFile("", downloadMetadata, false, accessPolicy);
                string targetPath = Path.Combine(targetDir, "protected", downloadMetadata.Id.ToString("N"));
                File.Copy(downloadPath, targetPath, true);

                catalog.DownloadUrl = new Uri("https://data.slovensko.sk/download?id=" + downloadMetadata.Id);

                content = catalog.ToString();
                storage.InsertFile(content, metadata, true, accessPolicy);
            }
        }

        storage.UpdateMetadata(datasetMetadata, accessPolicy);
        datasetMetadatas[datasetId] = datasetMetadata;
    }
    else
    {
        Console.WriteLine("Dataset not found: " + catalog.Uri);
    }
}

foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "Catalog")))
{
    string content = File.ReadAllText(path);
    DcatCatalog catalog = DcatCatalog.Parse(content)!;

    RemoveEmptyTexts(catalog.Title, catalog.SetTitle);
    RemoveEmptyTexts(catalog.Description, catalog.SetDescription);

    bool isValid = true;

    string title = catalog.Title.GetValueOrDefault("sk", string.Empty);
    if (title.Length == 0)
    {
        Console.WriteLine("Catalog without sk title: " + catalog.Uri);
        isValid = false;
    }
    if (title.Length > 200)
    {
        Console.WriteLine("Catalog with large sk title: " + catalog.Uri);
        isValid = false;
    }

    string description = catalog.Description.GetValueOrDefault("sk", string.Empty);
    //if (description.Length == 0)
    //{
    //    Console.WriteLine("Catalog without sk description: " + catalog.Uri);
    //    continue;
    //}
    if (description.Length > 4000)
    {
        Console.WriteLine("Catalog with large sk description: " + catalog.Uri);
        isValid = false;
    }

    if (catalog.Publisher == null)
    {
        Console.WriteLine("Catalog without publisher: " + catalog.Uri);
        isValid = false;
    }

    if (catalog.ContactPoint is not null)
    {
        RemoveEmptyTexts(catalog.ContactPoint.Name, catalog.ContactPoint.SetNames);
        if (catalog.ContactPoint.Email is not null)
        {
            if (string.IsNullOrWhiteSpace(catalog.ContactPoint.Email))
            {
                catalog.ContactPoint.Email = null;
            }
            else if (!Regex.IsMatch(catalog.ContactPoint.Email, @"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])"))
            {
                Console.WriteLine("Dataset with invalid email: " + catalog.Uri);
                isValid = false;
            }
        }
    }

    if (!await CheckCodelistValue(DcatCatalog.LocalCatalogTypeCodelist, catalog.Type, true))
    {
        Console.WriteLine("Catalog with unknown Type: " + catalog.Uri + $" ({catalog.Type})");
        isValid = false;
    }

    if (!isValid)
    {
        continue;
    }


    FileMetadata metadata = catalog.UpdateMetadata();

    storage.InsertFile(
        content, metadata, true, accessPolicy
    );

    foreach (Uri uri in catalog.GetUrisFromUriNode("dcat:dataset"))
    {
        if (datasetMetadataByUri.TryGetValue(uri, out FileMetadata? datasetMetadata))
        {
            FileMetadata MarkAsHarvested(FileMetadata metadata)
            {
                FileState state = storage.GetFileState(metadata.Id, accessPolicy)!;
                metadata = state.Metadata;
                Dictionary<string, string[]> additionalValues = metadata.AdditionalValues ?? new Dictionary<string, string[]>();
                additionalValues["localCatalog"] = new string[] { catalog.Uri.ToString() };
                additionalValues["Harvested"] = new string[] { "true" };
                metadata = metadata with { AdditionalValues = additionalValues };

                storage.DeleteFile(metadata.Id, accessPolicy);
                storage.InsertFile(state.Content!, metadata, true, accessPolicy);
                return metadata;
            }
                       
            datasetMetadatas[datasetMetadata.Id] = MarkAsHarvested(datasetMetadata);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery { ParentFile = datasetMetadata.Id, OnlyTypes = new List<FileType> { FileType.DistributionRegistration } }, accessPolicy);
            foreach (FileState state in response.Files)
            {
                MarkAsHarvested(state.Metadata);
            }
        }
    }
}