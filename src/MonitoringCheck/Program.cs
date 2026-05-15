using Abstractions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MonitoringCheck;
using NkodSk.Abstractions;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml;
using ValidationCore;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Paths;
using VDS.RDF.Storage;
using VDS.RDF.Writing;
using RdfObject = MonitoringCheck.RdfObject;
using ValidationResults = ValidationCore.ValidationResults;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddEnvironmentVariables(prefix: "APP_")
    .AddCommandLine(args)
    .Build();

string? iamClientUrl = config["IAMUrl"];
if (!Uri.IsWellFormedUriString(iamClientUrl, UriKind.Absolute))
{
    throw new Exception("Unable to get IAMUrl");
}

string? monitoringSettingsUrl = config["MonitoringSettingsUrl"];
if (!Uri.IsWellFormedUriString(monitoringSettingsUrl, UriKind.Absolute))
{
    throw new Exception("Unable to get MonitoringSettingsUrl");
}

StaticHttpContextAccessor httpContextAccessor = new StaticHttpContextAccessor(null);
StaticHttpClientFactory iamClientHttpClientFactory = new StaticHttpClientFactory(_ => new HttpClient { BaseAddress = new Uri(iamClientUrl) });
IAMClient.IdentityAccessManagementClient iamClient = new IAMClient.IdentityAccessManagementClient(iamClientHttpClientFactory, httpContextAccessor);

string? harvesterAuth = config["HarvesterAuth"];
if (string.IsNullOrWhiteSpace(harvesterAuth))
{
    throw new Exception("HarvesterAuth is not defined");
}
string? token = await iamClient.LoginHarvester(harvesterAuth, null);
if (string.IsNullOrEmpty(token))
{
    throw new Exception("Unable to obtain token");
}

httpContextAccessor = new StaticHttpContextAccessor(token, ["Harvester"]);
StaticHttpClientFactory monitoringSettingsHttpClientFactory = new StaticHttpClientFactory(_ => new HttpClient { BaseAddress = new Uri(monitoringSettingsUrl) });
MonitoringSettingsClient.MonitoringSettingsClient monitoringSettingsClient = new MonitoringSettingsClient.MonitoringSettingsClient(monitoringSettingsHttpClientFactory, httpContextAccessor);

List<MonitoringNotificationSettings> notificationSettings = await monitoringSettingsClient.GetSettings();

Console.WriteLine($"Monitoring settings count: {notificationSettings.Count}");
foreach (MonitoringNotificationSettings setting in notificationSettings)
{
    Console.WriteLine(JsonSerializer.Serialize(setting));
}
Console.WriteLine();

EmailOptions? emailOptions = config.GetSection("EmailOptions").Get<EmailOptions>();

MonitoringOptions? options = config.Get<MonitoringOptions>();

Console.WriteLine("Configured options:");
Console.WriteLine(JsonSerializer.Serialize(options));
Console.WriteLine();

string? logDir = options?.ExecutorLogsPath;
string? pipelinesDir = options?.PipelinesPath;
string? outputDir = options?.OutputPath;

if (config["RemoveLast"] == "1" && Directory.Exists(outputDir))
{
    DateTimeOffset timeLimit = DateTimeOffset.UtcNow.AddDays(-1);
    foreach (string? path in Directory.GetFiles(outputDir, "*.trig", SearchOption.TopDirectoryOnly))
    {
        FileInfo fileInfo = new FileInfo(path);
        if (fileInfo.Exists && fileInfo.LastWriteTimeUtc >= timeLimit)
        {
            try
            {
                string dir = Path.Combine(outputDir, "old");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.Move(path, Path.Combine(dir, Guid.NewGuid().ToString() + Path.GetExtension(path)), true);
                Console.WriteLine($"Deleted file: {path}");
            }
            catch
            {
                Console.WriteLine($"Unable to delete file: {path}");
            }
        }
    }
}

if (!Directory.Exists(logDir))
{
    throw new Exception("Log dir does not exists");
}

if (!Directory.Exists(pipelinesDir))
{
    throw new Exception("Pipelines dir does not exists");
}

if (!Directory.Exists(outputDir))
{
    throw new Exception("Output dir does not exists");
}

List<PipelineLog> logs = PipelineLog.Load(logDir);

PipelineCollection pipelines = new PipelineCollection();
foreach (string path in Directory.GetFiles(pipelinesDir, "*.trig", SearchOption.TopDirectoryOnly).Order())
{
    string name = Path.GetFileNameWithoutExtension(path);
    if (name != "00 - Cache" && name != "00 - Nahrát aktuální stav do GraphDB" && name != "01 - Harvestace data.gov.sk - CKAN API" && name != "02 - Transformace data.gov.sk - CKAN" && !string.IsNullOrWhiteSpace(name))
    {
        pipelines.Add(new Pipeline(path));
    }
}

ValidationOptions validationOptions = new ValidationOptions { DefaultCodelistItemValidator = new PublicCodelistItemValidator(), CheckForDatasetSeriesCallback = _ => true };
Validator validator = new Validator(validationOptions);

Console.WriteLine("Pipelines:");
foreach (Pipeline pipeline in pipelines)
{
    Console.WriteLine(pipeline.Label);
}
Console.WriteLine();

Pipeline startPipeline = pipelines.First();

List<PipelineLog> harvestationStartups = logs.Where(l => UriEqualityComparer.Instance.Equals(l.Execution.Pipeline, startPipeline.SubjectId) && l.IsDone && l.Started.HasValue).OrderBy(l => l.Started.GetValueOrDefault()).ToList();

Console.WriteLine("All startups:");
foreach (PipelineLog log in harvestationStartups)
{
    Console.WriteLine(log.Dir);
}
Console.WriteLine();

