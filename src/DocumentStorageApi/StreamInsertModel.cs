using NkodSk.Abstractions;

namespace DocumentStorageApi
{
    public record StreamInsertModel(FileMetadata Metadata, bool EnableOverwrite, IFormFile File)
    {
    }
}
