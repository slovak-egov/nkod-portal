using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class PublisherFileAccessPolicy : IFileStorageAccessPolicy
    {
        private readonly string publisherId;

        public PublisherFileAccessPolicy(string publisherId)
        {
            this.publisherId = publisherId;
        }

        public bool HasDeleteAccessToFile(FileMetadata metadata)
        {
            return metadata.Publisher == publisherId;
        }

        public bool HasModifyAccessToFile(FileMetadata metadata)
        {
            return metadata.Publisher == publisherId;
        }

        public bool HasReadAccessToFile(FileMetadata metadata)
        {
            return metadata.Publisher == publisherId || metadata.IsPublic;
        }
    }
}
