using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfFileStorage.Test
{
    public class DynamicAccessPolicy : IFileStorageAccessPolicy
    {
        private readonly Func<FileMetadata, bool> resultFunc;

        public DynamicAccessPolicy(Func<FileMetadata, bool> resultFunc)
        {
            this.resultFunc = resultFunc;
        }

        public bool HasModifyAccessToFile(FileMetadata metadata)
        {
            return resultFunc(metadata);
        }

        public bool HasReadAccessToFile(FileMetadata metadata)
        {
            return resultFunc(metadata);
        }

        public bool HasDeleteAccessToFile(FileMetadata metadata)
        {
            return resultFunc(metadata);
        }
    }
}
