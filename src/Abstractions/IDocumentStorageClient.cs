using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        async Task UpdateDatasetMetadata(Guid datasetId)
        {
            FileState? datasetState = await GetFileState(datasetId).ConfigureAwait(false);
            if (datasetState?.Content is not null)
            {
                DcatDataset? dataset = DcatDataset.Parse(datasetState.Content);

                if (dataset is not null)
                {
                    FileMetadata datasetMetadata = datasetState.Metadata;

                    FileStorageQuery query = new FileStorageQuery
                    {
                        ParentFile = datasetId,
                        OnlyTypes = new List<FileType> { FileType.DistributionRegistration },
                    };
                    FileStorageResponse response = await GetFileStates(query).ConfigureAwait(false);

                    datasetMetadata = dataset.UpdateMetadata(response.Files.Count > 0, datasetMetadata);

                    foreach (FileState state in response.Files)
                    {
                        if (state.Content is not null)
                        {
                            DcatDistribution? distribution = DcatDistribution.Parse(state.Content);
                            if (distribution is not null)
                            {
                                datasetMetadata = distribution.UpdateDatasetMetadata(datasetMetadata);
                            }
                        }
                    }

                    await UpdateMetadata(datasetMetadata).ConfigureAwait(false);
                }
            }
        }
    }
}
