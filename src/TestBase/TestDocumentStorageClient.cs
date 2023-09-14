using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class TestDocumentStorageClient : IDocumentStorageClient
    {
        private readonly IFileStorage fileStorage;

        private readonly IFileStorageAccessPolicy accessPolicy;

        public TestDocumentStorageClient(IFileStorage fileStorage, IFileStorageAccessPolicy accessPolicy)
        {
            this.fileStorage = fileStorage;
            this.accessPolicy = accessPolicy;
        }

        public Task DeleteFile(Guid id)
        {
            fileStorage.DeleteFile(id, accessPolicy);
            return Task.CompletedTask;
        }

        public Task<Stream?> DownloadStream(Guid id)
        {
            return Task.FromResult(fileStorage.OpenReadStream(id, accessPolicy));
        }

        public Task<FileMetadata?> GetFileMetadata(Guid id)
        {
            return Task.FromResult(fileStorage.GetFileMetadata(id, accessPolicy));
        }

        public Task<FileState?> GetFileState(Guid id)
        {
            return Task.FromResult(fileStorage.GetFileState(id, accessPolicy));
        }

        public Task<FileStorageResponse> GetFileStates(FileStorageQuery query)
        {
            return Task.FromResult(fileStorage.GetFileStates(query, accessPolicy));
        }

        public Task<FileStorageGroupResponse> GetFileStatesByPublisher(FileStorageQuery query)
        {
            return Task.FromResult(fileStorage.GetFileStatesByPublisher(query, accessPolicy));
        }

        public Task InsertFile(string content, bool enableOverwrite, FileMetadata metadata)
        {
            fileStorage.InsertFile(content, metadata, enableOverwrite, accessPolicy);
            return Task.CompletedTask;
        }

        public Task UpdateMetadata(FileMetadata metadata)
        {
            fileStorage.UpdateMetadata(metadata, accessPolicy);
            return Task.CompletedTask;  
        }

        public async Task UploadStream(Stream source, FileMetadata metadata, bool enableOverwrite)
        {
            using (source)
            {
                using Stream target = fileStorage.OpenWriteStream(metadata, enableOverwrite, accessPolicy);
                await source.CopyToAsync(target);
            }
        }
    }
}
