namespace NkodSk.Abstractions
{
    public class PublicFileAccessPolicy : IFileStorageAccessPolicy
    {
        public bool HasModifyAccessToFile(FileMetadata metadata) => false;

        public bool HasReadAccessToFile(FileMetadata metadata) => false;
    }
}
