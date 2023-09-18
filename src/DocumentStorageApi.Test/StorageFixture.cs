using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using RdfFileStorage.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using static Lucene.Net.Documents.Field;

namespace DocumentStorageApi.Test
{
    public class StorageFixture : IDisposable
    {
        private readonly string path;

        public StorageFixture()
        {
            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public List<FileState> ExistingStates { get; } = new List<FileState>();

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

                    ExistingStates.Clear();
                    int allIndex = 0;

                    FileState Create(FileType fileType, Guid? parentFile, string? publisher, bool isPublic)
                    {
                        FileMetadata metadata = new FileMetadata(Guid.NewGuid(), Guid.NewGuid().ToString(), fileType, parentFile, publisher, isPublic, null, DateTimeOffset.UtcNow.AddMinutes(-allIndex * 2), DateTimeOffset.UtcNow.AddMinutes(-allIndex));
                        string content;

                        if (Storage.IsTurtleFile(metadata))
                        {
                            content = $"<http://example.com/{metadata.Id}> <http://example.com/title> \"{metadata.Name["sk"]}\"@sk .";
                        }
                        else
                        {
                            content = $"content {allIndex}";
                        }

                        FileState state = new FileState(metadata, content);
                        CreateFile(state);
                        ExistingStates.Add(state);
                        return state;
                    }

                    string publisher1 = Guid.NewGuid().ToString();
                    string publisher2 = Guid.NewGuid().ToString();
                    string publisher3 = Guid.NewGuid().ToString();
                    FileState parent1 = Create(FileType.DatasetRegistration, null, publisher1, true);
                    FileState parent2 = Create(FileType.LocalCatalogRegistration, null, publisher3, false);

                    foreach (FileType fileType in Enum.GetValues(typeof(FileType)))
                    {
                        for (int i = 0; i < 40; i++)
                        {
                            Guid? parent = (allIndex % 3) switch
                            {
                                0 => parent1.Metadata.Id,
                                1 => parent2.Metadata.Id,
                                _ => null,
                            };
                            string? publisher = ((allIndex + 1) % 3) switch
                            {
                                0 => publisher1,
                                1 => publisher2,
                                _ => null,
                            };
                            Create(fileType, parent, publisher, (i % 2) == 0);
                            allIndex++;
                        }
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
