using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public record FileStorageGroup(string Key, FileState? PublisherFileState, int Count, Dictionary<string, int>? Themes)
    {
    }
}
