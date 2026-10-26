using Abstractions;
using AngleSharp.Dom;
using Lucene.Net.Util;
using Microsoft.ApplicationInsights;
using NkodSk.Abstractions;
using System.Collections.Immutable;

namespace NkodSk.Abstractions
{
    public class InternalCodelistProvider : ICodelistProviderClient
    {
        private readonly IDocumentStorageClient storageClient;

        private readonly ILanguagesSource languages;

        private readonly TelemetryClient? telemetryClient;

        private ImmutableDictionary<string, Codelist>? codelists;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public InternalCodelistProvider(IDocumentStorageClient storageClient, ILanguagesSource languages, TelemetryClient? telemetryClient)
        {
            this.storageClient = storageClient;
            this.languages = languages;
            this.telemetryClient = telemetryClient;
        }

        private bool ShouldRefresh => codelists is null;

        private async Task Refresh()
        {
            if (ShouldRefresh)
            {
                await semaphore.WaitAsync();
                try
                {
                    if (ShouldRefresh)
                    {
                        FileStorageQuery query = new FileStorageQuery
                        {
                            OnlyTypes = new List<FileType> { FileType.Codelist },
                            OnlyPublished = true
                        };
                        FileStorageResponse response = await storageClient.GetFileStates(query).ConfigureAwait(false);

                        Dictionary<string, Codelist> newCodelists = new Dictionary<string, Codelist>();
                        foreach (FileState state in response.Files)
                        {
                            try
                            {
                                SkosConceptScheme? conceptScheme;

                                if (state.Content is not null)
                                {
                                    conceptScheme = SkosConceptScheme.Parse(state.Content);
                                }
                                else
                                {
                                    using (Stream? stream = await storageClient.DownloadStream(state.Metadata.Id).ConfigureAwait(false))
                                    {
                                        if (stream is not null)
                                        {
                                            conceptScheme = SkosConceptScheme.Parse(stream);
                                        }
                                        else
                                        {
                                            conceptScheme = null;
                                        }
                                    }
                                }

                                if (conceptScheme?.Id is not null)
                                {
                                    // Codelists stored before short ids were introduced get deterministic ids, which are persisted on the next update
                                    conceptScheme.AssignShortIds(null);
                                    Codelist codelist = CreateCodelist(conceptScheme, state.Metadata);
                                    newCodelists[codelist.Id] = codelist;
                                }
                            }
                            catch (Exception e)
                            {
                                telemetryClient?.TrackException(e);
                            }
                        }

                        codelists = newCodelists.ToImmutableDictionary();
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        private Codelist CreateCodelist(SkosConceptScheme conceptScheme, FileMetadata metadata)
        {
            Codelist codelist = new Codelist(conceptScheme.Id);

            codelist.FileId = metadata.Id;

            foreach (string lang in languages)
            {
                string? text = conceptScheme.GetLabel(lang);
                if (text is not null)
                {
                    codelist.Labels[lang] = text;
                }
            }

            foreach (SkosConcept concept in conceptScheme.Concepts)
            {
                if (concept?.Id is not null)
                {
                    CodelistItem codelistItem = new CodelistItem(concept.Id);
                    codelistItem.IsDeprecated = concept.IsDeprecated;
                    codelistItem.Priority = concept.Priority;
                    codelistItem.ShortId = concept.ShortId;

                    foreach (string lang in languages)
                    {
                        string? text = concept.GetLabel(lang);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            codelistItem.Labels[lang] = text;
                        }
                    }

                    codelist.Items[codelistItem.Id] = codelistItem;
                }
            }

            return codelist;
        }

        public async Task LoadCodelist(SkosConceptScheme conceptScheme, FileMetadata metadata)
        {
            Codelist codelist = CreateCodelist(conceptScheme, metadata);
            await semaphore.WaitAsync();
            try
            {
                if (codelists is not null)
                {
                    codelists = codelists.SetItem(codelist.Id, codelist);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<List<Codelist>> GetCodelists()
        {
            await Refresh().ConfigureAwait(false);
            ImmutableDictionary<string, Codelist>? codelists = this.codelists;
            if (codelists is not null)
            {
                return codelists.Values.ToList();
            }
            else
            {
                return new List<Codelist>();
            }
        }

        public async Task<Codelist?> GetCodelist(string? id)
        {
            if (id is null) return null;

            await Refresh().ConfigureAwait(false);
            ImmutableDictionary<string, Codelist>? codelists = this.codelists;
            if (codelists is not null && codelists.TryGetValue(id, out Codelist? codelist))
            {
                return codelist;
            }
            return null;
        }

        public async Task<CodelistItem?> GetCodelistItem(string codelistId, string itemId)
        {
            if (codelistId is null) return null;

            await Refresh().ConfigureAwait(false);
            ImmutableDictionary<string, Codelist>? codelists = this.codelists;
            if (codelists is not null && codelists.TryGetValue(codelistId, out Codelist? codelist))
            {
                return codelist.GetItem(itemId);
            }
            return null;
        }

        private async Task<SkosConceptScheme?> LoadConceptScheme(Guid fileId)
        {
            FileState? state = await storageClient.GetFileState(fileId).ConfigureAwait(false);
            if (state?.Content is not null)
            {
                return SkosConceptScheme.Parse(state.Content);
            }

            using Stream? stream = await storageClient.DownloadStream(fileId).ConfigureAwait(false);
            return stream is not null ? SkosConceptScheme.Parse(stream) : null;
        }

        public async Task<bool> UpdateCodelist(Stream stream)
        {
            string path = Path.GetTempFileName();
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    using (stream)
                    {
                        await stream.CopyToAsync(fs).ConfigureAwait(false);
                    }

                    fs.Seek(0, SeekOrigin.Begin);

                    await Refresh();

                    SkosConceptScheme? conceptScheme;
                    conceptScheme = SkosConceptScheme.Parse(fs);
                    if (conceptScheme is not null)
                    {
                        Codelist? existing = await GetCodelist(conceptScheme.Id);

                        SkosConceptScheme? previous = null;
                        if (existing is not null)
                        {
                            // Without the previous version the short ids could not be preserved
                            previous = await LoadConceptScheme(existing.FileId).ConfigureAwait(false) ?? throw new InvalidOperationException($"Unable to load previous version of codelist {conceptScheme.Id}");
                            previous.AssignShortIds(null);
                        }
                        conceptScheme.AssignShortIds(previous);

                        FileMetadata metadata = new FileMetadata(existing?.FileId ?? Guid.NewGuid(), conceptScheme.Id, FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
                        await storageClient.InsertFile(conceptScheme.ToString(), true, metadata);

                        await LoadCodelist(conceptScheme, metadata);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                    //ignore
                }
            }
        }
    }
}
