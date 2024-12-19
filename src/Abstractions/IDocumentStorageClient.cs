using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Query.Paths;

namespace NkodSk.Abstractions
{
    public interface IDocumentStorageClient
    {
        Task<FileState?> GetFileState(Guid id);

        Task<FileStorageResponse> GetFileStates(FileStorageQuery query);

        Task<FileMetadata?> GetFileMetadata(Guid id);

        Task<FileStorageGroupResponse> GetFileStatesByPublisher(FileStorageQuery query);

        Task<Stream?> DownloadStream(Guid id);

        Task UploadStream(Stream source, FileMetadata metadata, bool enableOverwrite);

        Task InsertFile(string content, bool enableOverwrite, FileMetadata metadata);

        Task UpdateMetadata(FileMetadata metadata);

        Task DeleteFile(Guid id);

        Task<long?> GetSize(Guid id);

        async Task UpdatePublisherDatasets(string publisherId, bool updateDatasetModifiedDate)
        {
            FileState? publisherState = await GetPublisherFileState(publisherId).ConfigureAwait(false);
            if (publisherState?.Content is null)
            {
                return;
            }

            FoafAgent? publisher = FoafAgent.Parse(publisherState.Content);
            if (publisher is null)
            {
                return;
            }

            FileStorageQuery query = new FileStorageQuery
            {
                OnlyTypes = new List<FileType> { FileType.DatasetRegistration },
                OnlyPublishers = new List<string> { publisherId },
            };
            FileStorageResponse response = await GetFileStates(query).ConfigureAwait(false);
            foreach (FileState datasetState in response.Files)
            {
                await UpdateDatasetMetadata(datasetState, publisher, updateDatasetModifiedDate).ConfigureAwait(false);
            }
        }

        
        private async Task UpdateDatasetMetadata(FileState datasetState, FoafAgent publisher, bool updateDatasetModifiedDate)
        {
            DcatDataset? dataset = datasetState.Content is not null ? DcatDataset.Parse(datasetState.Content) : null;

            if (dataset?.Publisher is not null)
            {
                if (updateDatasetModifiedDate)
                {
                    dataset.Modified = DateTime.UtcNow;
                }
                FileMetadata datasetMetadata = datasetState.Metadata;

                FileStorageQuery query = new FileStorageQuery
                {
                    ParentFile = datasetMetadata.Id,
                    OnlyTypes = new List<FileType> { FileType.DistributionRegistration },
                };
                FileStorageResponse response = await GetFileStates(query).ConfigureAwait(false);

                datasetMetadata = dataset.UpdateMetadata(response.Files.Count > 0 || dataset.IsSerie, publisher, datasetMetadata);
                datasetMetadata = DcatDistribution.ClearDatasetMetadata(datasetMetadata);

                dataset.RemoveAllDistributions();

                foreach (FileState state in response.Files)
                {
                    if (state.Content is not null)
                    {
                        DcatDistribution? distribution = DcatDistribution.Parse(state.Content);
                        if (distribution is not null)
                        {
                            datasetMetadata = distribution.UpdateDatasetMetadata(datasetMetadata);

                            Uri hvdLegislation = new Uri(DcatDataset.HvdLegislation);

                            if (dataset.IsHvd)
                            {
                                void AddLegislationIfNeeded(IEnumerable<Uri> original, Action<IEnumerable<Uri>> setter)
                                {
                                    List<Uri> list = new List<Uri>(original);
                                    if (!list.Any(u => string.Equals(u.OriginalString, hvdLegislation.OriginalString, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        list.Add(hvdLegislation);
                                        setter(list);
                                    }
                                }

                                AddLegislationIfNeeded(distribution.ApplicableLegislations, l => distribution.ApplicableLegislations = l);

                                if (distribution.DataService is DcatDataService dataService)
                                {
                                    AddLegislationIfNeeded(dataService.ApplicableLegislations, l => dataService.ApplicableLegislations = l);
                                    dataService.HvdCategory = dataService.HvdCategory;
                                }
                            }
                            else
                            {
                                void RemoveLegislationIfNeeded(IEnumerable<Uri> original, Action<IEnumerable<Uri>> setter)
                                {
                                    if (original.Any(u => string.Equals(u.OriginalString, hvdLegislation.OriginalString, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        List<Uri> list = new List<Uri>(original);
                                        list.RemoveAll(u => string.Equals(u.OriginalString, hvdLegislation.OriginalString, StringComparison.OrdinalIgnoreCase));
                                        setter(list);
                                    }
                                }

                                RemoveLegislationIfNeeded(distribution.ApplicableLegislations, l => distribution.ApplicableLegislations = l);

                                if (distribution.DataService is DcatDataService dataService)
                                {
                                    RemoveLegislationIfNeeded(dataService.ApplicableLegislations, l => dataService.ApplicableLegislations = l);
                                    dataService.HvdCategory = dataService.HvdCategory;
                                }
                            }

                            string content = distribution.ToString();
                            if (!string.Equals(state.Content, content, StringComparison.Ordinal))
                            {
                                await InsertFile(content, true, distribution.UpdateMetadata(datasetMetadata, state.Metadata));
                            }

                            distribution.IncludeInDataset(dataset);
                        }
                    }
                }

                await InsertFile(dataset.ToString(), true, datasetMetadata).ConfigureAwait(false);
            }              
        }

        async Task UpdateDatasetMetadata(Guid datasetId, bool updateDatasetModifiedDate)
        {
            FileState? datasetState = await GetFileState(datasetId).ConfigureAwait(false);
            if (datasetState?.Content is not null)
            {
                DcatDataset? dataset = DcatDataset.Parse(datasetState.Content);

                if (dataset?.Publisher is not null)
                {
                    FileState? publisherState = await GetPublisherFileState(dataset.Publisher.ToString()).ConfigureAwait(false);
                    if (publisherState?.Content is null)
                    {
                        return;
                    }

                    FoafAgent? publisher = FoafAgent.Parse(publisherState.Content);
                    if (publisher is null)
                    {
                        return;
                    }

                    await UpdateDatasetMetadata(datasetState, publisher, updateDatasetModifiedDate).ConfigureAwait(false);
                }
            }
        }

        public async Task<Guid?> GetParentDataset(Guid datasetId)
        {
            FileState? state = await GetFileState(datasetId).ConfigureAwait(false);
            if (state?.Content is not null && state.Metadata.Type == FileType.DatasetRegistration)
            {
                DcatDataset? dataset = DcatDataset.Parse(state.Content);
                if (dataset is not null && Guid.TryParse(dataset.IsPartOfInternalId, out Guid parentId))
                {
                    return parentId;
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<Guid>> GetDatasetParts(Guid datasetId)
        {
            FileStorageQuery query = new FileStorageQuery
            {
                ParentFile = datasetId,
                OnlyTypes = new List<FileType> { FileType.DatasetRegistration },
            };
            FileStorageResponse response = await GetFileStates(query).ConfigureAwait(false);
            List<Guid> ids = new List<Guid>(response.Files.Count);
            foreach (FileState state in response.Files)
            {
                ids.Add(state.Metadata.Id);
            }
            return ids;
        }

        public async Task<bool> CanBeAddedToSerie(Guid directParentId, Guid partId)
        {
            HashSet<Guid> visited = new HashSet<Guid>();
            Guid parentId = directParentId;
            while (true)
            {
                if (parentId == partId)
                {
                    return false;
                }

                if (visited.Add(parentId))
                {
                    Guid? newParentId = await GetParentDataset(parentId).ConfigureAwait(false);
                    if (newParentId.HasValue)
                    {
                        parentId = newParentId.Value;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<FileState?> GetPublisherFileState(string publisherId)
        {
            FileStorageQuery storageQuery = new FileStorageQuery
            {
                OnlyTypes = new List<FileType> { FileType.PublisherRegistration },
                OnlyPublishers = new List<string> { publisherId },
                MaxResults = 1
            };
            FileStorageResponse response = await GetFileStates(storageQuery).ConfigureAwait(false);
            return response.Files.Count > 0 ? response.Files[0] : null;
        }
    }
}
