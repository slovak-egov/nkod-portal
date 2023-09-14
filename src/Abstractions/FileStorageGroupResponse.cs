using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public record FileStorageGroupResponse(List<FileStorageGroup> Groups, int TotalCount)
    {
    }
}
