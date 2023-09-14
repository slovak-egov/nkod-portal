using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class FileStorageQuery
    {
        public List<string>? OnlyPublishers { get; set; }

        public Guid? ParentFile { get; set; }

        public List<FileType>? OnlyTypes { get; set; }

        public bool OnlyPublished { get; set; }

        public List<Guid>? OnlyIds { get; set; } 

        public bool IncludeDependentFiles { get; set; }

        public Dictionary<string, string[]>? AdditionalFilters { get; set; }

        public string? QueryText { get; set; }

        public List<FileStorageOrderDefinition>? OrderDefinitions { get; set; } 

        public int? MaxResults { get; set; }

        public int SkipResults { get; set; }

        public DateOnly? DateFrom { get; set; }

        public DateOnly? DateTo { get; set; }

        public List<string>? RequiredFacets { get; set; }

        public string Language { get; set; } = "sk";
    }
}
