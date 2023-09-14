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
    }
}
