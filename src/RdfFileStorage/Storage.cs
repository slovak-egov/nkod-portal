using Lucene.Net.QueryParsers.Classic;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using static Lucene.Net.Queries.Function.ValueSources.MultiFunction;
using static Lucene.Net.Search.FieldValueHitQueue;

namespace NkodSk.RdfFileStorage
{
    public class Storage : IFileStorage, IDisposable
    {
        private readonly string storagePath;

        private readonly string publicTurtlePath;

        private readonly string protectedPath;

        private readonly HashSet<Guid> entries;

        private readonly Dictionary<Guid, Entry> entryProperties;

        private readonly Dictionary<Guid, HashSet<Guid>> dependentEntries;

        private readonly Dictionary<string, HashSet<Guid>> entriesByPublisher;

        private readonly Dictionary<FileType, HashSet<Guid>> entriesByType;

        private readonly Dictionary<string, Dictionary<string, HashSet<Guid>>> additionalFilters;

        private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        private int disposed;

        public Storage(string path, string publicTurtleFolderName = "public", string protectedFolderName = "protected")
        {
            this.storagePath = path;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            publicTurtlePath = Path.Combine(path, publicTurtleFolderName);
            if (!Directory.Exists(publicTurtlePath))
            {
                Directory.CreateDirectory(publicTurtlePath);
            }

            protectedPath = Path.Combine(path, protectedFolderName);
            if (!Directory.Exists(protectedPath))
            {
                Directory.CreateDirectory(protectedPath);
            }

            string[] files = GetAllMetadataFiles();
            int defaultCapacity = files.Length * 2;

            entries = new HashSet<Guid>(defaultCapacity);
            entryProperties = new Dictionary<Guid, Entry>(defaultCapacity);
            dependentEntries = new Dictionary<Guid, HashSet<Guid>>(defaultCapacity);
            entriesByPublisher = new Dictionary<string, HashSet<Guid>>(defaultCapacity);
            entriesByType = new Dictionary<FileType, HashSet<Guid>>(Enum.GetValues<FileType>().Length);
            additionalFilters = new Dictionary<string, Dictionary<string, HashSet<Guid>>>();

            LoadEntries(files);
        }

        private string[] GetAllMetadataFiles() => Directory.GetFiles(storagePath, "*.metadata", SearchOption.AllDirectories);

        public void LoadEntries()
        {
            LoadEntries(GetAllMetadataFiles());
        }

        private void ClearEntries()
        {
            entries.Clear();
            entryProperties.Clear();
            dependentEntries.Clear();
            entriesByPublisher.Clear();

            foreach (HashSet<Guid> entries in entriesByType.Values)
            {
                entries.Clear();
            }
        }

