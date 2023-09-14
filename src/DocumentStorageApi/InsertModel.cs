using NkodSk.Abstractions;

namespace DocumentStorageApi
{
    public record InsertModel(string Content, FileMetadata Metadata, bool EnableOverwrite)
    {

    }
}
