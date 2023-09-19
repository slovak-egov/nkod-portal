using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class PublisherAccessPolicy : IFileStorageAccessPolicy
    {
        private string publisher;

        public PublisherAccessPolicy(string publisher)
        {
            this.publisher = publisher;
        }

        public bool HasModifyAccessToFile(FileMetadata metadata)
        {
            return metadata.Publisher == publisher;
        }

        public bool HasReadAccessToFile(FileMetadata metadata)
        {
            return metadata.Publisher == publisher;
        }
    }
}
