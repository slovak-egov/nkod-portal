using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class HarvestedDataImport
    {
        private readonly ISparqlClient sparqlClient;

        private readonly IDocumentStorageClient documentStorageClient;

        private readonly Func<string?, Task> loginProvider;

        private readonly Action<string> logger;

        public HarvestedDataImport(ISparqlClient sparqlClient, IDocumentStorageClient documentStorageClient, Func<string?, Task> loginProvider, Action<string> logger)
        {
            this.sparqlClient = sparqlClient;
            this.documentStorageClient = documentStorageClient;
            this.loginProvider = loginProvider;
            this.logger = logger;
        }

        public async Task Import()
        { 
            async Task UpdateChildren<T>(FileType type, FileMetadata parentMetadata, bool removeChildren, Func<string, T?> parser, Func<Task<List<T>>> fetcher, Func<T, FileMetadata?, FileMetadata> metadataCreator, Func<Task> savedCallback, Func<FileMetadata, T, Task> innerUpdate) where T : RdfObject
            {
                logger("Loading existing children from storage");

                FileStorageQuery query = new FileStorageQuery()
                {
                    OnlyTypes = new List<FileType>() { type },
                    ParentFile = parentMetadata.Id,
                    AdditionalFilters = new Dictionary<string, string[]>()
                    {
                        { "Harvested", new[]{ "true" } },
                    },
                };

                FileStorageResponse response = await documentStorageClient.GetFileStates(query);

                logger($"Total children loaded: {response.Files.Count}");

                Dictionary<string, (FileState, T)> objectsById = new Dictionary<string, (FileState, T)>();
                foreach (FileState datasetState in response.Files)
                {
                    logger("Existing child: " + datasetState.Metadata.Id);

                    T? rdfObject = datasetState.Content is not null ? parser(datasetState.Content) : null;
                    if (rdfObject is not null)
                    {
                        logger($"Existing child id: {rdfObject.Uri}");
                        objectsById[rdfObject.Uri.ToString()] = (datasetState, rdfObject);
                    }
                }

                logger($"All children should be removed: {removeChildren}");
                logger($"Parent is public: {parentMetadata.IsPublic}");

                try
                {
                    if (!removeChildren && parentMetadata.IsPublic)
                    {
                        logger("Fetching children from sparql endpoint");

                        List<T> rdfObjects = await fetcher();

                        logger($"Total children fetched: {rdfObjects.Count}");

                        foreach (T rdfObject in rdfObjects)
                        {
                            logger("Processing child: " + rdfObject.Uri);

                            rdfObject.IsHarvested = true;

                            FileMetadata? newMetadata = null;
                            string newContent = rdfObject.ToString();
                            string key = rdfObject.Uri.ToString();

                            if (objectsById.TryGetValue(key, out (FileState State, T RdfObject) existing))
                            {
                                if (!string.Equals(newContent, existing.State.Content, StringComparison.Ordinal))
                                {
                                    newMetadata = metadataCreator(rdfObject, existing.State.Metadata);
                                    logger("Updating child");
                                }
                                else
                                {
                                    logger("Child does not need to update");
                                }

                                objectsById.Remove(key);
                            }
                            else
                            {
                                newMetadata = metadataCreator(rdfObject, null);

                                logger("Creating child");
                            }

                            if (newMetadata is not null)
                            {                                
                                newMetadata = newMetadata with { ParentFile = parentMetadata.Id };

                                logger("Saving child");
                                logger("Metadata:");
                                logger(JsonConvert.SerializeObject(newMetadata, Formatting.Indented));
                                logger("Content:");
                                logger(newContent);

                                await documentStorageClient.InsertFile(newContent, true, newMetadata);
                                await savedCallback();

                                logger("Child saved");
                            }

                            logger("Updating inner children");

                            await innerUpdate(newMetadata ?? existing.State.Metadata, rdfObject);

                            logger("Inner children updated");
                        }
                    }
                }
                finally
                {
                    foreach ((FileState existingState, _) in objectsById.Values)
                    {
                        logger($"Removing child: {existingState.Metadata.Id}");

                        await documentStorageClient.DeleteFile(existingState.Metadata.Id);

                        logger("Child removed");
                    }
                }
            }

            logger("Acquiring token for harvester (without publisher)");

            await loginProvider(null);

            logger("Fetching local catalogs");

            FileStorageQuery catalogQuery = new FileStorageQuery()
            {
                OnlyTypes = new List<FileType>() { FileType.LocalCatalogRegistration },
            };
            FileStorageResponse response = await documentStorageClient.GetFileStates(catalogQuery);

            logger($"Total catalogs fetched: {response.Files.Count}");

            foreach (FileState state in response.Files)
            {
                logger("Loading catalog: " + state.Metadata.Id);

                try
                {
                    logger($"Acquiring token for harvester (publisher {state.Metadata.Publisher})");

                    await loginProvider(state.Metadata.Publisher);

                    DcatCatalog? catalog = state.Content is not null ? DcatCatalog.Parse(state.Content) : null;

                    logger($"Catalog parsed: {catalog is not null}");
                    logger($"Catalog uri: {catalog?.Uri}");

                    await UpdateChildren(
                        FileType.DatasetRegistration,
                        state.Metadata,
                        catalog is null,
                        DcatDataset.Parse,
                        () => catalog?.Uri is not null ? sparqlClient.GetDatasets(catalog.Uri) : Task.FromResult(new List<DcatDataset>()),
                        (d, m) =>
                        {
                            m = d.UpdateMetadata(true, m);
                            Dictionary<string, string[]> additionalValues = m.AdditionalValues ?? new Dictionary<string, string[]>();
                            if (catalog is not null)
                            {
                                additionalValues["localCatalog"] = new[] { state.Metadata.Id.ToString() };
                            }
                            m = m with { AdditionalValues = additionalValues };
                            return m;
                        },
                        () => Task.CompletedTask,
                        async (datasetMetadata, dataset) =>
                        {
                            await UpdateChildren(
                                FileType.DistributionRegistration,
                                datasetMetadata,
                                false,
                                DcatDistribution.Parse,
                                () => sparqlClient.GetDistributions(dataset.Uri),
                                (d, m) => d.UpdateMetadata(datasetMetadata, m),
                                () => documentStorageClient.UpdateDatasetMetadata(datasetMetadata.Id),
                                (_, _) => Task.CompletedTask
                            );
                        }
                    );
                }
                catch (Exception e)
                {
                    logger(e.Message);
                    logger($"Error during local catalog: {state.Metadata.Id}");
                }
            }
        }
    }
}
