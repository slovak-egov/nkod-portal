using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class TestAccess : IFileStorageAccessPolicy
    {
        public bool HasModifyAccessToFile(FileMetadata metadata)
        {
            return true;
        }

        public bool HasReadAccessToFile(FileMetadata metadata)
        {
            return true;
        }
    }
}
