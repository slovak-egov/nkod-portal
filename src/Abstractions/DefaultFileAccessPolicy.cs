using NkodSk.Abstractions;

namespace NkodSk.Abstractions
{
    public class DefaultFileAccessPolicy : IFileStorageAccessPolicy
    {
        private readonly IHttpContextValueAccessor httpContextAccessor;

        public DefaultFileAccessPolicy(IHttpContextValueAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public bool HasReadAccessToFile(FileMetadata metadata)
        {
            if (metadata.IsPublic)
            {
                return true;
            }

            return HasModifyAccessToFile(metadata);
        }

        public bool HasModifyAccessToFile(FileMetadata metadata)
        {
            if (httpContextAccessor.HasRole("Superadmin"))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(metadata.Publisher))
            {
                return httpContextAccessor.Publisher == metadata.Publisher;
            }

            return false;
        }
    }
}
