using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace WebApi.Test
{
    public class StorageFixture : IDisposable
    {
        private readonly string path;

        private static int index = 0;

        public StorageFixture()
        {
            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public void CreateFile(FileState state)
        {
            bool isPublic = Storage.ShouldBePublic(state.Metadata);
            string filePath = Path.Combine(path, isPublic ? "public" : "protected", state.Metadata.Id.ToString("N") + (isPublic ? ".ttl" : string.Empty));
            string metadataPath = Path.Combine(path, "protected", state.Metadata.Id.ToString("N") + ".metadata");
            File.WriteAllText(filePath, state.Content);
            state.Metadata.SaveTo(metadataPath);
        }

        public Guid CreateDataset(string name, string publisher, bool isPublic = true)
        {
            int index = Interlocked.Increment(ref StorageFixture.index);
            DcatDataset dataset = DcatDataset.Create(new Uri($"http://example.com/dataset/{index}"));
            dataset.SetTitle(new Dictionary<string, string> { { "sk", name } });
            dataset.Publisher = new Uri(publisher);
            FileMetadata metatdata = dataset.UpdateMetadata(true);
            CreateFile(new FileState(metatdata, dataset.ToString()));
            return metatdata.Id;
        }

        public Guid CreateDistributionFile(string name, string content, bool isPublic = true)
        {
            Guid id = Guid.NewGuid();
            FileMetadata metadata = new FileMetadata(id, id.ToString(), FileType.DistributionFile, null, null, isPublic, name, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            CreateFile(new FileState(metadata, content));
            return id;
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
