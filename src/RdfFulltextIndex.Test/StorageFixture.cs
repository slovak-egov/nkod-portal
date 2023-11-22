using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.RdfFulltextIndex.Test
{
    public class StorageFixture 
    {
        public StorageFixture() 
        {
            Index = new FulltextIndex(new DefaultLanguagesSource());
            Index.Index(new[] { new FileState(
                new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, "Pub1", true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                File.ReadAllText(@"c:\Temp\rdf.txt")) });
        }

        public FulltextIndex Index { get; set; }
    }
}
