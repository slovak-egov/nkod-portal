﻿using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using Test;
using VDS.RDF.Parsing;
using VDS.RDF;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Xml;

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
    conceptScheme.SetLabel(new LanguageDependedTexts { { "sk", lines[2] } });

    for (int i = 2; i < lines.Length; i += 2)
    {
        SkosConcept concept = conceptScheme.CreateConcept(new Uri(lines[i]));
        concept.SetLabel(new LanguageDependedTexts { { "sk", lines[i + 1] } });
    }

    storage.InsertFile(
       conceptScheme.ToString(), new FileMetadata(Guid.NewGuid(), Path.GetFileName(path), FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), true, accessPolicy
   );
}

void PrepareXml(string path)
{
    XmlDocument document = new XmlDocument();
    document.Load(path);
    XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
    nsmgr.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
    nsmgr.AddNamespace("skos", "http://www.w3.org/2008/05/skos-xl#");

    XmlNodeList? nodes = document.SelectNodes("//rdf:Description", nsmgr);

    if (nodes is not null)
    {
        foreach (XmlNode node in nodes)
        {
            bool isValid = false;
            XmlNodeList? literals = node.ChildNodes;
            if (literals is not null)
            {
                foreach (XmlNode literal in literals)
                {
                    string? lang = literal.Attributes?["xml:lang"]?.Value;
                    if (lang == "sk")
                    {
                        isValid = true;
                        break;
                    }
                }
            }
            if (!isValid)
            {
                node.ParentNode!.RemoveChild(node);
            }
        }
    }

    File.WriteAllText(path, document.OuterXml);
}

void SaveGraph(IGraph g, string name)
{
    IUriNode type = g.CreateUriNode("skos:Concept");
    int index = 0;
    foreach (Triple t in g.GetTriplesWithPredicateObject(g.GetUriNode("rdf:type"), type))
    {
        IUriNode subject = (IUriNode)t.Subject;

        List<Triple> labels = g.GetTriplesWithSubjectPredicate(subject, g.GetUriNode("skos:prefLabel")).ToList();
        bool hasSk = labels.Select(t => t.Object).OfType<ILiteralNode>().Any(n => n.Language == "sk");
        if (hasSk && index < 100)
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
    PrepareXml(Path.Combine(sourceDir, "ConceptScheme", "eurovoc.rdf"));

    IGraph g = new Graph();
    SkosConceptScheme.AddDefaultNamespaces(g);
    FileLoader.Load(g, Path.Combine(sourceDir, "ConceptScheme", "eurovoc.rdf"));

    SaveGraph(g, "eurovoc");
}

{
    PrepareXml(Path.Combine(sourceDir, "ConceptScheme", "places-skos.rdf"));
    PrepareXml(Path.Combine(sourceDir, "ConceptScheme", "countries-skos.rdf"));

    IGraph g = new Graph();
    SkosConceptScheme.AddDefaultNamespaces(g);
    FileLoader.Load(g, Path.Combine(sourceDir, "ConceptScheme", "places-skos.rdf"));
    FileLoader.Load(g, Path.Combine(sourceDir, "ConceptScheme", "countries-skos.rdf"));
    SaveGraph(g, "places");
}

{
    SkosConceptScheme conceptScheme = SkosConceptScheme.Create(new Uri("http://www.iana.org/assignments/media-types"));
    conceptScheme.SetLabel(new LanguageDependedTexts { { "sk", "Typy súborov" } });

    foreach (string path in Directory.EnumerateFiles(Path.Combine(sourceDir, "ConceptScheme"), "iana*.csv"))
    {
        using (FileStream fs = File.OpenRead(path))
        using (TextReader tr = new StreamReader(fs))
        using (CsvReader csvReader = new CsvReader(tr, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ",", HasHeaderRecord = true}))
        {
            while (csvReader.Read())
            {
                SkosConcept concept = conceptScheme.CreateConcept(new Uri("http://www.iana.org/assignments/media-types/" + csvReader.GetField(0)!));
                concept.SetLabel(new LanguageDependedTexts { { "sk", csvReader.GetField(1)! } });
            }
        }
    }

    SaveGraph(conceptScheme.Graph, "iana");
}