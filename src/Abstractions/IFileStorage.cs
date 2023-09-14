using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public interface IFileStorage
    {
        FileStorageResponse GetFileStates(FileStorageQuery query, IFileStorageAccessPolicy accessPolicy);

        FileMetadata? GetFileMetadata(Guid id, IFileStorageAccessPolicy accessPolicy);

        FileState? GetFileState(Guid id, IFileStorageAccessPolicy accessPolicy);

        FileStorageGroupResponse GetFileStatesByPublisher(FileStorageQuery query, IFileStorageAccessPolicy accessPolicy);

        Stream? OpenReadStream(Guid id, IFileStorageAccessPolicy accessPolicy);

        Stream OpenWriteStream(FileMetadata metadata, bool enableOverwrite, IFileStorageAccessPolicy accessPolicy);

        void InsertFile(string content, FileMetadata metadata, bool enableOverwrite, IFileStorageAccessPolicy accessPolicy);

        void UpdateMetadata(FileMetadata metadata, IFileStorageAccessPolicy accessPolicy);

        void DeleteFile(Guid id, IFileStorageAccessPolicy accessPolicy);
    }
}
