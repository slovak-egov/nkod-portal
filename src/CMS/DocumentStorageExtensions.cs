using DocumentStorageClient;
using NkodSk.Abstractions;

namespace CMS
{
    public static class DocumentStorageExtensions
    {
        public static async Task<(string, string, Guid?)> GetEmailForDataset(this IDocumentStorageClient documentStorageClient, string key)
        {
            FileStorageResponse storageResponse = await documentStorageClient.GetFileStates(new FileStorageQuery
            {
                OnlyTypes = new List<FileType> { FileType.DatasetRegistration },
                AdditionalFilters = new Dictionary<string, string[]> { { "key", new[] { key } } }
            });
            if (storageResponse is not null && storageResponse.Files.Count > 0 && storageResponse.Files[0].Content is not null)
            {
                DcatDataset dataset = DcatDataset.Parse(storageResponse.Files[0].Content);
                if (dataset?.Publisher is not null)
                {
                    return (await documentStorageClient.GetEmailForPublisher(dataset.Publisher.OriginalString), dataset.GetTitle("sk"), storageResponse.Files[0].Metadata.Id);
                }
            }
            return (null, null, null);
        }

        public static async Task<string> GetEmailForPublisher(this IDocumentStorageClient documentStorageClient, string key)
        {
            FileStorageResponse storageResponse = await documentStorageClient.GetFileStates(new FileStorageQuery
            {
                OnlyTypes = new List<FileType> { FileType.PublisherRegistration },
                OnlyPublishers = new List<string> { key }
            });
            if (storageResponse is not null && storageResponse.Files.Count > 0 && storageResponse.Files[0].Content is not null)
            {
                FoafAgent agent = FoafAgent.Parse(storageResponse.Files[0].Content);
                return agent?.EmailAddress;
            }
            return null;
        }
    }
}
