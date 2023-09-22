using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class AllAccessFilePolicy : IFileStorageAccessPolicy
    {
        public bool HasModifyAccessToFile(FileMetadata metadata) => true;

        public bool HasReadAccessToFile(FileMetadata metadata) => true;
    }
}
