using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfFileStorage.Test
{
    public class PublisherAccessPolicy : IFileStorageAccessPolicy
    {
        private readonly string publisher;

        public PublisherAccessPolicy(string publisher)
        {
            this.publisher = publisher;
        }

        public bool HasModifyAccessToFile(FileMetadata metadata)
        {
            if (metadata.IsPublic)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(metadata.Publisher) && metadata.Publisher == publisher)
            {
                return true;
            }

            return false;
        }

        public bool HasReadAccessToFile(FileMetadata metadata)
        {
            return HasModifyAccessToFile(metadata);
        }
    }
}
