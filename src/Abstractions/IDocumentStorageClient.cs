﻿using System;
using System.Collections.Generic;
using System.Linq;
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

        async Task UpdateDatasetMetadata(Guid datasetId, bool updateDatasetModifiedDate)
        {
            FileState? datasetState = await GetFileState(datasetId).ConfigureAwait(false);
            if (datasetState?.Content is not null)
            {
                DcatDataset? dataset = DcatDataset.Parse(datasetState.Content);

                if (dataset is not null)
                {
                    if (updateDatasetModifiedDate)
                    {
                        dataset.Modified = DateTime.UtcNow;
                    }
                    FileMetadata datasetMetadata = datasetState.Metadata;

                    FileStorageQuery query = new FileStorageQuery
                    {
                        ParentFile = datasetId,
                        OnlyTypes = new List<FileType> { FileType.DistributionRegistration },
                    };
                    FileStorageResponse response = await GetFileStates(query).ConfigureAwait(false);

                    datasetMetadata = dataset.UpdateMetadata(response.Files.Count > 0, datasetMetadata);
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
                                distribution.IncludeInDataset(dataset);
                            }
                        }
                    }

                    await InsertFile(dataset.ToString(), true, datasetMetadata).ConfigureAwait(false);
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
    }
}
