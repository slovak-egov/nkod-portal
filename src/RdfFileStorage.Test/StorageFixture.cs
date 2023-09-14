using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfFileStorage.Test
{
    public class StorageFixture : IDisposable
    {
        private readonly string path;

        public StorageFixture()
        {
            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            DateTimeOffset time = DateTimeOffset.UtcNow.AddHours(-2);

            FileMetadata metadata1 = new FileMetadata(Guid.NewGuid(), "Test1", FileType.DatasetRegistration, null, Guid.NewGuid().ToString(), true, null, time, time);
            FileMetadata metadata2 = new FileMetadata(Guid.NewGuid(), "Test2", FileType.DistributionRegistration, metadata1.Id, metadata1.Publisher, true, null, time.AddMinutes(1), time.AddHours(2));
            FileMetadata metadata3 = new FileMetadata(Guid.NewGuid(), "Test3", FileType.LocalCatalogRegistration, null, Guid.NewGuid().ToString(), false, null, time.AddHours(1), time.AddHours(1));

            ExistingStates = new FileState[]
            {
                new FileState(metadata1, "content1"),
                new FileState(metadata2, "content2"),
                new FileState(metadata3, "content3"),
            };
        }

        public FileState[] ExistingStates { get; }

        public FileState PublicFile => ExistingStates[0];

        public FileState NonPublicFile => ExistingStates[2];

        private void CreateFile(FileState state)
        {
            bool isPublic = Storage.ShouldBePublic(state.Metadata);
            string filePath = Path.Combine(path, isPublic ? "public" : "protected", state.Metadata.Id.ToString("N") + (isPublic ? ".ttl" : string.Empty));
            string metadataPath = Path.Combine(path, "protected", state.Metadata.Id.ToString("N") + ".metadata");
            File.WriteAllText(filePath, state.Content);
            state.Metadata.SaveTo(metadataPath);
        }

        public string GetStoragePath(bool createDirectory = true, bool insertContent = true)
        {
            if (createDirectory)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            } 
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
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

                if (insertContent)
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

                    foreach (FileState state in ExistingStates)
                    {
                        CreateFile(state);
                    }
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
