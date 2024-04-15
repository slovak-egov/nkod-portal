using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportRegistrations.Test
{
    public class StorageFixture : IDisposable
    {
        private readonly string path;

        public StorageFixture()
        {
            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public void UpdateMetadata(FileMetadata metadata)
        {
            string metadataPath = Path.Combine(path, "protected", metadata.Id.ToString("N") + ".metadata");
            metadata.SaveTo(metadataPath);
        }

        public void CreateFile(FileState state)
        {
            bool isPublic = Storage.ShouldBePublic(state.Metadata);
            string folder = Path.Combine(path, Storage.GetDefaultSubfolderName(state.Metadata));
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string filePath = Path.Combine(folder, state.Metadata.Id.ToString("N") + (isPublic ? ".ttl" : string.Empty));
            File.WriteAllText(filePath, state.Content);
            UpdateMetadata(state.Metadata);
        }

        public (Uri, Guid) CreateLocalCatalog(string name, string publisher, string? nameEn = null, string? description = null, string? descriptionEn = null)
        {
            DcatCatalog catalog = DcatCatalog.Create();

            Dictionary<string, string> names = new Dictionary<string, string> { { "sk", name } };
            if (nameEn is not null)
            {
                names["en"] = nameEn;
            }
            catalog.SetTitle(names);

            names.Clear();
            if (description is not null)
            {
                names["sk"] = description;
            }
            if (descriptionEn is not null)
            {
                names["en"] = descriptionEn;
            }
            catalog.SetDescription(names);

            catalog.Publisher = new Uri(publisher);

            FileMetadata metadata = catalog.UpdateMetadata();
            CreateFile(new FileState(metadata, catalog.ToString()));
            return (catalog.Uri, metadata.Id);
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