        private void LoadEntries(string[] files)
        {
            rwLock.EnterWriteLock();
            try
            {
                CheckDispose();

                ClearEntries();
                
                int defaultCapacity = files.Length * 2;
                foreach (FileType fileType in Enum.GetValues<FileType>())
                {
                    if (!entriesByType.ContainsKey(fileType))
                    {
                        entriesByType[fileType] = new HashSet<Guid>(defaultCapacity);
                    }
                }

                List<Exception> exceptions = new List<Exception>();

                foreach (string metadataPath in files)
                {
                    try
                    {
                        FileMetadata metadata = FileMetadata.LoadFrom(metadataPath);
                        string filePath = GetFilePath(metadata);

                        if (!File.Exists(filePath))
                        {
                            throw new Exception($"Unable to find target file for metadata file {metadataPath}, id {metadata.Id}");
                        }

                        InsertFileEntry(metadata, filePath);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }

                if (exceptions.Count > 0)
                {
                    throw new AggregateException(exceptions);
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        private void CheckDispose()
        {
            if (disposed == 1)
            {
                throw new ObjectDisposedException("Storage is already disposed");
            }
        }

        private Entry InsertFileEntry(FileMetadata metadata, string path)
        {
            Entry entry = new Entry(path, metadata, new StreamLocks(path));

            InsertFileEntry(entry);

            return entry;
        }

        private void InsertFileEntry(Entry entry)
        {
            FileMetadata metadata = entry.Metadata;

            entries.Add(metadata.Id);
            entryProperties[metadata.Id] = entry;

            if (metadata.ParentFile.HasValue)
            {
                if (!dependentEntries.ContainsKey(metadata.ParentFile.Value))
                {
                    dependentEntries[metadata.ParentFile.Value] = new HashSet<Guid>(10);
                }
                dependentEntries[metadata.ParentFile.Value].Add(metadata.Id);
            }

            if (metadata.Publisher != null)
            {
                if (!entriesByPublisher.ContainsKey(metadata.Publisher))
                {
                    entriesByPublisher[metadata.Publisher] = new HashSet<Guid>(10);
                }
                entriesByPublisher[metadata.Publisher].Add(metadata.Id);
            }

            if (metadata.AdditionalValues is not null)
            {
                foreach ((string key, string[] values) in metadata.AdditionalValues)
                {
                    if (!additionalFilters.ContainsKey(key))
                    {
                        additionalFilters[key] = new Dictionary<string, HashSet<Guid>>(10);
                    }

                    foreach (string value in values)
                    {
                        if (!additionalFilters[key].ContainsKey(value))
                        {
                            additionalFilters[key][value] = new HashSet<Guid>(10);
                        }
                        additionalFilters[key][value].Add(metadata.Id);
                    }
                }
            }

            entriesByType[metadata.Type].Add(metadata.Id);
        }

        private void RemoveFileEntry(Guid id)
        {
            if (entryProperties.TryGetValue(id, out Entry? entry))
            {
                FileMetadata metadata = entry.Metadata;

                if (metadata.ParentFile.HasValue && dependentEntries.TryGetValue(metadata.ParentFile.Value, out HashSet<Guid>? parentFiles))
                {
                    parentFiles.Remove(metadata.Id);
                }

                if (metadata.Publisher != null && entriesByPublisher.TryGetValue(metadata.Publisher, out HashSet<Guid>? publisherFiles))
                {
                    publisherFiles.Remove(metadata.Id);
                }

                if (entriesByType.TryGetValue(metadata.Type, out HashSet<Guid>? typeFiles))
                {
                    typeFiles.Remove(metadata.Id);
                }

                if (metadata.AdditionalValues is not null)
                {
                    foreach ((string key, string[] values) in metadata.AdditionalValues)
                    {
                        if (additionalFilters.TryGetValue(key, out Dictionary<string, HashSet<Guid>>? filterValues))
                        {
                            foreach (string value in values)
                            {
                                if (filterValues.TryGetValue(value, out HashSet<Guid>? filterFiles))
                                {
                                    filterFiles.Remove(metadata.Id);
                                }
                            }
                        }
                    }
                }

                entryProperties.Remove(metadata.Id);
                entries.Remove(metadata.Id);
            }
        }

        public static bool IsTurtleFile(FileMetadata metadata) =>
            metadata.Type == FileType.DatasetRegistration ||
            metadata.Type == FileType.DistributionRegistration ||
            metadata.Type == FileType.LocalCatalogRegistration ||
            metadata.Type == FileType.PublisherRegistration;

        public static bool ShouldBePublic(FileMetadata metadata) => metadata.IsPublic && IsTurtleFile(metadata);

        private string GetFilePath(FileMetadata metadata)
        {
            if (ShouldBePublic(metadata))
            {
                return Path.Combine(publicTurtlePath, metadata.Id.ToString("N") + ".ttl");
            }
            else
            {
                return Path.Combine(protectedPath, metadata.Id.ToString("N"));
            }
        }

        private string GetMetadataPath(Guid id) => Path.Combine(protectedPath, id.ToString("N") + ".metadata");

        private static string? ReadFileContent(string path)
        {
            FileInfo fileInfo = new FileInfo(path);

            if (!fileInfo.Exists)
            {
                throw new Exception("Unable to find required file");
            }
            else if (fileInfo.Length > 10485760)
            {
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            try
            {
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to read file {path}", e);
            }
        }

        private List<Entry> GetRelevantEntries(FileStorageQuery query, IFileStorageAccessPolicy accessPolicy)
        {
            List<HashSet<Guid>> sets = new List<HashSet<Guid>>();

            static HashSet<Guid> MergeSets<T>(Dictionary<T, HashSet<Guid>> entriesByKey, IEnumerable<T> keys) where T : notnull
            {
                int capacity = 0;
                foreach (T key in keys)
                {
                    if (entriesByKey.TryGetValue(key, out HashSet<Guid>? typeSet))
                    {
                        capacity += typeSet.Count;
                    }
                }

                HashSet<Guid> typesSet = new HashSet<Guid>(capacity);
                foreach (T key in keys)
                {
                    if (entriesByKey.TryGetValue(key, out HashSet<Guid>? typeSet))
                    {
                        typesSet.UnionWith(typeSet);
                    }
                }
                return typesSet;
            }

            if (query.OnlyPublishers is not null && query.OnlyPublishers.Count > 0 && (query.RequiredFacets == null || !query.RequiredFacets.Contains("publishers")))
            {
                sets.Add(MergeSets(entriesByPublisher, query.OnlyPublishers));
            }

            if (query.OnlyTypes != null && query.OnlyTypes.Count > 0)
            {
                sets.Add(MergeSets(entriesByType, query.OnlyTypes));
            }

            if (query.ParentFile.HasValue)
            {
                if (dependentEntries.TryGetValue(query.ParentFile.Value, out HashSet<Guid>? parentSet))
                {
                    sets.Add(parentSet);
                }
            }

            if (query.OnlyIds is not null && query.OnlyIds.Count > 0)
            {
                sets.Add(new HashSet<Guid>(query.OnlyIds));
            }

            if (query.AdditionalFilters is not null)
            {
                foreach ((string key, string[] values) in query.AdditionalFilters)
                {
                    if (values.Length > 0 && additionalFilters.TryGetValue(key, out Dictionary<string, HashSet<Guid>>? filterValues) && (query.RequiredFacets == null || !query.RequiredFacets.Contains(key)))
                    {
                        sets.Add(MergeSets(filterValues, values));
                    }
                }
            }

            HashSet<Guid> set;

            if (sets.Count > 0)
            {
                set = new HashSet<Guid>(entries);
                foreach (HashSet<Guid> s in sets)
                {
                    set.IntersectWith(s);
                }
            }
            else
            {
                set = entries;
            }

            List<Entry> results = new List<Entry>(set.Count);

            foreach (Guid id in set)
            {
                if (entryProperties.TryGetValue(id, out Entry? entry) && entry.CanBeReadWithPolicy(accessPolicy) && (!query.OnlyPublished || entry.Metadata.IsPublic))
                {
                    results.Add(entry);
                }
            }

            return results;
        }

        public FileStorageResponse GetFileStates(FileStorageQuery query, IFileStorageAccessPolicy accessPolicy)
        {
            rwLock.EnterReadLock();
            try
            {
                CheckDispose();

                List<Entry> results = GetRelevantEntries(query, accessPolicy);

                List<FileStorageOrderDefinition> orderDefinitions = query.OrderDefinitions?.ToList() ?? new List<FileStorageOrderDefinition>(1);
                if (orderDefinitions.Count == 0)
                {
                      orderDefinitions.Add(new FileStorageOrderDefinition(FileStorageOrderProperty.LastModified, true));
                }

                int CompareEntries(Entry a, Entry b)
                {
                    for (int i = 0; i < orderDefinitions.Count; i++)
                    {
                        FileStorageOrderDefinition orderDefinition = orderDefinitions[i];
                        int reverseCoefficient = orderDefinition.ReverseOrder ? -1 : 1;
                        int compare;
                        switch (orderDefinition.Property)
                        {
                            case FileStorageOrderProperty.Created:
                                compare = a.Metadata.Created.CompareTo(b.Metadata.Created) * reverseCoefficient;
                                break;
                            case FileStorageOrderProperty.LastModified:
                            case FileStorageOrderProperty.Revelance:
                                compare = a.Metadata.LastModified.CompareTo(b.Metadata.LastModified) * reverseCoefficient;
                                break;
                            case FileStorageOrderProperty.Name:
                                compare = StringComparer.CurrentCultureIgnoreCase.Compare(a.Metadata.Name.GetText(query.Language), b.Metadata.Name.GetText(query.Language)) * reverseCoefficient;
                                break;
                            default:
                                throw new Exception($"Invalid order type {Enum.GetName(orderDefinition.Property)}");
                        }
                        if (compare != 0)
                        {
                            return compare;
                        }
                    }
                    return 0;
                }

                List<Facet> facets = new List<Facet>(query.RequiredFacets?.Count ?? 0);

                if (query.RequiredFacets is not null && query.RequiredFacets.Count > 0)
                {
                    foreach (string facetId in query.RequiredFacets)
                    {
                        Facet facet = new Facet(facetId);
                        Dictionary<string, int> counts = facet.Values;
                       
                        switch (facetId)
                        {
                            case "publishers":
                                foreach (Entry entry in results)
                                {
                                    if (entry.Metadata.Publisher is not null)
                                    {
                                        if (!counts.TryGetValue(entry.Metadata.Publisher, out int count))
                                        {
                                            count = 0;
                                        }
                                        counts[entry.Metadata.Publisher] = count + 1;
                                    }
                                }
                                break;
                            default:
                                foreach (Entry entry in results)
                                {
                                    if (entry.Metadata.AdditionalValues is not null && entry.Metadata.AdditionalValues.TryGetValue(facetId, out string[]? values))
                                    {
                                        foreach (string value in values)
                                        {
                                            if (!counts.TryGetValue(value, out int count))
                                            {
                                                count = 0;
                                            }
                                            counts[value] = count + 1;
                                        }
                                    }
                                }
                                break;
                        }

                        facets.Add(facet);
                    }

                    HashSet<Entry> filteredEntries = new HashSet<Entry>(results);

                    if (query.RequiredFacets.Contains("publishers") && query.OnlyPublishers is not null && query.OnlyPublishers.Count > 0)
                    {
                        HashSet<string> publisherKeys = new HashSet<string>(query.OnlyPublishers);
                        filteredEntries.RemoveWhere(e => e.Metadata.Publisher == null || !publisherKeys.Contains(e.Metadata.Publisher));
                    }

                    if (query.AdditionalFilters is not null)
                    {
                        foreach (string facetId in query.RequiredFacets)
                        {
                            if (query.AdditionalFilters.TryGetValue(facetId, out string[]? values) && values.Length > 0)
                            {
                                HashSet<string> keys = new HashSet<string>(values);
                                filteredEntries.RemoveWhere(e => e.Metadata.AdditionalValues == null || !e.Metadata.AdditionalValues.TryGetValue(facetId, out string[]? entryValues) || !keys.Overlaps(entryValues));
                            }                                
                        }
                    }

                    results = filteredEntries.ToList();
                }

                results.Sort(CompareEntries);

                int pageResultsCount = Math.Min(query.MaxResults.GetValueOrDefault(results.Count), int.Max(0, results.Count - query.SkipResults));

                List<FileState> pageResults = new List<FileState>(pageResultsCount);

                int maxIndex = query.SkipResults + pageResultsCount;
                for (int i = query.SkipResults; i < maxIndex; i++)
                {
                    Entry entry = results[i];

                    string? fileContent = ReadFileContent(entry.Path);

                    List<FileState>? dependentStates = null;

                    if (query.IncludeDependentFiles)
                    {
                        if (dependentEntries.TryGetValue(entry.Metadata.Id, out HashSet<Guid>? dependentKeys))
                        {
                            dependentStates = new List<FileState>(dependentKeys.Count);
                            foreach (Guid dependentKey in dependentKeys)
                            {
                                if (entryProperties.TryGetValue(dependentKey, out Entry? dependentEntry) && dependentEntry.CanBeReadWithPolicy(accessPolicy))
                                {
                                    string? dependentContent = ReadFileContent(dependentEntry.Path);
                                    dependentStates.Add(new FileState(dependentEntry.Metadata, dependentContent, null));
                                }
                            }
                        }
                        else
                        {
                            dependentStates = new List<FileState>();
                        }
                    }
                    
                    FileState fileState = new FileState(entry.Metadata, fileContent, dependentStates);

                    pageResults.Add(fileState);
                }

                return new FileStorageResponse(pageResults, results.Count, facets);
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        private FileState? GetPublisherRegistration(string publisherId)
        {
            if (entriesByPublisher.TryGetValue(publisherId, out HashSet<Guid>? publisherFiles))
            {
                if (entriesByType.TryGetValue(FileType.PublisherRegistration, out HashSet<Guid>? publisherRegistrations))
                {
                    HashSet<Guid> keys = new HashSet<Guid>(publisherFiles);
                    keys.IntersectWith(publisherRegistrations);
                    if (keys.Count > 0)
                    {
                        Guid id = keys.First();
                        if (entryProperties.TryGetValue(id, out Entry? entry))
                        {
                            string? fileContent = ReadFileContent(entry.Path);
                            return new FileState(entry.Metadata, fileContent);
                        }
                    }
                }
            }
            return null;
        }

        public FileStorageGroupResponse GetFileStatesByPublisher(FileStorageQuery query, IFileStorageAccessPolicy accessPolicy)
        {
            Dictionary<string, int> countByPublisher = new Dictionary<string, int>(100);
            Dictionary<string, Dictionary<string, int>> themesByPublisher = new Dictionary<string, Dictionary<string, int>>(100);

            rwLock.EnterReadLock();
            try
            {
                CheckDispose();

                string themeKey = $"themes_{query.Language}";

                FileStorageQuery datasetQuery = new FileStorageQuery
                {
                    OnlyTypes = new List<FileType> { FileType.DatasetRegistration },
                    OnlyPublished = true
                };

                List<Entry> results = GetRelevantEntries(datasetQuery, accessPolicy);

                foreach (Entry entry in results)
                {
                    string? publisherName = entry.Metadata.Publisher;
                    if (!string.IsNullOrEmpty(publisherName))
                    {
                        if (!countByPublisher.TryGetValue(publisherName, out int count))
                        {
                            count = 0;
                        }
                        countByPublisher[publisherName] = count + 1;

                        if (entry.Metadata.AdditionalValues is not null && entry.Metadata.AdditionalValues.TryGetValue(themeKey, out string[]? themes) && themes.Length > 0)
                        {
                            Dictionary<string, int>? themesCount;
                            if (!themesByPublisher.TryGetValue(publisherName, out themesCount))
                            {
                                themesCount = new Dictionary<string, int>(10);
                                themesByPublisher[publisherName] = themesCount;
                            }

                            foreach (string theme in themes)
                            {
                                if (!themesCount.TryGetValue(theme, out int themeCount))
                                {
                                    themeCount = 0;
                                }
                                themesCount[theme] = themeCount + 1;
                            }
                        }


                    }
                }
            }
            finally
            {
                rwLock.ExitReadLock();
            }


            List<FileStorageGroup> groups = new List<FileStorageGroup>(countByPublisher.Count);
            foreach ((string publisherId, int count) in countByPublisher)
            {
                themesByPublisher.TryGetValue(publisherId, out Dictionary<string, int>? themes);
                groups.Add(new FileStorageGroup(publisherId, GetPublisherRegistration(publisherId), count, themes));
            }

            List<FileStorageOrderDefinition> orderDefinitions = query.OrderDefinitions?.ToList() ?? new List<FileStorageOrderDefinition>(2);
            if (orderDefinitions.Count == 0)
            {
                orderDefinitions.Add(new FileStorageOrderDefinition(FileStorageOrderProperty.Revelance, true));
            }

            int CompareEntries(FileStorageGroup a, FileStorageGroup b)
            {
                for (int i = 0; i < orderDefinitions.Count; i++)
                {
                    FileStorageOrderDefinition orderDefinition = orderDefinitions[i];
                    int reverseCoefficient = orderDefinition.ReverseOrder ? -1 : 1;
                    switch (orderDefinition.Property)
                    {
                        case FileStorageOrderProperty.Revelance:
                            return a.Count.CompareTo(b.Count) * -reverseCoefficient;
                        case FileStorageOrderProperty.Name:
                            string na = a.PublisherFileState?.Metadata.Name.GetText(query.Language) ?? string.Empty;
                            string nb = b.PublisherFileState?.Metadata.Name.GetText(query.Language) ?? string.Empty;
                            return StringComparer.CurrentCultureIgnoreCase.Compare(na, nb) * reverseCoefficient;
                    }
                }
                return 0;
            }

            groups.Sort(CompareEntries);

            List<FileStorageGroup> pageResults;

            if (string.IsNullOrEmpty(query.QueryText))
            {
                if (groups.Count > query.SkipResults)
                {
                    pageResults = groups.GetRange(query.SkipResults, Math.Min(query.MaxResults.GetValueOrDefault(groups.Count), groups.Count - query.SkipResults));
                }
                else
                {
                    pageResults = new List<FileStorageGroup>(0);
                }
            }
            else
            {
                pageResults = groups;
            }

            return new FileStorageGroupResponse(pageResults, groups.Count);
        }

        public FileMetadata? GetFileMetadata(Guid id, IFileStorageAccessPolicy accessPolicy)
        {
            rwLock.EnterReadLock();
            try
            {
                if (entryProperties.TryGetValue(id, out Entry? entry) && entry.CanBeReadWithPolicy(accessPolicy))
                {
                    return entry.Metadata;
                }
                return null;
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        public FileState? GetFileState(Guid id, IFileStorageAccessPolicy accessPolicy)
        {
            rwLock.EnterReadLock();
            try
            {
                if (entryProperties.TryGetValue(id, out Entry? entry) && entry.CanBeReadWithPolicy(accessPolicy))
                {
                    string? fileContent = ReadFileContent(entry.Path);

                    return new FileState(entry.Metadata, fileContent);
                }
                return null;
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        private void ValidateMetadata(FileMetadata metadata)
        {
            if (metadata.ParentFile.HasValue && !entries.Contains(metadata.ParentFile.Value))
            {
                throw new Exception($"Parent file {metadata.ParentFile.Value} does not exist");
            }
        }

        private Entry InternalInsertFile(FileMetadata metadata, bool enableOverwrite, IFileStorageAccessPolicy accessPolicy, Action<string> writeAction)
        {
            ValidateMetadata(metadata);

            if (!accessPolicy.HasModifyAccessToFile(metadata))
            {
                throw new UnauthorizedAccessException($"Unauthorized access to file {metadata.Id}");
            }

            string path;

            if (entryProperties.TryGetValue(metadata.Id, out Entry? existingEntry))
            {
                if (!accessPolicy.HasModifyAccessToFile(existingEntry.Metadata))
                {
                    throw new UnauthorizedAccessException($"Unauthorized access to file {metadata.Id}");
                }

                path = GetFilePath(existingEntry.Metadata);

                if (File.Exists(path) && !enableOverwrite)
                {
                    throw new Exception($"File {metadata.Id} already exists and cannot be overwritten");
                }

                if (!existingEntry.CanBeWritten)
                {
                    throw new Exception($"File {metadata.Id} is in use and cannot be overwritten");
                }

                File.Delete(path);
            }

            path = GetFilePath(metadata);

            if (File.Exists(path) && !enableOverwrite)
            {
                throw new Exception($"File {metadata.Id} already exists and cannot be overwritten");
            }

            metadata.SaveTo(GetMetadataPath(metadata.Id));

            writeAction(path);

            RemoveFileEntry(metadata.Id);
            return InsertFileEntry(metadata, path);
        }

        public void InsertFile(string content, FileMetadata metadata, bool enableOverwrite, IFileStorageAccessPolicy accessPolicy)
        {
            rwLock.EnterWriteLock();
            try
            {
                InternalInsertFile(metadata, enableOverwrite, accessPolicy, path => File.WriteAllText(path, content));
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public void DeleteFile(Guid id, IFileStorageAccessPolicy accessPolicy)
        {
            rwLock.EnterWriteLock();
            try
            {
                CheckDependentTreeDeleteAccess(id, accessPolicy);
                InternalDeleteFile(id);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        private void CheckDependentTreeDeleteAccess(Guid id, IFileStorageAccessPolicy accessPolicy)
        {
            if (entryProperties.TryGetValue(id, out Entry? existingEntry))
            {
                if (!accessPolicy.HasModifyAccessToFile(existingEntry.Metadata))
                {
                    throw new UnauthorizedAccessException($"Unauthorized access to file {id}");
                }

                if (dependentEntries.TryGetValue(id, out HashSet<Guid>? dependents))
                {
                    foreach (Guid dependentId in dependents)
                    {
                        CheckDependentTreeDeleteAccess(dependentId, accessPolicy);
                    }
                }
            }
            else
            {
                throw new Exception($"File {id} not found");
            }
        }

        private void InternalDeleteFile(Guid id)
        {
            if (entryProperties.TryGetValue(id, out Entry? existingEntry))
            {
                if (dependentEntries.TryGetValue(id, out HashSet<Guid>? dependents))
                {
                    foreach (Guid dependentId in dependents.ToArray())
                    {
                        InternalDeleteFile(dependentId);
                    }
                }

                string metadataPath = GetMetadataPath(id);
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }

                existingEntry.StreamLocks.DeleteFile();

                RemoveFileEntry(id);
            }
            else
            {
                throw new Exception($"File {id} not found");
            }
        }

        public Stream? OpenReadStream(Guid id, IFileStorageAccessPolicy accessPolicy)
        {
            rwLock.EnterReadLock();
            try
            {
                if (entryProperties.TryGetValue(id, out Entry? entry) && entry.CanBeReadWithPolicy(accessPolicy))
                {
                    return entry.StreamLocks.OpenReadStream();
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        public Stream OpenWriteStream(FileMetadata metadata, bool enableOverwrite, IFileStorageAccessPolicy accessPolicy)
        {
            rwLock.EnterWriteLock();
            try
            {
                CheckDispose();

                Entry entry = InternalInsertFile(metadata, enableOverwrite, accessPolicy, path =>
                {
                    using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        fs.SetLength(0);
                    }
                });

                Stream? stream = entry.StreamLocks.OpenWriteStream();

                return stream ?? throw new Exception($"File {metadata.Id} is already locked");
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public void UpdateMetadata(FileMetadata metadata, IFileStorageAccessPolicy accessPolicy)
        {
            rwLock.EnterWriteLock();
            try
            {
                CheckDispose();

                ValidateMetadata(metadata);

                if (entryProperties.TryGetValue(metadata.Id, out Entry? existingEntry))
                {
                    if (!accessPolicy.HasModifyAccessToFile(existingEntry.Metadata))
                    {
                        throw new UnauthorizedAccessException($"Unauthorized access to file {metadata.Id}");
                    }

                    if (!accessPolicy.HasModifyAccessToFile(metadata))
                    {
                        throw new UnauthorizedAccessException($"Unauthorized access to file {metadata.Id}");
                    }

                    string oldPath = GetFilePath(existingEntry.Metadata);
                    string newPath = GetFilePath(metadata);
                    StreamLocks streamLocks = existingEntry.StreamLocks;

                    if (oldPath != newPath)
                    {
                        if (!existingEntry.CanBeRead)
                        {
                            throw new Exception($"File {metadata.Id} is in use and cannot be moved");
                        }

                        File.Copy(oldPath, newPath, true);
                        streamLocks.DeleteFile();

                        streamLocks = new StreamLocks(newPath);
                    }

                    metadata.SaveTo(GetMetadataPath(metadata.Id));                    
                    RemoveFileEntry(metadata.Id);

                    Entry newEntry = existingEntry with { Metadata = metadata, Path = newPath, StreamLocks = streamLocks };                    
                    InsertFileEntry(newEntry);
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                rwLock.EnterWriteLock();
                try
                {
                    foreach (Entry entry in entryProperties.Values)
                    {
                        entry.StreamLocks.Dispose();
                    }

                    ClearEntries();
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }
        }

        private record Entry(string Path, FileMetadata Metadata, StreamLocks StreamLocks)
        {
            public bool FileExists => File.Exists(Path);

            public bool CanBeRead => FileExists && !StreamLocks.IsOpenExclusively;

            public bool CanBeWritten => !StreamLocks.IsOpen;

            public bool CanBeReadWithPolicy(IFileStorageAccessPolicy accessPolicy) => CanBeRead && (Metadata.IsPublic || accessPolicy.HasReadAccessToFile(Metadata));
        }
    }
}
