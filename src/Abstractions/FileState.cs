using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public record FileState(FileMetadata Metadata, string? Content = null, IEnumerable<FileState>? DependentFiles = null)
    {
        
    }
}