foreach (PipelineLog firstLog in harvestationStartups)
{
    DateTimeOffset? harvestationStart = firstLog.Started;
    if (harvestationStart.HasValue)
    {
        using FileStream validationStream = new FileStream(Path.Combine(outputDir, "last-validation.csv"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        using CsvHelper.CsvWriter validationWriter = new CsvHelper.CsvWriter(new StreamWriter(validationStream), new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { Delimiter = ";", HasHeaderRecord = true });

        validationStream.SetLength(0);
        validationWriter.WriteField("Predicate");
        validationWriter.WriteField("Dataset");
        validationWriter.WriteField("Context");
        validationWriter.WriteField("Note");

        Console.WriteLine($"Starting analysis of harvestation: {harvestationStart.Value:G}");

        string harvestionationName = harvestationStart.Value.UtcDateTime.ToString("yyyy-MM-dd-HH-mm-ss");
        string harvestionationLogName = harvestionationName + ".trig";
        string outputFilePath = Path.Combine(outputDir, harvestionationLogName);

        Console.WriteLine($"Harvestation output file: {outputFilePath}");

        if (File.Exists(outputFilePath))
        {
            Console.WriteLine("Output file exists - stop");
            continue;
        }

        Harvestation harvestation = new Harvestation(harvestionationName);
        Dictionary<Uri, HarvestationResult> resultByCatalogs = new Dictionary<Uri, HarvestationResult>(UriEqualityComparer.Instance);
        Dictionary<Uri, HarvestationResult> resultByPublishers = new Dictionary<Uri, HarvestationResult>(UriEqualityComparer.Instance);
        using Graph outputGraph = new Graph(harvestation.IdentifierNode.Uri);

        HarvestationResult GetOrAddResultForPublisher(Uri publisher)
        {
            if (!resultByPublishers.TryGetValue(publisher, out HarvestationResult? publisherResult))
            {
                publisherResult = new HarvestationResult(harvestation, publisher, "publisher", publisher.OriginalString);
                resultByPublishers[publisher] = publisherResult;
            }
            return publisherResult;
        }

        void AddResultForPublisher(Uri publisher, HarvestationResult result)
        {
            HarvestationResult publisherResult = GetOrAddResultForPublisher(publisher);
            publisherResult.Add(result);
        }

        DateTimeOffset? lastHarvestationStarted = null;
        bool isHarvestationCompleted = true;
        bool harvestationFailed = false;

        foreach (Pipeline pipeline in pipelines)
        {
            Console.WriteLine($"Searching for pipeline: {pipeline.SubjectId} - {pipeline.Label}");
            List<PipelineLog> currentPipelineLogs = logs.Where(l => UriEqualityComparer.Instance.Equals(l.Execution.Pipeline, pipeline.SubjectId) && l.Started.HasValue && l.IsDone).ToList();

            PipelineLog? log = null;
            if (lastHarvestationStarted.HasValue)
            {
                log = currentPipelineLogs.Where(l => l.Started.HasValue && l.Started.Value >= lastHarvestationStarted.Value).OrderBy(l => l.Started.GetValueOrDefault()).FirstOrDefault();
            }
            else
            {
                log = firstLog;
                lastHarvestationStarted = log.Started;
                if (!lastHarvestationStarted.HasValue)
                {
                    isHarvestationCompleted = false;
                    break;
                }
            }

            Console.WriteLine($"Found log: {(log?.Dir ?? "---")}");
            Console.WriteLine($"Pipeline started: {log?.Started:G}");
            Console.WriteLine($"Pipeline ended: {log?.Ended:G}");
            Console.WriteLine($"Pipeline duration: {log?.Duration:G}");
            Console.WriteLine($"Pipeline status: {log?.Execution.Status:G}");

            if (log is not null)
            {
                harvestation.Add(pipeline, log);

                if (log.Execution.IsFailed)
                {
                    isHarvestationCompleted = true;
                    harvestationFailed = false;
                    break;
                }
            }
            else
            {
                if (lastHarvestationStarted.HasValue && lastHarvestationStarted.Value < DateTimeOffset.Now.AddHours(-12))
                {
                    isHarvestationCompleted = true;
                    harvestationFailed = false;
                    break;
                }
                else
                {
                    isHarvestationCompleted = false;
                    break;
                }
            }
        }

        Console.WriteLine($"Harvestation is marked as completed: {isHarvestationCompleted}");

        if (!isHarvestationCompleted)
        {
            continue;
        }

        harvestation.HarvestationFailed = harvestationFailed;

        Console.WriteLine($"Harvestation is marked as failed: {harvestationFailed}");

        if (!harvestationFailed)
        {
            Console.WriteLine($"Started: {harvestation.Started:g}");
            Console.WriteLine($"Ended: {harvestation.Ended:g}");
            Console.WriteLine($"Duration: {harvestation.Duration}");

            Pipeline? harvestationPipeline = pipelines.GetPipeline("03 - Harvestace LKOD a registrací");
            Debug.Assert(harvestationPipeline is not null);
            PipelineLog? harvestationPipelineLog = harvestation.GetLog(harvestationPipeline);
            Debug.Assert(harvestationPipelineLog is not null);

            Component? catalogRegistrationsComponent = harvestationPipelineLog.GetComponentByName(harvestationPipeline, "Registrace LKOD");
            Debug.Assert(catalogRegistrationsComponent is not null);

            using Graph? registrations = catalogRegistrationsComponent.GetGraph(harvestationPipelineLog, "FilesOutput");
            Debug.Assert(registrations is not null);

            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Konfigurace stažení LKOD dcat:Catalog")!.GetGraph(harvestationPipelineLog, "OutputRdf"));
            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Stažení LKOD dcat:Catalog")!.GetGraph(harvestationPipelineLog, "ReportRdf"));
            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Konfigurace stažení dcat:Dataset")!.GetGraph(harvestationPipelineLog, "OutputRdf"));
            Component localCatalogDocDatasetComponent = harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Stažení dcat:Dataset")!;
            registrations.Merge(localCatalogDocDatasetComponent.GetGraph(harvestationPipelineLog, "ReportRdf"));

            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Generovat dotazy na datové sady v LKODech řešených SPARQL endpointem")!.GetGraph(harvestationPipelineLog, "OutputRdf"));
            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Vrátit datové sady z LKODů řešených SPARQL endpointem")!.GetGraph(harvestationPipelineLog, "OutputRdf"));
            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Vrátit datové sady z LKODů řešených SPARQL endpointem")!.GetGraph(harvestationPipelineLog, "ErrorOutputRdf"));

            using Graph harvestedDatasetsUsingSparql = harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Harvestace LKODů řešených SPARQL endpointem")!.GetGraph(harvestationPipelineLog, "OutputRdf");

            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Generovat dotazy na datové sady")!.GetGraph(harvestationPipelineLog, "OutputRdf"));
            registrations.Merge(harvestedDatasetsUsingSparql);
            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Harvestace LKODů řešených SPARQL endpointem")!.GetGraph(harvestationPipelineLog, "ErrorOutputRdf"));

            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Konfigurace stažení LKOD package_list")!.GetGraph(harvestationPipelineLog, "OutputRdf"));
            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Stažení LKOD package_list")!.GetGraph(harvestationPipelineLog, "ReportRdf"));
            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Konfigurace stažení package_show")!.GetGraph(harvestationPipelineLog, "OutputRdf"));
            Component localCatalogCkanDatasetComponent = harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Stažení LKOD package_show")!;
            registrations.Merge(localCatalogCkanDatasetComponent.GetGraph(harvestationPipelineLog, "ReportRdf"));
            
            registrations.Merge(harvestationPipelineLog.GetComponentByDescription(harvestationPipeline, "Vytvořit záznamy datových sad z LKODů tvořených CKAN API")!.GetGraph(harvestationPipelineLog, "OutputRdf"));

            using Graph previousHarvestedGraph = new Graph();
            string previousHarvestedFilePath = Path.Combine(outputDir, "harvested.ttl");
            if (File.Exists(previousHarvestedFilePath))
            {
                previousHarvestedGraph.LoadFromFile(previousHarvestedFilePath, new TurtleParser());
            }

            using Graph harvestedGraph = harvestationPipelineLog.GetComponentById(new Uri("https://sk-nkod.opendata.cz/etl/resources/pipelines/16540020101841654002010184165400201018479"))!.GetGraph(harvestationPipelineLog, "FilesInput")!;
            using Graph harvestedLocalCatalogGraph = harvestationPipelineLog.GetComponentById(new Uri("https://sk-nkod.opendata.cz/etl/resources/pipelines/16540020101841654002010184165400201018432"))!.GetGraph(harvestationPipelineLog, "FilesInput")!;
            Dictionary<string, INode> previousIdentifiers = previousHarvestedGraph.GetIdentifierMap();
            Dictionary<string, INode> harvestedIdentifiers = harvestedGraph.GetIdentifierMap();

            HashSet<Triple> previousTriples = [];
            HashSet<Triple> harvestedTriples = [];

            HashSet<Uri> checkDatasets = new HashSet<Uri>();

            DatasetModificationStatus GetDatasetStatus(string datasetId, ISet<INode> previousDatasets)
            {
                previousTriples.Clear();
                harvestedTriples.Clear();

                Uri datasetUri = new Uri(datasetId);
                checkDatasets.Add(datasetUri);

                if (previousIdentifiers.TryGetValue(datasetId, out INode? previousDatasetSubject))
                {
                    previousHarvestedGraph.LoadDatasetTriples(previousDatasetSubject, previousTriples);
                    previousDatasets.Remove(previousDatasetSubject);
                }
                previousDatasetSubject = previousHarvestedGraph.CreateUriNode(datasetUri);
                previousHarvestedGraph.LoadDatasetTriples(previousDatasetSubject, previousTriples);
                previousDatasets.Remove(previousDatasetSubject);

                if (harvestedIdentifiers.TryGetValue(datasetId, out INode? datasetSubject))
                {
                    harvestedGraph.LoadDatasetTriples(datasetSubject, harvestedTriples);
                    
                    if (datasetSubject is IUriNode datasetSubjectUriNode)
                    {
                        checkDatasets.Add(datasetSubjectUriNode.Uri);
                    }
                }
                harvestedGraph.LoadDatasetTriples(harvestedGraph.CreateUriNode(datasetUri), harvestedTriples);

                if (previousTriples.Count > 0)
                {
                    if (harvestedTriples.Count > 0)
                    {
                        if (!harvestedTriples.SetEquals(previousTriples))
                        {
                            return DatasetModificationStatus.Modified;
                        }
                    }
                    else
                    {
                        return DatasetModificationStatus.Removed;
                    }
                }
                else if (harvestedTriples.Count > 0)
                {
                    return DatasetModificationStatus.Added;
                }
                else
                {
                    return DatasetModificationStatus.Empty;
                }

                return DatasetModificationStatus.NotModified;
            }

            foreach (IUriNode catalogNode in registrations.GetSubjectsByType(KnownPredicates.DcatCatalogClass))
            {
                RdfObject catalog = new RdfObject(registrations, catalogNode);
                Uri catalogId = catalogNode.Uri;
                Uri? endpointUrl = catalog.GetUri(KnownPredicates.DcatEndpointUrl);
                bool catalogMainSourceHarvested = false;
                bool catalogAtLeastOneDatasetHarvested = false;
                string catalogName = catalog.GetString(KnownPredicates.DctTitle, "sk") ?? catalog.GetString(KnownPredicates.DctTitle, string.Empty) ?? catalogId.OriginalString;

                if (endpointUrl is not null && catalogId is not null)
                {
                    HarvestationResult result = new HarvestationResult(harvestation, catalogId, "catalog", catalogName);
                    HashSet<INode> previousDatasets = new HashSet<INode>(previousHarvestedGraph.GetTriplesWithSubjectPredicate(previousHarvestedGraph.CreateUriNode(catalogNode.Uri), previousHarvestedGraph.CreateUriNode(KnownPredicates.DcatDataset)).Select(t => t.Object));

                    void AddHarvestationProblem(string datasetId, string message)
                    {
                        HarvestationProblem harvestationProblem = new HarvestationProblem(datasetId, message);
                        result.Problems.Add(harvestationProblem);
                        harvestation.Problems.Add(harvestationProblem);
                    }

                    async Task ValidateDatasetFromLocalCatalogFile(string path, Uri datasetId)
                    {
                        try
                        {
                            if (File.Exists(path))
                            {
                                Graph graph = new Graph();

                                string fileContent = File.ReadAllText(path);

                                List<Exception>? exceptions = null;

                                void AddParsingError(string message)
                                {
                                    AddHarvestationProblem(datasetId.OriginalString, message);
                                }

                                bool LoadTurtleFile()
                                {
                                    try
                                    {
                                        TurtleParser parser = new TurtleParser();
                                        parser.Warning += AddParsingError;
                                        int tripletCountBefore = graph.Triples.Count;
                                        graph.LoadFromString(fileContent, parser);
                                        return graph.Triples.Count > tripletCountBefore;
                                    }
                                    catch (RdfException e)
                                    {
                                        exceptions ??= [];
                                        exceptions.Add(e);
                                        return false;
                                    }
                                    catch
                                    {
                                        return false;
                                    }
                                }

                                bool LoadJsonLdFile()
                                {
                                    try
                                    {
                                        TripleStore store = new TripleStore();
                                        JsonLdParser parser = new JsonLdParser();
                                        parser.Warning += AddParsingError;
                                        store.LoadFromString(fileContent, parser);
                                        bool containsTriplet = false;
                                        foreach (Triple triple in store.Triples)
                                        {
                                            graph.Assert(triple);
                                            containsTriplet = true;
                                        }
                                        return containsTriplet;
                                    }
                                    catch (RdfException e)
                                    {
                                        exceptions ??= [];
                                        exceptions.Add(e);
                                        return false;
                                    }
                                    catch
                                    {
                                        return false;
                                    }
                                }

                                bool isProbablyJson = fileContent.StartsWith('{');

                                if (isProbablyJson)
                                {
                                    if (!LoadJsonLdFile())
                                    {
                                        LoadTurtleFile();
                                    }
                                }
                                else if (!LoadTurtleFile())
                                {
                                    LoadJsonLdFile();
                                }

                                if (graph.Triples.Count > 0)
                                {
                                    await ValidateDatasetFromLocalCatalog(graph, datasetId);
                                }
                                else if (exceptions is not null && exceptions.Count > 0)
                                {
                                    AddHarvestationProblem(datasetId.OriginalString, string.Join("; ", exceptions.Select(e => e.Message)));
                                }
                                else
                                {
                                    AddHarvestationProblem(datasetId.OriginalString, "Pre dataset neboli harvestované žiadne triplety");
                                }
                            }
                        }
                        catch
                        {
                            AddHarvestationProblem(datasetId.OriginalString, "Počas validácie nastal neočakávaný problém");
                        }
                    }

                    async Task ValidateDatasetFromLocalCatalog(Graph sourceGraph, Uri datasetId)
                    {
                        try
                        {
                            if (sourceGraph.GetTriplesWithSubject(sourceGraph.CreateUriNode(datasetId)).Any())
                            {
                                ValidationResults results = await validator.ValidateDataset(sourceGraph, datasetId);
                                foreach (ValidationResult validationResult in results)
                                {
                                    if (validationResult.Level == ValidationErrorLevel.Warning || validationResult.Level == ValidationErrorLevel.Error)
                                    {
                                        AddHarvestationProblem(validationResult.Context.ToString(), validationResult.Message);

                                        validationWriter.NextRecord();
                                        validationWriter.WriteField(validationResult.Context.LastOrDefault(t => t.Predicate is not null)?.Predicate?.ToString() ?? "-");
                                        validationWriter.WriteField(datasetId.OriginalString);
                                        validationWriter.WriteField(validationResult.Context.ToString());
                                        validationWriter.WriteField(validationResult.Message);
                                    }
                                }
                            }
                            else
                            {
                                AddHarvestationProblem(datasetId.OriginalString, "Pre dataset neboli harvestované žiadne triplety");
                            }
                        }
                        catch
                        {
                            AddHarvestationProblem(datasetId.OriginalString, "Počas validácie nastal neočakávaný problém");
                        }
                    }

                    if (catalog.HasType(KnownPredicates.LocalCatalogTypeDcatDocuments))
                    {
                        (Report? report, HttpTask? httpTask) = registrations.GetReportAndHttpTask(catalogNode);
                        if (report is not null)
                        {
                            TimeSpan duration = report.Duration ?? TimeSpan.Zero;
                            bool isSuccess = report.IsSuccess;
                            catalogMainSourceHarvested = isSuccess;
                            result.ExternalSources.Add(report.CreateResult(endpointUrl.OriginalString));

                            result.MainSourceIsAvailable = isSuccess;
                        }

                        string? componentDir = localCatalogDocDatasetComponent.GetAbsolutePath(harvestationPipelineLog, "FilesOutput");
                        List<string> harvestedDatasetDocumentsDirs = !string.IsNullOrEmpty(componentDir) ? PipelineLog.GetReferencedPath(componentDir) : [];

                        foreach (IUriNode datasetNode in catalog.GetUriNodes(KnownPredicates.DcatDataset))
                        {
                            RdfObject dataset = new RdfObject(registrations, datasetNode);

                            (report, httpTask) = registrations.GetReportAndHttpTask(datasetNode);
                            if (report is not null)
                            {
                                TimeSpan duration = report.Duration ?? TimeSpan.Zero;
                                result.ExternalSources.Add(report.CreateResult(datasetNode.Uri.OriginalString));
                                result.ExplicitlyStarted = report.Started;

                                if (report.IsSuccess)
                                {
                                    catalogAtLeastOneDatasetHarvested = true;
                                }
                            }

                            result.AddDatasetStatus(GetDatasetStatus(datasetNode.Uri.OriginalString, previousDatasets));

                            if (!string.IsNullOrEmpty(httpTask?.FileName))
                            {
                                foreach (string d in harvestedDatasetDocumentsDirs)
                                {
                                    string path = Path.Combine(d, httpTask.FileName);
                                    if (File.Exists(path))
                                    {
                                        await ValidateDatasetFromLocalCatalogFile(path, datasetNode.Uri);
                                    }
                                }
                            }
                        }
                    }
                    else if (catalog.HasType(KnownPredicates.LocalCatalogTypeSparql))
                    {
                        (Report? report, SparqlTask? httpTask) = registrations.GetReportAndSparqlTask(catalogNode);
                        if (report is not null)
                        {
                            TimeSpan duration = report.Duration ?? TimeSpan.Zero;
                            bool isSuccess = report.IsSuccess;
                            catalogMainSourceHarvested = isSuccess;
                            result.ExternalSources.Add(report.CreateResult($"SPARQL (zoznam datasetov) {endpointUrl.OriginalString}"));

                            result.MainSourceIsAvailable = isSuccess;
                            result.ExplicitlyStarted = report.Started;
                        }

                        foreach (IUriNode datasetNode in catalog.GetUriNodes(KnownPredicates.DcatDataset))
                        {
                            result.AddDatasetStatus(GetDatasetStatus(datasetNode.Uri.OriginalString, previousDatasets));
                            await ValidateDatasetFromLocalCatalog(harvestedDatasetsUsingSparql, datasetNode.Uri);

                            catalogAtLeastOneDatasetHarvested = true;
                        }
                    }
                    else if (catalog.HasType(KnownPredicates.LocalCatalogTypeCkan))
                    {
                        (Report? report, HttpTask? httpTask) = registrations.GetReportAndHttpTask(catalogNode);
                        if (report is not null)
                        {
                            TimeSpan duration = report.Duration ?? TimeSpan.Zero;
                            bool isSuccess = report.IsSuccess;
                            catalogMainSourceHarvested = isSuccess;
                            result.ExternalSources.Add(report.CreateResult(endpointUrl.OriginalString));

                            result.MainSourceIsAvailable = isSuccess;
                            result.ExplicitlyStarted = report.Started;
                        }

                        IUriNode registrationCatalogPredicate = registrations.CreateUriNode(new Uri("https://data.gov.sk/slovník/nkod/catalog"));
                        IUriNode sourceNode = registrations.CreateUriNode(KnownPredicates.DctSource);

                        foreach (IUriNode datasetReportNode in registrations.GetTriplesWithPredicateObject(registrationCatalogPredicate, catalogNode).Select(t => t.Subject).OfType<IUriNode>())
                        {
                            if (datasetReportNode.Uri.OriginalString != catalogNode.Uri.OriginalString)
                            {
                                (report, httpTask) = registrations.GetReportAndHttpTask(datasetReportNode);
                                if (report is not null && httpTask is not null)
                                {
                                    Uri? sourceUrl = httpTask?.FileUri;
                                    if (sourceUrl is not null)
                                    {
                                        if (report.IsSuccess)
                                        {
                                            catalogAtLeastOneDatasetHarvested = true;
                                        }

                                        TimeSpan duration = report.Duration ?? TimeSpan.Zero;
                                        result.ExternalSources.Add(report.CreateResult(datasetReportNode.Uri.OriginalString));
                                        IUriNode sourceUrlNode = registrations.CreateUriNode(sourceUrl);
                                        foreach (IUriNode catalogRecordNode in registrations.GetTriplesWithPredicateObject(sourceNode, sourceUrlNode).Select(t => t.Subject))
                                        {
                                            RdfObject catalogRecord = new RdfObject(registrations, catalogRecordNode);
                                            if (catalogRecord.HasType(KnownPredicates.DcatCatalogRecord))
                                            {
                                                IUriNode? datasetNode = catalogRecord.GetUriNode(KnownPredicates.FoafPrimaryTopic);
                                                if (datasetNode is not null)
                                                { 
                                                    result.AddDatasetStatus(GetDatasetStatus(datasetNode.Uri.OriginalString, previousDatasets)); 
                                                }

                                                break;
                                            }
                                        }
                                    }
                                }                                
                            }
                        }
                    }

                    result.RemovedDatasets += previousDatasets.Count;

                    resultByCatalogs[catalogId] = result;

                    if (catalog.GetUri(KnownPredicates.DctPublisher) is Uri publisher)
                    {
                        AddResultForPublisher(publisher, result);
                        harvestation.Add(result);
                    }
                    harvestation.ExternalSources.AddRange(result.ExternalSources);
                    harvestation.CatalogSources.AddRange(result.ExternalSources);
                    harvestation.CatalogCount++;
                    if (catalogMainSourceHarvested && catalogAtLeastOneDatasetHarvested)
                    {
                        harvestation.CorrectlyHarvestedCatalogCount++;
                    }
                }
            }

            Dictionary<string, DownloadQualityExternalSource> downloadQualityExternalSources = [];
            SparqlResultSet results = (SparqlResultSet)SparqlHelper.Query(harvestedGraph, @"PREFIX dct: <http://purl.org/dc/terms/>
PREFIX dcat: <http://www.w3.org/ns/dcat#>

SELECT ?publisher ?dataset ?download
WHERE {
  ?dataset a dcat:Dataset;
             dct:publisher ?publisher ;
             dcat:distribution ?distribution .
  OPTIONAL { ?distribution dcat:downloadURL ?download }
}");

            Dictionary<Uri, HashSet<Uri>> datasetsByPublisher = [];

            foreach (ISparqlResult result in results)
            {
                Uri publisher = ((IUriNode)result["publisher"]).Uri;
                Uri dataset = ((IUriNode)result["dataset"]).Uri;
                
                if (checkDatasets.Contains(dataset))
                {
                    string? distribution = result.TryGetBoundValue("download", out INode? downloadNode) ? downloadNode.AsValuedNode().AsString() : null;

                    if (distribution is not null)
                    {
                        downloadQualityExternalSources[distribution] = new DownloadQualityExternalSource
                        {
                            Publisher = publisher,
                            Dataset = dataset,
                            Distribution = distribution
                        };
                    }

                    if (!datasetsByPublisher.TryGetValue(publisher, out HashSet<Uri>? datasets))
                    {
                        datasets = [];
                        datasetsByPublisher[publisher] = datasets;
                    }
                    datasets.Add(dataset);
                }
            }

            //foreach ((Uri publisher, HashSet<Uri> datasets) in datasetsByPublisher)
            //{
            //    HarvestationResult publisherResult = GetOrAddResultForPublisher(publisher);
            //    HashSet<INode> previousDatasets = new HashSet<INode>(previousHarvestedGraph.GetTriplesWithPredicateObject(previousHarvestedGraph.CreateUriNode(KnownPredicates.DctPublisher), previousHarvestedGraph.CreateUriNode(publisher)).Select(t => t.Subject));
            //    foreach (Uri dataset in datasets)
            //    {
            //        DatasetModificationStatus status = GetDatasetStatus(dataset.OriginalString, previousDatasets);
            //        publisherResult.AddDatasetStatus(status);
            //        harvestation.AddDatasetStatus(status);
            //    }
            //    publisherResult.RemovedDatasets += previousDatasets.Count;
            //    harvestation.RemovedDatasets += previousDatasets.Count;
            //}

            Pipeline? headQualityPipeline = pipelines.GetPipeline("04 - Statistika dostupnosti distribucí, schémat, podmínek užití a dokumentace - HEAD");
            Debug.Assert(headQualityPipeline is not null);
            PipelineLog? headQualityPipelineLog = harvestation.GetLog(headQualityPipeline);
            Debug.Assert(headQualityPipelineLog is not null);

            using Graph qualityGraph = headQualityPipelineLog.GetComponentByDescription(headQualityPipeline, "HEAD distributions")!.GetGraph(headQualityPipelineLog, "ReportRdf");

            results = (SparqlResultSet)SparqlHelper.Query(qualityGraph, @"PREFIX dct: <http://purl.org/dc/terms/>
PREFIX schema: <http://schema.org/>
PREFIX dcat: <http://www.w3.org/ns/dcat#>
PREFIX dqv: <http://www.w3.org/ns/dqv#>
PREFIX q: <https://data.gov.sk/def/observation/data-quality/metrics/>
PREFIX skos: <http://www.w3.org/2004/02/skos/core#>
PREFIX report: <https://vocabulary.etl.linkedpipes.com/report/>

SELECT ?download ?success ?error ?duration
WHERE {
  ?report a report:Report ;
            report:task ?download ;
            report:duration ?duration .
  OPTIONAL {
    ?report  report:status report:Failed ;
             report:exception ?exception .
    
    ?exception report:message ?error.
  }

  BIND(!BOUND(?error) as ?success)
}
");
            foreach (ISparqlResult result in results)
            {
                Uri distribution = ((IUriNode)result["download"]).Uri;
                if (downloadQualityExternalSources.TryGetValue(distribution.OriginalString, out DownloadQualityExternalSource? source))
                {
                    source.Success = result["success"].EffectiveBooleanValue();
                    source.Duration = TimeSpan.FromMilliseconds(result["duration"].AsValuedNode().AsInteger());
                    source.Note = result.TryGetBoundValue("error", out INode errorNode) ? errorNode.AsValuedNode().AsString() : null;
                }
            }

            foreach (DownloadQualityExternalSource source in downloadQualityExternalSources.Values)
            {
                if (!source.Success)
                {
                    GetOrAddResultForPublisher(source.Publisher).ExternalSources.Add(source);
                    harvestation.ExternalSources.Add(source);
                    harvestation.DistributionSources.Add(source);
                }
            }

            harvestation.PublisherCount = resultByPublishers.Count;

            Console.WriteLine($"Sources: {harvestation.ExternalSourcesCount}");
            Console.WriteLine($"Ratio: {harvestation.SuccessRatio * 100:N1}%");
            Console.WriteLine($"Sources Duration: {harvestation.ExternalSourcesDuration}");
            Console.WriteLine($"Sources Avg Duration: {harvestation.ExternalSourcesAverageDuration}");
            Console.WriteLine($"Datasets count: {harvestation.DatasetsCount}");
            Console.WriteLine($"Total catalog count: {harvestation.CatalogCount}");
            Console.WriteLine($"Correctly harvested catalog count: {harvestation.CorrectlyHarvestedCatalogCount}");
            Console.WriteLine($"Unaviaable catalog sources count: {harvestation.UnavaiableCatalogSourcesCount}");
            Console.WriteLine($"Unaviaable distribution sources count: {harvestation.UnavailableDistributionSourcesCount}");
            Console.WriteLine($"New datasets: {harvestation.NewDatasets}");
            Console.WriteLine($"Modified datasets: {harvestation.ModifiedDatasets}");
            Console.WriteLine($"Removed datasets: {harvestation.RemovedDatasets}");

            foreach ((Uri catalogId, HarvestationResult result) in resultByCatalogs)
            {
                Console.WriteLine();
                Console.WriteLine($"Catalog: {catalogId}");
                Console.WriteLine($"Invalid sources count: {result.InvalidExternalSourcesCount}");
                Console.WriteLine($"Average fetching sources duration: {result.ExternalSourcesAverageDuration}");
                Console.WriteLine($"Total fetching sources duration: {result.ExternalSourcesDuration}");
                Console.WriteLine($"Datasets count: {result.DatasetsCount}");
                Console.WriteLine($"New datasets: {result.NewDatasets}");
                Console.WriteLine($"Modified datasets: {result.ModifiedDatasets}");
                Console.WriteLine($"Removed datasets: {result.RemovedDatasets}");
            }

            foreach ((Uri publisherId, HarvestationResult result) in resultByPublishers)
            {
                Console.WriteLine();
                Console.WriteLine($"Publisher: {publisherId}");
                Console.WriteLine($"Invalid sources count: {result.InvalidExternalSourcesCount}");
                Console.WriteLine($"Datasets count: {result.DatasetsCount}");
                Console.WriteLine($"New datasets: {result.NewDatasets}");
                Console.WriteLine($"Modified datasets: {result.ModifiedDatasets}");
                Console.WriteLine($"Removed datasets: {result.RemovedDatasets}");
            }

            INode hasSubResult = outputGraph.CreateUriNode(Harvestation.GetPredicate("hasSubResult"));
            foreach (HarvestationResult result in resultByCatalogs.Values)
            {
                result.WriteTriples(outputGraph);
                outputGraph.Assert(harvestation.IdentifierNode, hasSubResult, result.IdentifierNode);
            }
            foreach (HarvestationResult result in resultByPublishers.Values)
            {
                result.WriteTriples(outputGraph);
                outputGraph.Assert(harvestation.IdentifierNode, hasSubResult, result.IdentifierNode);
            }

            harvestedGraph.Assert(harvestedLocalCatalogGraph.Triples);

            harvestedGraph.SaveToFile(previousHarvestedFilePath, new CompressingTurtleWriter());

        }

        harvestation.WriteTriples(outputGraph);


        int replaced = MonitoringCheck.Extensions.SanitizeGraph(outputGraph);
        if (replaced > 0)
        {
            Console.WriteLine($"Replaced {replaced} literals with sanitized versions");
        }

        TripleStore outputStore = new TripleStore();
        outputStore.Add(outputGraph);
        outputStore.SaveToFile(outputFilePath, new TriGWriter());

        if (Uri.TryCreate(options?.GraphDbUrl, UriKind.Absolute, out Uri? serverUri))
        {
            SparqlHttpProtocolConnector sparql = new SparqlHttpProtocolConnector(serverUri);

            if (!string.IsNullOrEmpty(options.GraphDbUserName) && !string.IsNullOrEmpty(options.GraphdbPassword))
            {
                sparql.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.GraphDbUserName}:{options.GraphdbPassword}")));
            }
            sparql.Timeout = (int)TimeSpan.FromHours(1).TotalMilliseconds;
            sparql.UpdateGraph(harvestation.IdentifierNode.Uri.OriginalString, outputGraph.Triples, []);
        }

        Console.WriteLine();
        Console.WriteLine("Notifikácie");

        foreach (MonitoringNotificationSettings notificationSetting in notificationSettings)
        {
            Dictionary<string, HarvestationResult> resultsToNotify = [];
            
            if (notificationSetting.UseForLocalCatalog is not null)
            {
                if (Uri.TryCreate(notificationSetting.UseForLocalCatalog, UriKind.Absolute, out Uri? catalogUri) && resultByCatalogs.TryGetValue(catalogUri, out HarvestationResult? result))
                {
                    resultsToNotify[catalogUri.OriginalString] = result;
                }
            }
            else
            {
                foreach ((Uri catalogUri, HarvestationResult? result) in resultByCatalogs)
                {
                    if (notificationSetting.IgnoreLocalCatalogs is null || !notificationSetting.IgnoreLocalCatalogs.Contains(catalogUri.OriginalString))
                    {
                        resultsToNotify[catalogUri.OriginalString] = result;
                    }
                }
            }

            foreach (string key in resultsToNotify.Keys.ToList())
            {
                HarvestationResult result = resultsToNotify[key];

                bool shouldBeIncluded = notificationSetting.Always ||
                    (result.AllDetectedProblems.Any() && notificationSetting.NotifyIfLocalCatalogIsUnavaiable) ||
                    (result.SuccessRatio < .9f && notificationSetting.SuccessRatioIsLow) ||
                    (!result.MainSourceIsAvailable && notificationSetting.NotifyIfLocalCatalogIsUnavaiable) ||
                    (harvestationFailed && notificationSetting.HarvestationFailed);

                if (!shouldBeIncluded)
                {
                    resultsToNotify.Remove(key);
                }
            }

            Dictionary<string, List<HarvestationProblem>> allProblems = [];
            List<string> errors = [];

            if (resultsToNotify.Count > 0)
            {
                if (harvestationFailed)
                {
                    errors.Add("Harvestácia neprebehla");
                }

                foreach (HarvestationResult result in resultsToNotify.Values)
                {
                    string name = result.Name;
                    List<HarvestationProblem> problems = result.AllDetectedProblems.ToList();
                    if (problems.Count > 0)
                    {
                        allProblems[name] = problems;
                    }

                    int percentage = (int)Math.Floor(result.SuccessRatio * 100);
                    if (percentage < 100)
                    {
                        errors.Add($"{name}: úspešnosť získania zdrojov bola {percentage}%");
                    }

                    if (problems.Count > 0)
                    {
                        errors.Add($"{name}: boli objavené problémy (podrobnosti v prílohe)");
                    }

                    if (!result.MainSourceIsAvailable)
                    {
                        errors.Add($"{name}: katalóg nebol dostupný");
                    }
                }
            }

            if (notificationSetting.Always || errors.Count > 0)
            {
                StringBuilder messageBuilder = new StringBuilder("Dobrý deň,<br><br>");

                if (resultsToNotify.Count == 1)
                {
                    HarvestationResult? oneResult = resultsToNotify.Values.FirstOrDefault();
                    DateTimeOffset? started = oneResult?.ExplicitlyStarted ?? harvestation.Started;
                    messageBuilder.Append($"harvestácia lokálneho katalógu {HttpUtility.HtmlEncode(oneResult?.Name)}");
                    if (started.HasValue)
                    {
                        messageBuilder.Append($" v čase od {started.Value.ToString("dd. MM. yyyy HH:mm")}");
                    }
                } 
                else
                {
                    messageBuilder.Append("harvestácia lokálnych katalógov");
                    if (harvestation.Started.HasValue)
                    {
                        messageBuilder.Append($" v čase od {harvestation.Started.Value.ToString("dd. MM. yyyy HH:mm")}");
                    }
                }

                messageBuilder.Append(' ');
                
                if (errors.Count > 0)
                {
                    messageBuilder.Append("- problémy:");
                    foreach (string error in errors)
                    {
                        messageBuilder.AppendLine("<br>");
                        messageBuilder.Append(HttpUtility.HtmlEncode(error));
                    }
                }
                else
                {                   
                    messageBuilder.AppendLine("prebehla úspešne.");
                }

                messageBuilder.AppendLine("<br>");
                messageBuilder.AppendLine("<br>");
                messageBuilder.AppendLine("Zasielanie e-mailov môžete zrušiť v nastaveniach lokálneho katalógu.");
                messageBuilder.AppendLine("<br>");
                messageBuilder.AppendLine("<br>");
                messageBuilder.AppendLine("Tím centrálneho portálu otvorených dát data.slovensko.sk");

                string message = messageBuilder.ToString();
                string? attachment = null;

                if (allProblems.Count > 0)
                {
                    using System.IO.StringWriter attachmentWriter = new System.IO.StringWriter();
                    using CsvHelper.CsvWriter csv = new CsvHelper.CsvWriter(attachmentWriter, new CsvHelper.Configuration.CsvConfiguration(new System.Globalization.CultureInfo("sk-SK")) { Delimiter = ";" });

                    csv.WriteField("Katalóg");
                    csv.WriteField("Názov");
                    csv.WriteField("Poznámka");

                    int count = 0;

                    foreach ((string name, List<HarvestationProblem> problems) in allProblems)
                    {
                        foreach (HarvestationProblem problem in problems)
                        {
                            csv.NextRecord();
                            csv.WriteField(name);
                            csv.WriteField(problem.Name);
                            csv.WriteField(problem.Note);

                            count++;
                            if (count >= 1000)
                            {
                                break;
                            }
                        }

                        if (count >= 1000)
                        {
                            break;
                        }
                    }
                    csv.Flush();

                    attachment = attachmentWriter.ToString();
                }

                Console.WriteLine();
                Console.WriteLine("Settings:");
                Console.WriteLine(JsonSerializer.Serialize(notificationSetting));
                Console.WriteLine("Message:");
                Console.WriteLine(message);
                Console.WriteLine($"Attachment size: {attachment?.Length ?? 0}");

                string[] recipients = notificationSetting.EmailAddresses?.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

                if (!string.IsNullOrEmpty(emailOptions?.Host) && !string.IsNullOrEmpty(emailOptions?.FromAddress) && recipients.Length > 0)
                {
                    foreach (string recipient in recipients)
                    {
                        try
                        {
                            Console.WriteLine($"Odosielam email na adresu {recipient}");

                            using SmtpClient client = new SmtpClient();
                            await client.ConnectAsync(emailOptions.Host, emailOptions.Port ?? 25, emailOptions.UseSsl);
                            if (!string.IsNullOrEmpty(emailOptions.Username) && !string.IsNullOrEmpty(emailOptions.Password))
                            {
                                await client.AuthenticateAsync(emailOptions.Username, emailOptions.Password);
                            }

                            using MimeMessage emailMessage = new MimeMessage();
                            emailMessage.From.Add(new MailboxAddress(emailOptions.FromName, emailOptions.FromAddress));
                            emailMessage.To.Add(new MailboxAddress(string.Empty, recipient));
                            emailMessage.Subject = "Harvestácia LKOD na data.slovensko.sk";

                            if (!string.IsNullOrEmpty(attachment))
                            {
                                emailMessage.Body = new Multipart("mixed")
                                {
                                    new TextPart("html") { Text = message },
                                    new MimePart("text", "csv")
                                    {
                                        Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes(attachment))),
                                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment) { FileName = "export.csv" },
                                        ContentTransferEncoding = ContentEncoding.Base64
                                    }
                                };
                            }
                            else
                            {
                                emailMessage.Body = new TextPart("html") { Text = message };
                            }

                            await client.SendAsync(emailMessage);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Exception: {e.Message}");
                        }
                    }
                }
            }
        }
    }
}

