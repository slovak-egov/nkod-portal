using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF.Query.Algebra;

namespace DocumentStorageApi.Test
{
    public static class Extensions
    {
        public static FileStorageResponse GetResponse(this IEnumerable<FileState> allStates, FileStorageQuery query, IFileStorageAccessPolicy accessPolicy)
        {
            List<FileState> states = new List<FileState>(allStates);

            states.RemoveAll(s => !s.Metadata.IsPublic && !accessPolicy.HasReadAccessToFile(s.Metadata));

            if (query.OnlyPublishers is not null)
            {
                states.RemoveAll(s => s.Metadata.Publisher is null || !query.OnlyPublishers.Contains(s.Metadata.Publisher));
            }
            if (query.OnlyTypes is not null)
            {
                states.RemoveAll(s => !query.OnlyTypes.Contains(s.Metadata.Type));
            }
            if (query.ParentFile is not null)
            {
                states.RemoveAll(s => s.Metadata.ParentFile != query.ParentFile);
            }
            if (query.OnlyIds is not null)
            {
                states.RemoveAll(s => !query.OnlyIds.Contains(s.Metadata.Id));
            }
            if (query.OnlyPublished)
            {
                states.RemoveAll(s => !s.Metadata.IsPublic);
            }
            if (query.ExcludeIds is not null)
            {
                states.RemoveAll(s => query.ExcludeIds.Contains(s.Metadata.Id));
            }
            states.RemoveAll(s => !s.Metadata.Name.ContainsKey(query.Language));

            if (query.AdditionalFilters is not null)
            {
                foreach ((string key, string[] values) in query.AdditionalFilters)
                {
                    HashSet<string> searchValues = new HashSet<string>(values);
                    string filterId = Storage.GetFilterId(key, query.Language);

                    if (values.Length > 0 && (query.RequiredFacets == null || !query.RequiredFacets.Contains(key)))
                    {
                        states.RemoveAll(s => s.Metadata.AdditionalValues is null || !s.Metadata.AdditionalValues.ContainsKey(filterId) || !searchValues.Overlaps(s.Metadata.AdditionalValues[filterId]));
                    }
                }
            }

            List<FileStorageOrderDefinition> orderDefinitions = query.OrderDefinitions?.ToList() ?? new List<FileStorageOrderDefinition>();
            if (orderDefinitions.Count == 0)
            {
                orderDefinitions.Add(new FileStorageOrderDefinition(FileStorageOrderProperty.LastModified, true));
            }

            int count = states.Count;

            int Compare(FileState a, FileState b)
            {
                foreach (FileStorageOrderDefinition orderDefinition in orderDefinitions)
                {
                    int result;
                    switch (orderDefinition.Property)
                    {
                        case FileStorageOrderProperty.Created:
                            result = a.Metadata.Created.CompareTo(b.Metadata.Created);
                            break;
                        case FileStorageOrderProperty.Revelance:
                        case FileStorageOrderProperty.LastModified:
                            result = a.Metadata.LastModified.CompareTo(b.Metadata.LastModified);
                            break;
                        case FileStorageOrderProperty.Name:
                            result = (a.Metadata.Name.GetText("sk") ?? string.Empty).CompareTo(b.Metadata.Name.GetText("sk") ?? string.Empty);
                            break;
                        default:
                            throw new NotSupportedException($"Order property {orderDefinition.Property} is not supported.");
                    }
                    if (result != 0)
                    {
                        return orderDefinition.ReverseOrder ? -result : result;
                    }
                }
                return 0;
            }

            states.Sort(Compare);

            List<FileState> expected = states.Skip(query.SkipResults).Take(query.MaxResults.GetValueOrDefault(int.MaxValue)).ToList();

            return new FileStorageResponse(expected, count, new List<Facet>());
        }
    }
}
