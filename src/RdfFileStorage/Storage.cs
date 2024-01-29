﻿using AngleSharp.Dom;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime;
using System.Security.Policy;
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

        private readonly string? logPath;

        private readonly HashSet<Guid> entries;

        private readonly Dictionary<Guid, Entry> entryProperties;

        private readonly Dictionary<Guid, HashSet<Guid>> dependentEntries;

        private readonly Dictionary<string, HashSet<Guid>> entriesByPublisher;

        private readonly Dictionary<FileType, HashSet<Guid>> entriesByType;

        private readonly Dictionary<string, HashSet<Guid>> entriesByLanguage;

        private readonly Dictionary<string, Dictionary<string, HashSet<Guid>>> additionalFilters;

        private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        private readonly ReadOnlyDictionary<FileType, string> fileTypeFolders;

        private int disposed;

        private int defaultCapacity;

        public Storage(string path, string publicTurtleFolderName = "public", string protectedFolderName = "protected", string? logPath = null)
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

            FileType[] fileTypes = Enum.GetValues<FileType>();
            Dictionary<FileType, string> fileTypeFolders = new Dictionary<FileType, string>(fileTypes.Length);
            foreach (FileType fileType in Enum.GetValues<FileType>())
            {
                string folderPath = Path.Combine(publicTurtlePath, GetFileTypeSubfolderName(fileType));
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                fileTypeFolders[fileType] = folderPath;
            }
            this.fileTypeFolders = new ReadOnlyDictionary<FileType, string>(fileTypeFolders);

            protectedPath = Path.Combine(path, protectedFolderName);
            if (!Directory.Exists(protectedPath))
            {
                Directory.CreateDirectory(protectedPath);
            }

            string[] files = GetAllMetadataFiles();
            defaultCapacity = files.Length * 2;

            entries = new HashSet<Guid>(defaultCapacity);
            entryProperties = new Dictionary<Guid, Entry>(defaultCapacity);
            dependentEntries = new Dictionary<Guid, HashSet<Guid>>(defaultCapacity);
            entriesByPublisher = new Dictionary<string, HashSet<Guid>>(defaultCapacity);
            entriesByType = new Dictionary<FileType, HashSet<Guid>>(fileTypes.Length);
            entriesByLanguage = new Dictionary<string, HashSet<Guid>>();
            additionalFilters = new Dictionary<string, Dictionary<string, HashSet<Guid>>>();

            LoadEntries(files);
            this.logPath = logPath;
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
                
                foreach (FileType fileType in Enum.GetValues<FileType>())
                {
                    if (!entriesByType.ContainsKey(fileType))
                    {
                        entriesByType[fileType] = new HashSet<Guid>(defaultCapacity);
                    }
                }

                List<Exception> exceptions = new List<Exception>();

                try
                {
                    string testFileName = $"_test_{Guid.NewGuid()}_test";

                    void TestFolder(string directory)
                    {
                        string path = Path.Combine(directory, testFileName);
                        using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                        {
                            fs.Write(Encoding.UTF8.GetBytes(testFileName));
                            fs.Close();
                        }                            
                        File.Delete(path);
                    }

                    TestFolder(publicTurtlePath);
                    TestFolder(protectedPath);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }

                Parallel.ForEach(files, metadataPath =>
                {
                    try
                    {
                        static void CheckFile(string path)
                        {
                            if (!File.Exists(path))
                            {
                                throw new Exception($"Unable to find target file {path}");
                            }

                            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                            if (!fs.CanRead)
                            {
                                throw new Exception($"File {path} is not readable");
                            }
                            if (!fs.CanWrite)
                            {
                                throw new Exception($"File {path} is not writable");
                            }
                        }

                        CheckFile(metadataPath);

                        FileMetadata metadata = FileMetadata.LoadFrom(metadataPath);
                        string filePath = GetFilePath(metadata);

                        CheckFile(filePath);

                        lock (entries)
                        {
                            InsertFileEntry(metadata, filePath);
                        }
                    }
                    catch (Exception e)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(e);
                        }
                    }
                });

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

            foreach ((string language, string name) in metadata.Name)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    if (!entriesByLanguage.ContainsKey(language))
                    {
                        entriesByLanguage[language] = new HashSet<Guid>(defaultCapacity);
                    }
                    entriesByLanguage[language].Add(metadata.Id);
                }
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

                foreach (string language in metadata.Name.Keys)
                {
                    if (entriesByLanguage.TryGetValue(language, out HashSet<Guid>? languageFiles))
                    {
                        languageFiles.Remove(metadata.Id);
                    }
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

        public static bool ShouldBePublic(FileMetadata metadata) => metadata.IsPublic && IsTurtleFile(metadata) && !metadata.IsHarvested;

        private static string GetFileTypeSubfolderName(FileType fileType)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in Enum.GetName(fileType) ?? fileType.ToString())
            {
                if (sb.Length > 0 && char.IsUpper(c))
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        public static string GetDefaultSubfolderName(FileMetadata metadata)
        {
            if (ShouldBePublic(metadata))
            {
                return Path.Combine("public", GetFileTypeSubfolderName(metadata.Type));
            }
            else
            {
                return "protected";
            }
        }

        private string GetFilePath(FileMetadata metadata)
        {
            if (ShouldBePublic(metadata))
            {
                return Path.Combine(fileTypeFolders[metadata.Type], metadata.Id.ToString("N") + ".ttl");
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

        public static string GetFilterId(string key, string language)
        {
            return key switch
            {
                "keywords" => key + "_" + language,
                _ => key
            };
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
                else
                {
                    sets.Add(new HashSet<Guid>());
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
                    string filterId = GetFilterId(key, query.Language);

                    if (values.Length > 0 && (query.RequiredFacets == null || !query.RequiredFacets.Contains(key)))
                    {
                        if (additionalFilters.TryGetValue(filterId, out Dictionary<string, HashSet<Guid>>? filterValues))
                        {
                            sets.Add(MergeSets(filterValues, values));
                        }
                        else
                        {
                            sets.Add(new HashSet<Guid>());
                        }
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

            if (query.ExcludeIds is not null)
            {
                set.ExceptWith(query.ExcludeIds);
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
                    Dictionary<string, HashSet<Entry>> resultsWithFacet = new Dictionary<string, HashSet<Entry>>();

                    if (query.RequiredFacets.Contains("publishers") && query.OnlyPublishers is not null && query.OnlyPublishers.Count > 0)
                    {
                        HashSet<string> publisherKeys = new HashSet<string>(query.OnlyPublishers);
                        
                        HashSet<Entry> filteredEntries = new HashSet<Entry>(results);
                        filteredEntries.RemoveWhere(e => e.Metadata.Publisher == null || !publisherKeys.Contains(e.Metadata.Publisher));

                        resultsWithFacet["publishers"] = filteredEntries;
                    }

                    if (query.AdditionalFilters is not null)
                    {
                        foreach (string facetId in query.RequiredFacets)
                        {
                            if (query.AdditionalFilters.TryGetValue(facetId, out string[]? values) && values.Length > 0)
                            {
                                string filterId = GetFilterId(facetId, query.Language);
                                HashSet<string> keys = new HashSet<string>(values);

                                HashSet<Entry> filteredEntries = new HashSet<Entry>(results);
                                filteredEntries.RemoveWhere(e => e.Metadata.AdditionalValues == null || !e.Metadata.AdditionalValues.TryGetValue(filterId, out string[]? entryValues) || !keys.Overlaps(entryValues));

                                resultsWithFacet[facetId] = filteredEntries;
                            }                                
                        }
                    }

                    foreach (string facetId in query.RequiredFacets)
                    {
                        HashSet<Entry> sourceEntries = new HashSet<Entry>(results);

                        foreach (HashSet<Entry> facetEntries in resultsWithFacet.Where(kv => kv.Key != facetId).Select(kv => kv.Value))
                        {
                            sourceEntries.IntersectWith(facetEntries);
                        }

                        Facet facet = new Facet(facetId);
                        Dictionary<string, int> counts = facet.Values;

                        switch (facetId)
                        {
                            case "publishers":
                                foreach (Entry entry in sourceEntries)
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
                                string filterId = GetFilterId(facetId, query.Language);
                                foreach (Entry entry in sourceEntries)
                                {
                                    if (entry.Metadata.AdditionalValues is not null && entry.Metadata.AdditionalValues.TryGetValue(filterId, out string[]? values))
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

                    if (resultsWithFacet.Count > 0)
                    {
                        HashSet<Entry> filteredEntries = new HashSet<Entry>(results);
                        foreach (HashSet<Entry> facetEntries in resultsWithFacet.Values)
                        {
                            filteredEntries.IntersectWith(facetEntries);
                        }

                        results = filteredEntries.ToList();
                    }
                }

                if (orderDefinitions.Count >= 1 && orderDefinitions[0].Property == FileStorageOrderProperty.Revelance && !orderDefinitions[0].ReverseOrder && query.OnlyIds is not null && query.OnlyIds.Count > 0)
                {
                    orderDefinitions.RemoveAt(0);
                    Dictionary<Guid, Entry> indexedEntries = new Dictionary<Guid, Entry>(results.Count);
                    foreach (Entry entry in results)
                    {
                        indexedEntries[entry.Metadata.Id] = entry;
                    }
                    results.Clear();
                    foreach (Guid id in query.OnlyIds)
                    {
                        if (indexedEntries.TryGetValue(id, out Entry? entry))
                        {
                            results.Add(entry);
                        }
                    }
                }

                if (orderDefinitions.Count > 0)
                {
                    results.Sort(CompareEntries);
                }

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
            query.OnlyTypes = new List<FileType> { FileType.PublisherRegistration };
            Dictionary<string, int> countByPublisher = new Dictionary<string, int>(100);
            Dictionary<string, Dictionary<string, int>> themesByPublisher = new Dictionary<string, Dictionary<string, int>>(100);

            rwLock.EnterReadLock();
            try
            {
                CheckDispose();

                HashSet<string>? onlyPublishers = query.OnlyPublishers is not null && query.OnlyPublishers.Count > 0 ? new HashSet<string>(query.OnlyPublishers) : null;

                string themeKey = $"keywords_{query.Language}";

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
                        if (onlyPublishers is not null && !onlyPublishers.Contains(publisherName))
                        {
                            continue;
                        }

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

            List<Entry> publisherEntries = GetRelevantEntries(query, accessPolicy);
            List<FileStorageGroup> groups = new List<FileStorageGroup>(publisherEntries.Count);
            foreach (Entry entry in publisherEntries)
            {
                string? fileContent = ReadFileContent(entry.Path);
                FileState publisherState = new FileState(entry.Metadata, fileContent);
                string? publisher = publisherState.Metadata.Publisher;
                if (publisher is not null)
                {
                    countByPublisher.TryGetValue(publisher, out int count);
                    themesByPublisher.TryGetValue(publisher, out Dictionary<string, int>? themes);
                    groups.Add(new FileStorageGroup(publisher, publisherState, count, themes));
                }                
            }

            List<FileStorageOrderDefinition> orderDefinitions = query.OrderDefinitions?.ToList() ?? new List<FileStorageOrderDefinition>(2);
            if (orderDefinitions.Count == 0)
            {
                orderDefinitions.Add(new FileStorageOrderDefinition(FileStorageOrderProperty.Revelance, true));
                orderDefinitions.Add(new FileStorageOrderDefinition(FileStorageOrderProperty.Name, false));
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

        private void LogFileChange(FileMetadata metadata, Action<string> copyAction, IStorageLogAdapter? logAdapter)
        {
            if (logPath is not null)
            {
                string logName = $"{metadata.Id:N}_{DateTimeOffset.UtcNow:yyyyMMddHHiiss.fffff}_{Guid.NewGuid():N}";
                string fullPath = Path.Combine(logPath, logName);
                copyAction(fullPath);
                logAdapter?.LogFileCreated(fullPath);
            }
        }

        public void InsertFile(string content, FileMetadata metadata, bool enableOverwrite, IFileStorageAccessPolicy accessPolicy, IStorageLogAdapter? logAdapter = null)
        {
            rwLock.EnterWriteLock();
            try
            {
                InternalInsertFile(metadata, enableOverwrite, accessPolicy, path =>
                {
                    File.WriteAllText(path, content);
                    LogFileChange(metadata, logPath =>
                    {
                        File.Copy(path, logPath, false);
                    }, logAdapter);
                });
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
                if (!accessPolicy.HasDeleteAccessToFile(existingEntry.Metadata))
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

        public Stream OpenWriteStream(FileMetadata metadata, bool enableOverwrite, IFileStorageAccessPolicy accessPolicy, IStorageLogAdapter? logAdapter = null)
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

                FileStreamWrap? stream = entry.StreamLocks.OpenWriteStream();

                if (stream is not null)
                {
                    stream.Disposed += (object? _, EventArgs _) =>
                    {
                        LogFileChange(metadata, logPath =>
                        {
                            File.Copy(GetFilePath(metadata), logPath, false);
                        }, logAdapter);
                    };
                }

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
