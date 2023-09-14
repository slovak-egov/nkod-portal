using NkodSk.Abstractions;
using NkodSk.RdfFulltextIndex;
using System.IO;

namespace DocumentStorageApi
{
    public class FulltextStorageMap : FulltextIndex
    {
        public FulltextStorageMap(IFileStorage fileStorage) 
        {
            FileStorageQuery query = new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration, FileType.PublisherRegistration, FileType.LocalCatalogRegistration }, OnlyPublished = true };
            FileStorageResponse response = fileStorage.GetFileStates(query, new PublicFileAccessPolicy());

            Index(response.Files);
        }
    }
}
