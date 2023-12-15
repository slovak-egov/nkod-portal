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

            if (httpContextAccessor.HasRole("Superadmin"))
            {
                return true;
            }
                        
            if (httpContextAccessor.HasRole("Harvester") && metadata.Type == FileType.LocalCatalogRegistration)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(metadata.Publisher) && (httpContextAccessor.HasRole("Publisher") || httpContextAccessor.HasRole("PublisherAdmin")))
            {
                return httpContextAccessor.Publisher == metadata.Publisher;
            }

            return false;
        }

        public bool HasModifyAccessToFile(FileMetadata metadata)
        {
            if (httpContextAccessor.HasRole("Superadmin"))
            {
                return true;
            }

            if (httpContextAccessor.HasRole("Harvester") && metadata.IsHarvested)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(metadata.Publisher) && (httpContextAccessor.HasRole("Publisher") || httpContextAccessor.HasRole("PublisherAdmin")))
            {
                if (httpContextAccessor.Publisher == metadata.Publisher)
                {
                    return metadata.Type == FileType.DatasetRegistration ||
                        metadata.Type == FileType.DistributionRegistration ||
                        metadata.Type == FileType.LocalCatalogRegistration ||
                        metadata.Type == FileType.DistributionFile ||
                        metadata.Type == FileType.PublisherRegistration;
                }
            }

            return false;
        }

        public bool HasDeleteAccessToFile(FileMetadata metadata)
        {
            if (httpContextAccessor.HasRole("Superadmin"))
            {
                return true;
            }

            if (httpContextAccessor.HasRole("Harvester") && metadata.IsHarvested)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(metadata.Publisher) && (httpContextAccessor.HasRole("Publisher") || httpContextAccessor.HasRole("PublisherAdmin")))
            {
                if (httpContextAccessor.Publisher == metadata.Publisher)
                {
                    return metadata.Type == FileType.DatasetRegistration ||
                        metadata.Type == FileType.DistributionRegistration ||
                        metadata.Type == FileType.LocalCatalogRegistration ||
                        metadata.Type == FileType.DistributionFile;
                }
            }

            return false;
        }
    }
}
