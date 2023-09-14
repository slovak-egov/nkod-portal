using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodelistProvider.Test
{
    public class StorageFixture : IDisposable
    {
        private readonly string path;

        public StorageFixture()
        {
            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        private void CreateFile(FileState state)
        {
            bool isPublic = Storage.ShouldBePublic(state.Metadata);
            string filePath = Path.Combine(path, isPublic ? "public" : "protected", state.Metadata.Id.ToString("N") + (isPublic ? ".ttl" : string.Empty));
            string metadataPath = Path.Combine(path, "protected", state.Metadata.Id.ToString("N") + ".metadata");
            File.WriteAllText(filePath, state.Content);
            state.Metadata.SaveTo(metadataPath);
        }

        public string GetStoragePath(bool includeFiles = true)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (Directory.Exists(path))
            {
                foreach (string p in Directory.EnumerateDirectories(path))
                {
                    Directory.Delete(p, true);
                }
                foreach (string p in Directory.EnumerateFiles(path))
                {
                    File.Delete(p);
                }

                if (includeFiles)
                {
                    string folderPath = Path.Combine(path, "public");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    folderPath = Path.Combine(path, "protected");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "themes.ttl", FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
                    FileState state = new FileState(metadata, Properties.Resources.codelist1);
                    CreateFile(state);

                    metadata = new FileMetadata(Guid.NewGuid(), "frequencies.ttl", FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
                    state = new FileState(metadata, Properties.Resources.codelist2);
                    CreateFile(state);
                }
            }

            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
