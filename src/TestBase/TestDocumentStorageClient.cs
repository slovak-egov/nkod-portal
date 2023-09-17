using AngleSharp.Io;
using NkodSk.Abstractions;
using NkodSk.RdfFulltextIndex;
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

        private readonly FulltextIndex fulltextIndex;

        private readonly IFileStorageAccessPolicy accessPolicy;

        public TestDocumentStorageClient(IFileStorage fileStorage, IFileStorageAccessPolicy accessPolicy)
        {
            this.fileStorage = fileStorage;
            this.accessPolicy = accessPolicy;
            fulltextIndex = new FulltextIndex();
            fulltextIndex.Initialize(fileStorage);
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
            if (!string.IsNullOrWhiteSpace(query.QueryText))
            {
                FulltextResponse fulltextResponse = fulltextIndex.Search(query);
                if (fulltextResponse.Documents.Count > 0)
                {
                    FileStorageQuery internalQuery = new FileStorageQuery
                    {
                        OnlyIds = fulltextResponse.Documents.Select(d => d.Id).ToList(),
                        QueryText = null,
                        OnlyPublishers = query.OnlyPublishers,
                        OnlyPublished = query.OnlyPublished,
                        RequiredFacets = query.RequiredFacets,
                    };
                    return Task.FromResult(fileStorage.GetFileStates(internalQuery, accessPolicy));
                }
                else
                {
                    return Task.FromResult(new FileStorageResponse(new List<FileState>(), 0, new List<Facet>()));
                }
            }
            else
            {
                return Task.FromResult(fileStorage.GetFileStates(query, accessPolicy));
            }
        }

        public Task<FileStorageGroupResponse> GetFileStatesByPublisher(FileStorageQuery query)
        {
            query.OnlyTypes = new List<FileType> { FileType.PublisherRegistration };
            if (!string.IsNullOrWhiteSpace(query.QueryText))
            {
                FulltextResponse fulltextResponse = fulltextIndex.Search(query);
                if (fulltextResponse.Documents.Count > 0)
                {
                    FileStorageQuery internalQuery = new FileStorageQuery
                    {
                        OnlyIds = fulltextResponse.Documents.Select(d => d.Id).ToList(),
                        QueryText = null,
                        OnlyPublishers = query.OnlyPublishers,
                        OnlyPublished = query.OnlyPublished,
                        RequiredFacets = query.RequiredFacets,
                        OrderDefinitions = query.OrderDefinitions,
                    };
                    return Task.FromResult(fileStorage.GetFileStatesByPublisher(internalQuery, accessPolicy));
                }
                else
                {
                    return Task.FromResult(new FileStorageGroupResponse(new List<FileStorageGroup>(), 0));
                }
            }
            else
            {
                return Task.FromResult(fileStorage.GetFileStatesByPublisher(query, accessPolicy));
            }
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
