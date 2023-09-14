using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class AnonymousAccessPolicy : IFileStorageAccessPolicy
    {
        public static AnonymousAccessPolicy Default { get; } = new AnonymousAccessPolicy();

        public bool HasModifyAccessToFile(FileMetadata metadata) => false;

        public bool HasReadAccessToFile(FileMetadata metadata) => false;
    }
}
