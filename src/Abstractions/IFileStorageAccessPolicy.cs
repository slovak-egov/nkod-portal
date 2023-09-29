using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public interface IFileStorageAccessPolicy
    {
        bool HasReadAccessToFile(FileMetadata metadata);

        bool HasModifyAccessToFile(FileMetadata metadata);

        bool HasDeleteAccessToFile(FileMetadata metadata);
    }
}
