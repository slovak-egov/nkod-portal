using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public record FileMetadata(Guid Id, LanguageDependedTexts Name, FileType Type, Guid? ParentFile, string? Publisher, bool IsPublic, string? OriginalFileName, DateTimeOffset Created, DateTimeOffset LastModified, Dictionary<string, string[]>? AdditionalValues = null)
    {
        public static FileMetadata LoadFrom(string path)
        {
            if (File.Exists(path))
            {
                return JsonConvert.DeserializeObject<FileMetadata>(File.ReadAllText(path))
                    ?? throw new Exception($"Unable to read metadata content from file {path}");
            }
            else
            {
                throw new Exception($"Unable to read metadata content from file {path} because file does not exist");
            }
        }

        public void SaveTo(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }
    }
}
